# main.py - Main entry point
import machine
import time
import ujson
import network
import urequests
from line_follower import LineFollower
from firebase_handler import FirebaseHandler
from compartment_controller import CompartmentController

class DeliveryRobot:
    def __init__(self):
        # Robot state
        self.current_location = 0  # 0 = Base, 1-4 = Rooms
        self.is_moving = False
        self.active_deliveries = []  # Max 3 deliveries (one per compartment)
        
        # Hardware controllers
        self.line_follower = LineFollower()
        self.firebase = FirebaseHandler()
        self.compartments = CompartmentController()
        
        # Constants
        self.BASE = 0
        self.MAX_DELIVERIES = 3
        self.CONFIRMATION_TIMEOUT = 30  # seconds
        
        print("🤖 Lalabot initialized at BASE")
        
    def start(self):
        """Main robot loop"""
        print("🚀 Starting delivery robot...")
        
        while True:
            try:
                # Check WiFi connection
                if not self.firebase.is_connected():
                    print("⚠️ WiFi disconnected, reconnecting...")
                    self.stop_movement()
                    self.firebase.reconnect()
                    continue
                
                # Listen for new deliveries if we have space
                if len(self.active_deliveries) < self.MAX_DELIVERIES:
                    self.check_for_new_deliveries()
                
                # Process active deliveries
                if self.active_deliveries and not self.is_moving:
                    self.process_next_delivery()
                
                time.sleep(0.5)  # Small delay to prevent busy loop
                
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
                    
                    # Update Firebase with compartment assignment
                    self.firebase.update_delivery_compartment(delivery_id, available_compartment)
                    
                    print(f"✅ Accepted delivery {delivery_id} → Compartment {available_compartment}")
                    print(f"   From: Room {delivery_data['pickup']} → Room {delivery_data['destination']}")
                    
                    # If this is our first delivery, start immediately
                    if len(self.active_deliveries) == 1:
                        break
                        
        except Exception as e:
            print(f"⚠️ Error checking deliveries: {e}")
    
    def get_available_compartment(self):
        """Returns the first available compartment number (1-3) or None"""
        used_compartments = [d['compartment'] for d in self.active_deliveries]
        for i in range(1, 4):
            if i not in used_compartments:
                return i
        return None
    
    def process_next_delivery(self):
        """Process deliveries in order of pickup location"""
        if not self.active_deliveries:
            # No deliveries, return to base if not already there
            if self.current_location != self.BASE:
                print("📍 No deliveries, returning to base...")
                self.navigate_to(self.BASE)
            return
        
        # Sort deliveries by pickup location for efficient routing
        self.active_deliveries.sort(key=lambda d: d['pickup'])
        
        # Process first delivery in queue
        delivery = self.active_deliveries[0]
        
        if delivery['state'] == 'assigned':
            # Go to pickup location
            self.go_to_pickup(delivery)
            
        elif delivery['state'] == 'at_pickup':
            # Wait for user to place files
            self.wait_for_file_confirmation(delivery)
            
        elif delivery['state'] == 'files_confirmed':
            # Go to destination
            self.go_to_destination(delivery)
            
        elif delivery['state'] == 'at_destination':
            # Wait for receiver verification and pickup
            self.wait_for_receiver(delivery)
            
        elif delivery['state'] == 'completed':
            # Remove from active list
            self.active_deliveries.remove(delivery)
            print(f"✅ Delivery {delivery['id']} completed!")
    
    def go_to_pickup(self, delivery):
        """Navigate to pickup location and open compartment"""
        pickup_room = delivery['pickup']
        compartment = delivery['compartment']
        
        print(f"🚚 Going to pickup at Room {pickup_room} (Compartment {compartment})")
        
        # Update Firebase: in transit to pickup
        self.firebase.update_delivery_stage(delivery['id'], 0)  # Stage 0: Processing
        
        # Navigate to pickup room
        self.navigate_to(pickup_room)
        
        # We've arrived at pickup
        print(f"📍 Arrived at Room {pickup_room}")
        
        # Open compartment
        self.compartments.open(compartment)
        print(f"📂 Compartment {compartment} opened")
        
        # Update state and Firebase
        delivery['state'] = 'at_pickup'
        self.firebase.update_delivery_location(delivery['id'], pickup_room)
        self.firebase.set_files_confirmed(delivery['id'], False)
        
        # Set confirmation deadline (30 seconds from now)
        deadline = time.time() + self.CONFIRMATION_TIMEOUT
        delivery['confirmation_deadline'] = deadline
        self.firebase.set_confirmation_deadline(delivery['id'], deadline)
    
    def wait_for_file_confirmation(self, delivery):
        """Wait for sender to confirm files are placed"""
        # Check if files confirmed in Firebase
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
            
        else:
            # Check timeout
            if time.time() > delivery.get('confirmation_deadline', float('inf')):
                print(f"⏰ Timeout waiting for file confirmation - {delivery['id']}")
                
                # Send notification (handled by app, we just update status)
                self.firebase.notify_confirmation_timeout(delivery['id'])
                
                # Extend deadline by 30 seconds (give them another chance)
                delivery['confirmation_deadline'] = time.time() + self.CONFIRMATION_TIMEOUT
                
                # After 2 timeouts, cancel delivery
                delivery['timeout_count'] = delivery.get('timeout_count', 0) + 1
                if delivery['timeout_count'] >= 2:
                    print(f"❌ Cancelling delivery {delivery['id']} - no response")
                    self.cancel_delivery(delivery)
    
    def go_to_destination(self, delivery):
        """Navigate to destination"""
        destination_room = delivery['destination']
        
        print(f"🚚 Delivering to Room {destination_room}")
        
        # Update Firebase: Stage 1 (In Transit)
        self.firebase.update_delivery_stage(delivery['id'], 1)
        
        # Navigate to destination
        self.navigate_to(destination_room)
        
        # Update Firebase: Stage 2 (Approaching)
        self.firebase.update_delivery_stage(delivery['id'], 2)
        
        # Brief moment before arrival
        time.sleep(1)
        
        # Update Firebase: Stage 3 (Arrived)
        self.firebase.update_delivery_stage(delivery['id'], 3)
        self.firebase.set_ready_for_pickup(delivery['id'], True)
        
        print(f"📍 Arrived at Room {destination_room} - waiting for receiver")
        
        # Update state
        delivery['state'] = 'at_destination'
        delivery['arrival_time'] = time.time()
    
    def wait_for_receiver(self, delivery):
        """Wait for receiver to verify and collect files"""
        # Check if receiver retrieved files
        files_received = self.firebase.get_files_received(delivery['id'])
        
        if files_received:
            print(f"✅ Files received - delivery {delivery['id']} complete!")
            
            # Close compartment
            self.compartments.close(delivery['compartment'])
            
            # Mark as completed
            delivery['state'] = 'completed'
            self.firebase.mark_delivery_completed(delivery['id'])
            
            # Free the compartment
            self.firebase.free_compartment(delivery['compartment'])
    
    def navigate_to(self, target_room):
        """Navigate from current location to target room"""
        if self.current_location == target_room:
            print(f"✓ Already at target location: Room {target_room}")
            return
        
        print(f"🗺️ Navigating: Room {self.current_location} → Room {target_room}")
        
        self.is_moving = True
        
        # Start following the line
        self.line_follower.start()
        
        # Track room counter
        rooms_to_pass = self.calculate_rooms_to_pass(self.current_location, target_room)
        rooms_passed = 0
        
        while rooms_passed < rooms_to_pass:
            if not self.firebase.is_connected():
                print("⚠️ WiFi lost during navigation - stopping")
                self.stop_movement()
                self.firebase.reconnect()
                # Resume from current position
                continue
            
            # Check for intersection (all sensors black = room detected)
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
        """Calculate how many rooms to pass (circular track)"""
        # For circular: 0(Base) -> 1 -> 2 -> 3 -> 4 -> back to 0
        if target >= current:
            return target - current
        else:
            # Wrap around (e.g., from Room 4 to Base)
            return (4 - current) + target
    
    def get_next_room(self, current):
        """Get the next room number in circular fashion"""
        next_room = current + 1
        if next_room > 4:
            return 0  # Back to base
        return next_room
    
    def cancel_delivery(self, delivery):
        """Cancel a delivery and return to base"""
        print(f"🚫 Cancelling delivery {delivery['id']}")
        
        # Close compartment if open
        self.compartments.close(delivery['compartment'])
        
        # Update Firebase
        self.firebase.cancel_delivery(delivery['id'])
        
        # Remove from active list
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
        
        # Try to save state
        try:
            self.firebase.report_error(str(error), self.current_location)
        except:
            pass
        
        # Wait before retrying
        time.sleep(5)


# Initialize and start the robot
if __name__ == "__main__":
    robot = DeliveryRobot()
    robot.start()