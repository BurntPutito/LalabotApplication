# main.py - Main Lalabot delivery robot controller
import time
import signal
import sys
from line_follower import LineFollower
from firebase_handler import FirebaseHandler
from compartment_controller import CompartmentController
from theft_protection import TheftProtection
from config import CONFIRMATION_TIMEOUT, MAX_TIMEOUT_RETRIES

class DeliveryRobot:
    def __init__(self):
        print("🤖 Initializing Lalabot Delivery Robot...")
        
        # Robot state
        self.current_location = 0  # 0 = Base, 1-4 = Rooms
        self.is_moving = False
        self.active_deliveries = []  # Max 3 deliveries
        
        # Hardware controllers
        self.line_follower = LineFollower()
        self.firebase = FirebaseHandler()
        self.compartments = CompartmentController()
        self.theft_protection = TheftProtection(
            self.line_follower,
            self.firebase,
            self.line_follower.h
        )
        
        # Constants
        self.BASE = 0
        self.MAX_DELIVERIES = 3
        self.CONFIRMATION_TIMEOUT = CONFIRMATION_TIMEOUT
        self.MAX_TIMEOUT_RETRIES = MAX_TIMEOUT_RETRIES
        
        print("🤖 Lalabot initialized at BASE")
        print("=" * 50)
    
    def start(self):
        """Main robot loop"""
        print("🚀 Starting delivery robot system...")
        print("=" * 50)
        
        # Setup signal handler for graceful shutdown
        signal.signal(signal.SIGINT, self.signal_handler)
        
        while True:
            try:
                # Security check
                if self.theft_protection.check_for_theft(self.is_moving):
                    self.stop_movement()
                    print("⚠️ Theft detected! Robot stopped.")
                    print("Press Ctrl+C to exit or verify robot is safe...")
                    time.sleep(5)
                    self.theft_protection.disable_alarm()
                    continue
                
                # Check WiFi connection
                if not self.firebase.is_connected():
                    print("⚠️ WiFi disconnected, reconnecting...")
                    self.stop_movement()
                    self.firebase.reconnect()
                    continue
                
                # Check for new deliveries if we have space
                if len(self.active_deliveries) < self.MAX_DELIVERIES:
                    self.check_for_new_deliveries()
                
                # Process active deliveries
                if self.active_deliveries and not self.is_moving:
                    self.process_next_delivery()
                
                time.sleep(0.5)  # Small delay
                
            except Exception as e:
                print(f"❌ Error in main loop: {e}")
                self.handle_critical_error(e)
    
    def check_for_new_deliveries(self):
        """Check Firebase for pending deliveries"""
        try:
            pending = self.firebase.get_pending_deliveries()
            
            for delivery_id, delivery_data in pending.items():
                # Check if we already have this delivery
                if any(d['id'] == delivery_id for d in self.active_deliveries):
                    continue
                
                # Assign available compartment
                available_compartment = self.get_available_compartment()
                if available_compartment:
                    delivery_data['id'] = delivery_id
                    delivery_data['compartment'] = available_compartment
                    delivery_data['state'] = 'assigned'
                    
                    self.active_deliveries.append(delivery_data)
                    
                    # Update Firebase
                    self.firebase.update_delivery_compartment(delivery_id, available_compartment)
                    
                    print(f"✅ Accepted delivery {delivery_id}")
                    print(f"   Compartment: {available_compartment}")
                    print(f"   From: Room {delivery_data['pickup']} → Room {delivery_data['destination']}")
                    print("=" * 50)
                    
                    # If first delivery, start immediately
                    if len(self.active_deliveries) == 1:
                        break
                        
        except Exception as e:
            print(f"⚠️ Error checking deliveries: {e}")
    
    def get_available_compartment(self):
        """Returns first available compartment (1-3) or None"""
        used_compartments = [d['compartment'] for d in self.active_deliveries]
        for i in range(1, 4):
            if i not in used_compartments:
                return i
        return None
    
    def process_next_delivery(self):
        """Process deliveries in order"""
        if not self.active_deliveries:
            # No deliveries, return to base
            if self.current_location != self.BASE:
                print("📍 No deliveries, returning to base...")
                self.navigate_to(self.BASE)
            return
        
        # Sort by pickup location for efficient routing
        self.active_deliveries.sort(key=lambda d: d['pickup'])
        
        # Process first delivery
        delivery = self.active_deliveries[0]
        
        if delivery['state'] == 'assigned':
            self.go_to_pickup(delivery)
            
        elif delivery['state'] == 'at_pickup':
            self.wait_for_file_confirmation(delivery)
            
        elif delivery['state'] == 'files_confirmed':
            self.go_to_destination(delivery)
            
        elif delivery['state'] == 'at_destination':
            self.wait_for_receiver(delivery)
            
        elif delivery['state'] == 'completed':
            self.active_deliveries.remove(delivery)
            print(f"✅ Delivery {delivery['id']} completed!")
            print("=" * 50)
    
    def go_to_pickup(self, delivery):
        """Navigate to pickup and open compartment"""
        pickup_room = delivery['pickup']
        compartment = delivery['compartment']
        
        print(f"🚚 Going to pickup at Room {pickup_room}")
        
        # Update Firebase: Stage 0 (Processing)
        self.firebase.update_delivery_stage(delivery['id'], 0)
        
        # Navigate
        self.navigate_to(pickup_room)
        
        print(f"📍 Arrived at Room {pickup_room}")
        
        # Open compartment
        self.compartments.open(compartment)
        print(f"📂 Compartment {compartment} opened - waiting for files...")
        
        # Update state
        delivery['state'] = 'at_pickup'
        self.firebase.update_delivery_location(delivery['id'], pickup_room)
        self.firebase.set_files_confirmed(delivery['id'], False)
        
        # Set deadline
        deadline = time.time() + self.CONFIRMATION_TIMEOUT
        delivery['confirmation_deadline'] = deadline
        delivery['timeout_count'] = 0
        self.firebase.set_confirmation_deadline(delivery['id'], deadline)
    
    def wait_for_file_confirmation(self, delivery):
        """Wait for sender to confirm files placed"""
        # Check Firebase
        files_confirmed = self.firebase.get_files_confirmed(delivery['id'])
        
        if files_confirmed:
            print(f"✅ Files confirmed for delivery {delivery['id']}")
            
            # Close compartment
            self.compartments.close(delivery['compartment'])
            print(f"🔒 Compartment {delivery['compartment']} closed")
            
            # Update state
            delivery['state'] = 'files_confirmed'
            
            # Update Firebase: Stage 1 (In Transit)
            self.firebase.update_delivery_stage(delivery['id'], 1)
            print("🚚 Starting delivery...")
            
        else:
            # Check timeout
            if time.time() > delivery.get('confirmation_deadline', float('inf')):
                delivery['timeout_count'] = delivery.get('timeout_count', 0) + 1
                
                print(f"⏰ Timeout {delivery['timeout_count']}/{self.MAX_TIMEOUT_RETRIES} waiting for confirmation")
                
                if delivery['timeout_count'] >= self.MAX_TIMEOUT_RETRIES:
                    print(f"❌ Cancelling delivery {delivery['id']} - no response")
                    self.cancel_delivery(delivery)
                else:
                    # Extend deadline
                    new_deadline = time.time() + self.CONFIRMATION_TIMEOUT
                    delivery['confirmation_deadline'] = new_deadline
                    self.firebase.set_confirmation_deadline(delivery['id'], new_deadline)
                    print(f"⏱️ Extended deadline by {self.CONFIRMATION_TIMEOUT} seconds")
    
    def go_to_destination(self, delivery):
        """Navigate to destination"""
        destination_room = delivery['destination']
        
        print(f"🚚 Delivering to Room {destination_room}")
        
        # Navigate
        self.navigate_to(destination_room)
        
        # Update Firebase: Stage 2 (Approaching)
        self.firebase.update_delivery_stage(delivery['id'], 2)
        print(f"📍 Approaching Room {destination_room}...")
        
        time.sleep(1)
        
        # Update Firebase: Stage 3 (Arrived)
        self.firebase.update_delivery_stage(delivery['id'], 3)
        self.firebase.set_ready_for_pickup(delivery['id'], True)
        
        print(f"📍 Arrived at Room {destination_room}")
        print(f"⏳ Waiting for receiver verification...")
        
        # Update state
        delivery['state'] = 'at_destination'
        delivery['arrival_time'] = time.time()
    
    def wait_for_receiver(self, delivery):
        """Wait for receiver to verify and collect files"""
        # Check if files received
        files_received = self.firebase.get_files_received(delivery['id'])
        
        if files_received:
            print(f"✅ Files received - delivery complete!")
            
            # Close compartment
            self.compartments.close(delivery['compartment'])
            
            # Mark completed
            delivery['state'] = 'completed'
            self.firebase.mark_delivery_completed(delivery['id'])
            
            # Free compartment
            self.firebase.free_compartment(delivery['id'])
    
    def navigate_to(self, target_room):
        """Navigate from current location to target room"""
        if self.current_location == target_room:
            print(f"✓ Already at Room {target_room}")
            return
        
        print(f"🗺️ Navigating: Room {self.current_location} → Room {target_room}")
        
        self.is_moving = True
        
        # Start line following
        self.line_follower.start()
        
        # Calculate rooms to pass
        rooms_to_pass = self.calculate_rooms_to_pass(self.current_location, target_room)
        rooms_passed = 0
        
        print(f"📊 Need to pass {rooms_to_pass} room(s)")
        
        while rooms_passed < rooms_to_pass:
            # Check WiFi during navigation
            if not self.firebase.is_connected():
                print("⚠️ WiFi lost - stopping")
                self.stop_movement()
                self.firebase.reconnect()
                print("✅ Resuming navigation...")
                self.line_follower.start()
            
            # Check for intersection
            if self.line_follower.detect_intersection():
                rooms_passed += 1
                self.current_location = self.get_next_room(self.current_location)
                
                print(f"🏠 Passed Room {self.current_location} ({rooms_passed}/{rooms_to_pass})")
                
                # Brief pause to avoid double-counting
                time.sleep(0.3)
        
        # Stop at target
        self.line_follower.stop()
        self.is_moving = False
        
        print(f"✅ Reached Room {target_room}")
    
    def calculate_rooms_to_pass(self, current, target):
        """Calculate rooms to pass in circular track"""
        # Circular: 0(Base) -> 1 -> 2 -> 3 -> 4 -> back to 0
        if target >= current:
            return target - current
        else:
            # Wrap around
            return (4 - current) + target
    
    def get_next_room(self, current):
        """Get next room number (circular)"""
        next_room = current + 1
        if next_room > 4:
            return 0  # Back to base
        return next_room
    
    def cancel_delivery(self, delivery):
        """Cancel a delivery"""
        print(f"🚫 Cancelling delivery {delivery['id']}")
        
        # Close compartment
        self.compartments.close(delivery['compartment'])
        
        # Update Firebase
        self.firebase.cancel_delivery(delivery['id'])
        self.firebase.free_compartment(delivery['id'])
        
        # Remove from list
        self.active_deliveries.remove(delivery)
    
    def stop_movement(self):
        """Emergency stop"""
        self.line_follower.stop()
        self.is_moving = False
        print("🛑 Movement stopped")
    
    def handle_critical_error(self, error):
        """Handle critical errors"""
        print(f"🆘 Critical error: {error}")
        self.stop_movement()
        
        try:
            self.firebase.report_error(str(error), self.current_location)
        except:
            pass
        
        time.sleep(5)
    
    def signal_handler(self, sig, frame):
        """Handle Ctrl+C gracefully"""
        print("\n\n🛑 Shutdown signal received...")
        self.cleanup()
        sys.exit(0)
    
    def cleanup(self):
        """Cleanup all resources"""
        print("🧹 Cleaning up...")
        
        try:
            self.stop_movement()
            self.compartments.cleanup()
            self.line_follower.cleanup()
            self.theft_protection.cleanup()
            print("✅ Cleanup complete")
        except Exception as e:
            print(f"⚠️ Cleanup error: {e}")


# Main entry point
if __name__ == "__main__":
    print("=" * 50)
    print("    LALABOT DELIVERY ROBOT SYSTEM")
    print("=" * 50)
    print()
    
    try:
        robot = DeliveryRobot()
        robot.start()
    except KeyboardInterrupt:
        print("\n\n🛑 Program interrupted by user")
    except Exception as e:
        print(f"\n\n❌ Fatal error: {e}")
    finally:
        print("\n👋 Shutting down Lalabot...")