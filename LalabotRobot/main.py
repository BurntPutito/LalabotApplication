import time
import requests 
from time import sleep
import signal
import sys
from firebase_handler import FirebaseHandler
from motor_controller import MotorController
from line_follower import LineFollower
from compartment_controller import CompartmentController
from obstacle_detector import ObstacleDetector
from config import ROOM_COUNT

class DeliveryRobot:
    def __init__(self):
        # Initialize servos with correct pulse widths
        self.servos = {
            1: Servo(SERVO1_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000),
            2: Servo(SERVO2_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000),
            3: Servo(SERVO3_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000)
        }
        
        # Track compartment states - assume they might be in any position
        self.states = {1: "unknown", 2: "unknown", 3: "unknown"}
        
        # FORCE close all compartments on startup
        print("🔒 Forcing all compartments to closed position...")
        for num in self.servos:
            servo = self.servos[num]
            servo.value = self._angle_to_value(SERVO_CLOSED)
            self.states[num] = "closed"
        
        sleep(2)  # Give servos time to reach position
        
        # Detach servos after positioning to stop jitter
        for servo in self.servos.values():
            servo.detach()
        
        print("✓ Compartment controller initialized (all closed and verified)")
    
    def shutdown(self, signum, frame):
        """Clean shutdown handler"""
        print("\n\n⚠ Shutdown signal received...")
        self.running = False
        self.cleanup()
        sys.exit(0)
    
    def cleanup(self):
        """Cleanup all components"""
        print("\n🧹 Cleaning up...")
        self.motors.stop()
        self.compartments.close_all()
        self.motors.cleanup()
        self.compartments.cleanup()
        self.obstacle_detector.cleanup()
        self.line_follower.cleanup()
        print("✓ Cleanup complete\n")
    
    def plan_route(self, deliveries):
        """
        Plan optimal route - visit rooms in order (0→1→2→3→4)
        Handle ALL pickups/deliveries at each room before moving to next
        
        Returns: List of (room, tasks) where tasks = [(delivery, action), ...]
        """
        route = {}  # room: [(delivery, 'pickup'/'deliver'), ...]
        
        for delivery in deliveries:
            pickup_room = delivery['pickup']
            dest_room = delivery['destination']
            
            # Add pickup task
            if pickup_room not in route:
                route[pickup_room] = []
            route[pickup_room].append((delivery, 'pickup'))
            
            # Add delivery task
            if dest_room not in route:
                route[dest_room] = []
            route[dest_room].append((delivery, 'deliver'))
        
        # Sort rooms in order (circular route)
        sorted_route = []
        for room in range(1, 5):  # Rooms 1, 2, 3, 4
            if room in route:
                sorted_route.append((room, route[room]))
        
        return sorted_route
    
    def plan_route_from_current_location(self, deliveries):
        """
        Plan route starting from CURRENT location with proper task ordering
        Ensures pickups happen before deliveries for each delivery
        Allows multiple visits to same room if needed (circular route)
        
        Returns: List of (room, tasks) in circular order from current location
        """
        pickup_status = {}  # delivery_id: picked_up (True/False)
        
        # Initialize pickup status for all deliveries
        for delivery in deliveries:
            pickup_status[delivery['id']] = False
        
        sorted_route = []
        remaining_deliveries = set(d['id'] for d in deliveries)
        
        # Make TWO full loops maximum (to handle deliveries that need second pass)
        for loop in range(2):
            if not remaining_deliveries:
                break  # All deliveries handled
            
            # Visit rooms in circular order: current → next → ... → current
            for offset in range(1, ROOM_COUNT + 1):
                room = (self.current_location + offset) % (ROOM_COUNT + 1)
                
                # Skip base (Room 0) unless we're delivering there
                if room == 0:
                    continue
                
                room_tasks = []
                
                # Step 1: Add PICKUPS at this room (only if not picked up yet)
                for delivery in deliveries:
                    if (delivery['pickup'] == room and 
                        not pickup_status[delivery['id']] and
                        delivery['id'] in remaining_deliveries):
                        room_tasks.append((delivery, 'pickup'))
                        pickup_status[delivery['id']] = True  # Mark as picked up
                
                # Step 2: Add DELIVERIES at this room (only if already picked up)
                for delivery in deliveries:
                    if (delivery['destination'] == room and 
                        pickup_status[delivery['id']] and
                        delivery['id'] in remaining_deliveries):
                        room_tasks.append((delivery, 'deliver'))
                        remaining_deliveries.discard(delivery['id'])  # Mark as will be delivered
                
                # Only add room to route if there are tasks
                if room_tasks:
                    sorted_route.append((room, room_tasks))
        
        return sorted_route
    
    def handle_pickup(self, delivery):
        """Handle pickup at current location"""
        delivery_id = delivery['id']
        compartment = delivery['compartment']
        
        print(f"\n📦 PICKUP - Delivery {delivery_id}")
        print(f"  Compartment: {compartment}")
        print(f"  To: {delivery['receiver']}")
        
        # Update status
        self.firebase.update_status(delivery_id, 'at_pickup')
        
        # Open compartment
        self.compartments.open_compartment(compartment)
        
        # Reset filesConfirmed to false before waiting
        try:
            url = f"{self.firebase.base_url}/delivery_requests/{delivery_id}.json"
            import requests
            requests.patch(url, json={"filesConfirmed": False})
            print(f"  → Ready for files (filesConfirmed reset)")
        except Exception as e:
            print(f"  ⚠ Could not reset filesConfirmed: {e}")
        
        # Wait for user confirmation
        if self.firebase.wait_for_files_placed(delivery_id):
            # Close compartment
            self.compartments.close_compartment(compartment)
            print(f"  ✓ Pickup complete!\n")
            
            # Update to Stage 1: In Transit
            self.firebase.update_progress_stage(delivery_id, 1)
            
            return True
        else:
            # Timeout - close compartment and CANCEL delivery
            self.compartments.close_compartment(compartment)
            print(f"  ⚠ Pickup timeout - cancelling delivery\n")
            
            # Cancel the delivery in Firebase
            self.firebase.cancel_delivery(delivery_id, "Pickup timeout - sender did not confirm files")
            
            # Free the compartment
            self.firebase.free_compartment(delivery_id, compartment)
            
            return False

    def go_to_pickup(self, delivery):
        """Navigate to pickup location and open compartment"""
        pickup_room = delivery['pickup']
        compartment = delivery['compartment']
        
        print(f"🚚 Going to pickup at Room {pickup_room} (Compartment {compartment})")
        
        # Update Firebase: Stage 0 (Processing)
        self.firebase.update_delivery_stage(delivery['id'], 0)
        
        # Navigate to pickup room
        self.navigate_to(pickup_room)
        
        # Arrived at pickup
        print(f"📍 Arrived at Room {pickup_room}")
        
        # Open compartment for sender
        self.compartments.open(compartment)
        print(f"📂 Compartment {compartment} opened - waiting for files")
        
        # Update state and Firebase
        delivery['state'] = 'at_pickup'
        self.firebase.update_delivery_location(delivery['id'], pickup_room)
        self.firebase.set_files_confirmed(delivery['id'], False)
        
        # Set deadline for file placement (30 seconds from now)
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
            
            # Update state to proceed to destination
            delivery['state'] = 'files_confirmed'
            
            # Update Firebase: Stage 1 (In Transit to destination)
            self.firebase.update_delivery_stage(delivery['id'], 1)
            
        else:
            # Check if deadline exceeded
            if time.time() > delivery.get('confirmation_deadline', float('inf')):
                print(f"⏰ Timeout waiting for file confirmation - {delivery['id']}")
                
                # Notify app about timeout
                self.firebase.notify_confirmation_timeout(delivery['id'])
                
                # Extend deadline (give sender another chance)
                delivery['confirmation_deadline'] = time.time() + self.CONFIRMATION_TIMEOUT
                
                # Track timeout count
                delivery['timeout_count'] = delivery.get('timeout_count', 0) + 1
                
                # After 2 timeouts, cancel delivery
                if delivery['timeout_count'] >= 2:
                    print(f"❌ Cancelling delivery {delivery['id']} - no response from sender")
                    self.cancel_delivery(delivery)
    
    def go_to_destination(self, delivery):
        """Navigate to destination and wait for receiver"""
        destination_room = delivery['destination']
        
        print(f"🚚 Delivering to Room {destination_room}")
        
        # Update Firebase: Stage 1 (In Transit)
        self.firebase.update_delivery_stage(delivery['id'], 1)
        
        # Navigate to destination
        self.navigate_to(destination_room)
        
        # Update Firebase: Stage 2 (Approaching destination)
        self.firebase.update_delivery_stage(delivery['id'], 2)
        
        # Brief pause before arrival
        time.sleep(1)
        
        # Update Firebase: Stage 3 (Arrived - ready for pickup)
        self.firebase.update_delivery_stage(delivery['id'], 3)
        self.firebase.update_delivery_location(delivery['id'], destination_room)
        self.firebase.set_ready_for_pickup(delivery['id'], True)
        
        print(f"📍 Arrived at Room {destination_room}")
        print(f"📦 Waiting for receiver to enter verification code")
        
        # Update state
        delivery['state'] = 'at_destination'
        delivery['arrival_time'] = time.time()
    
    def wait_for_receiver(self, delivery):
        """Wait for receiver to verify code and collect files"""
        # Check if receiver retrieved files
        files_received = self.firebase.get_files_received(delivery['id'])
        
        if files_received:
            print(f"✅ Files received by receiver - delivery {delivery['id']} complete!")
            
            # Close compartment
            self.compartments.close(delivery['compartment'])
            print(f"🔒 Compartment {delivery['compartment']} closed")
            
            # Mark as completed
            delivery['state'] = 'completed'
            self.firebase.mark_delivery_completed(delivery['id'])
            
            # Free the compartment
            self.firebase.free_compartment(delivery['compartment'])
            
            print(f"🎉 Delivery {delivery['id']} successfully completed!\n")

    def navigate_to(self, target_room):
        """Navigate from current location to target room"""
        if self.current_location == target_room:
            print(f"✓ Already at Room {target_room}")
            return
        
        print(f"🗺️ Navigating: Room {self.current_location} → Room {target_room}")
        
        self.is_moving = True
        
        # Start line following
        self.line_follower.start()
        
        # Calculate how many rooms to pass
        rooms_to_pass = self.calculate_rooms_to_pass(self.current_location, target_room)
        rooms_passed = 0
        
        print(f"   📏 Distance: {rooms_to_pass} room(s)")
        
        while rooms_passed < rooms_to_pass:
            # Check WiFi connection during movement
            if not self.firebase.is_connected():
                print("⚠️ WiFi lost during navigation - stopping")
                self.stop_movement()
                self.firebase.reconnect()
                # Resume from current position
                continue
            
            # Check for intersection (room marker detected)
            if self.line_follower.detect_intersection():
                rooms_passed += 1
                self.current_location = self.get_next_room(self.current_location)
                
                print(f"   🏠 Passed Room {self.current_location} ({rooms_passed}/{rooms_to_pass})")
                
                # Brief pause to avoid double-counting same intersection
                time.sleep(0.5)
        
        # Stop at target
        self.line_follower.stop()
        self.is_moving = False
        
        print(f"✅ Reached Room {target_room}\n")
    
    def calculate_rooms_to_pass(self, current, target):
        """Calculate how many room markers to pass (circular track)"""
        # Track layout: 0(Base) -> 1 -> 2 -> 3 -> 4 -> back to 0
        if target >= current:
            return target - current
        else:
            # Wrap around (e.g., from Room 4 back to Base)
            return (4 - current) + target
    
    def get_next_room(self, current):
        """Get the next room number in circular path"""
        next_room = current + 1
        if next_room > 4:
            return 0  # Back to base
        return next_room

    def cancel_delivery(self, delivery):
        """Cancel a delivery and clean up"""
        print(f"🚫 Cancelling delivery {delivery['id']}")
        
        # Close compartment if open
        self.compartments.close(delivery['compartment'])
        
        # Update Firebase
        self.firebase.cancel_delivery(delivery['id'])
        
        # Free compartment
        self.firebase.free_compartment(delivery['compartment'])
        
        # Remove from active list
        if delivery in self.active_deliveries:
            self.active_deliveries.remove(delivery)
    
    def stop_movement(self):
        """Emergency stop all movement"""
        self.line_follower.stop()
        self.is_moving = False
        print("🛑 Emergency stop - all movement halted")
    
    def handle_critical_error(self, error):
        """Handle critical errors and attempt recovery"""
        print(f"🆘 CRITICAL ERROR: {error}")
        
        # Stop all movement immediately
        self.stop_movement()
        
        # Try to save state to Firebase
        try:
            self.firebase.report_error(str(error), self.current_location)
        except:
            print("⚠️ Could not report error to Firebase")
        
        # Wait before attempting to continue
        print("⏳ Waiting 10 seconds before retry...")
        time.sleep(10)
        
    def start(self):
        """Main robot loop - check for deliveries and process them"""
        print("🚀 Starting delivery robot...")
        print("📡 Listening for new delivery requests...\n")
        
        self.current_location = 0
        self.active_deliveries = []
        self.is_moving = False
        self.completed_deliveries = set()
        self.picked_up_deliveries = set()
        self.cancelled_deliveries = set()  # NEW: Track cancelled deliveries
        
        try:
            while self.running:
                # Check for new deliveries
                all_deliveries = self.firebase.get_active_deliveries()
                
                # Filter out completed AND cancelled deliveries
                new_deliveries = [
                    d for d in all_deliveries 
                    if d['id'] not in self.completed_deliveries 
                    and d['id'] not in self.cancelled_deliveries  # Skip cancelled ones
                ]
                
                if new_deliveries:
                    print(f"\n📋 Found {len(new_deliveries)} active delivery request(s)")
                    
                    # Plan route from CURRENT location
                    route = self.plan_route_from_current_location(new_deliveries)
                    
                    if route:
                        # Execute route
                        for room, tasks in route:
                            print(f"\n🗺️ Next stop: Room {room}")
                            
                            # Navigate to room
                            current_delivery_id = tasks[0][0]['id'] if tasks else None
                            self.line_follower.navigate_to_room(room, self.firebase, current_delivery_id)
                            self.current_location = room
                            
                            # Handle all tasks at this room
                            for delivery, action in tasks:
                                if action == 'pickup':
                                    success = self.handle_pickup(delivery)
                                    if success:
                                        self.firebase.update_status(delivery['id'], 'in_progress')
                                        self.picked_up_deliveries.add(delivery['id'])
                                    else:
                                        # Pickup failed - mark as cancelled
                                        self.cancelled_deliveries.add(delivery['id'])
                                
                                elif action == 'deliver':
                                    # SAFETY CHECK: Only deliver if pickup happened
                                    if delivery['id'] not in self.picked_up_deliveries:
                                        print(f"  ⚠ Skipping delivery {delivery['id']} - not picked up yet!")
                                        continue
                                    
                                    # Update to Stage 2 (approaching destination)
                                    self.firebase.update_progress_stage(delivery['id'], 2)
                                    
                                    # Handle the delivery
                                    success = self.handle_delivery(delivery)
                                    
                                    if success:
                                        # Mark as completed
                                        self.firebase.mark_completed(delivery['id'])
                                        self.firebase.free_compartment(delivery['id'], delivery['compartment'])
                                        self.completed_deliveries.add(delivery['id'])
                                        self.picked_up_deliveries.discard(delivery['id'])
                                    else:
                                        # Delivery failed - mark as cancelled
                                        self.cancelled_deliveries.add(delivery['id'])
                                        self.picked_up_deliveries.discard(delivery['id'])
                        
                        print("\n✅ Current route completed!\n")
                    else:
                        print("📍 No deliveries to handle from current location")
                
                else:
                    # No active deliveries - return to base if not already there
                    if self.current_location != 0:
                        print("\n🏠 No active deliveries - returning to base...")
                        self.line_follower.navigate_to_room(0, self.firebase, None)
                        self.current_location = 0
                        self.completed_deliveries.clear()
                        self.picked_up_deliveries.clear()
                        self.cancelled_deliveries.clear()  # Reset cancelled tracking
                
                # Check again in 3 seconds
                sleep(3)
                
        except KeyboardInterrupt:
            print("\n⚠️ Interrupted by user")
        except Exception as e:
            print(f"\n❌ Error in main loop: {e}")
            self.handle_critical_error(e)
        finally:
            self.cleanup()
    
    def handle_delivery(self, delivery):
        """Handle delivery at destination"""
        delivery_id = delivery['id']
        compartment = delivery['compartment']
        
        print(f"\n📬 DELIVERY - {delivery_id}")
        print(f"  Compartment: {compartment}")
        print(f"  For: {delivery['receiver']}")
        
        # Update to Stage 3: Arrived at destination
        self.firebase.update_progress_stage(delivery_id, 3)
        
        # Reset verification flags and set ready for pickup
        try:
            url = f"{self.firebase.base_url}/delivery_requests/{delivery_id}.json"
            import requests
            requests.patch(url, json={
                "readyForPickup": True,
                "codeVerified": False,
                "filesReceived": False
            })
            print(f"  🔐 Waiting for receiver to enter verification code...")
        except Exception as e:
            print(f"  ⚠ Could not set readyForPickup: {e}")
        
        # STEP 1: Wait for receiver to verify code
        if self.firebase.wait_for_verification(delivery_id):
            print(f"  ✅ Code verified! Opening compartment...")
            
            # STEP 2: Open compartment after verification
            self.compartments.open_compartment(compartment)
            
            # STEP 3: Wait for receiver to press "files received" button
            print(f"  ⏳ Waiting for receiver to take files and confirm...")
            if self.wait_for_files_received(delivery_id):
                # STEP 4: Close compartment after receiver confirms
                self.compartments.close_compartment(compartment)
                print(f"  ✓ Delivery complete!\n")
                return True
            else:
                # Timeout waiting for files received - close anyway
                print(f"  ⚠ Timeout - closing compartment anyway")
                self.compartments.close_compartment(compartment)
                return True  # Still consider it complete since they verified
        else:
            # Verification timeout - cancel delivery
            print(f"  ❌ Verification timeout - cancelling delivery\n")
            self.firebase.cancel_delivery(delivery_id, "Delivery timeout - receiver did not verify")
            self.firebase.free_compartment(delivery_id, compartment)
            return False

    def wait_for_files_received(self, delivery_id, timeout=300):
        """Wait for receiver to confirm they took the files"""
        print(f"  ⏳ Waiting for 'files received' confirmation (timeout: {timeout}s)...")
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                url = f"{self.firebase.base_url}/delivery_requests/{delivery_id}.json"
                response = requests.get(url)
                if response.status_code == 200:
                    delivery = response.json()
                    if delivery and delivery.get('filesReceived') == True:
                        print("  ✅ Receiver confirmed files received!")
                        return True
                time.sleep(1)
            except Exception as e:
                print(f"  ⚠ Error checking filesReceived: {e}")
                time.sleep(1)
        
        print("  ⚠ Timeout waiting for files received confirmation!")
        return False
    
# Initialize and start the robot
if __name__ == "__main__":
    print("""
    ╔════════════════════════════════════════════╗
    ║     LALABOT DELIVERY ROBOT SYSTEM          ║
    ║   Autonomous File Delivery Robot v1.0      ║
    ╚════════════════════════════════════════════╝
    """)
    
    try:
        robot = DeliveryRobot()
        robot.start()
    except Exception as e:
        print(f"\n❌ Fatal error during startup: {e}")
        sys.exit(1)

