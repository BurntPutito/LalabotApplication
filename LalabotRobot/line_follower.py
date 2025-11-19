import lgpio as GPIO
import time
from config import *

class LineFollower:
    def __init__(self, motor_controller, obstacle_detector):
        self.h = GPIO.gpiochip_open(0)
        self.motors = motor_controller
        self.obstacle_detector = obstacle_detector  # Add obstacle detector
        self.current_location = 0
        
        # Setup IR sensors
        GPIO.gpio_claim_input(self.h, IR_LEFT)
        GPIO.gpio_claim_input(self.h, IR_CENTER)
        GPIO.gpio_claim_input(self.h, IR_RIGHT)
        
        self.white_line_detected = False
        self.last_valid_reading = None
        
        print("✓ Line follower initialized at Room 0 (Base)")
    
    def read_sensors(self):
        """Read IR sensors (0 = black/object detected, 1 = white/no object)"""
        left = GPIO.gpio_read(self.h, IR_LEFT)
        center = GPIO.gpio_read(self.h, IR_CENTER)
        right = GPIO.gpio_read(self.h, IR_RIGHT)
        return left, center, right
    
    def is_white_line(self):
        """Detect if all 3 sensors see white (room marker)"""
        left, center, right = self.read_sensors()
        # White line = all sensors read 1 (no black line detected)
        white_count = (left == 1) + (center == 1) + (right == 1)
        
        # Debug print
        if white_count >= WHITE_LINE_THRESHOLD:
            print(f"  [DEBUG] White line detected: L={left} C={center} R={right}")
        
        return white_count >= WHITE_LINE_THRESHOLD
    
    def debug_sensors(self):
        """Print sensor readings for debugging"""
        left, center, right = self.read_sensors()
        
        # Visual representation
        l_char = "█" if left == 0 else "░"
        c_char = "█" if center == 0 else "░"
        r_char = "█" if right == 0 else "░"
        
        print(f"[{l_char}] [{c_char}] [{r_char}]  (L={left} C={center} R={right})")
    
    def follow_line(self):
        """Follow black line using IR sensors with obstacle detection"""
        # Check for obstacles FIRST
        if not self.obstacle_detector.is_path_clear():
            distance = self.obstacle_detector.get_distance()
            print(f"⚠ OBSTACLE DETECTED! Distance: {distance}cm - STOPPING")
            self.motors.stop()
            time.sleep(0.5)
            return
        
        left, center, right = self.read_sensors()
        
        # 0 = on black line, 1 = off black line
        
        # PRIORITY 1: Perfect alignment - only center sees black
        if left == 1 and center == 0 and right == 1:
            self.motors.forward()
            self.last_valid_reading = 'center'
        
        # PRIORITY 2: Slight left deviation - left + center on black
        elif left == 0 and center == 0 and right == 1:
            self.motors.turn_left()
            self.last_valid_reading = 'left'
        
        # PRIORITY 3: Slight right deviation - right + center on black
        elif left == 1 and center == 0 and right == 0:
            self.motors.turn_right()
            self.last_valid_reading = 'right'
        
        # PRIORITY 4: Strong left deviation - only left sees black
        elif left == 0 and center == 1 and right == 1:
            self.motors.turn_left()
            self.last_valid_reading = 'left'
        
        # PRIORITY 5: Strong right deviation - only right sees black
        elif left == 1 and center == 1 and right == 0:
            self.motors.turn_right()
            self.last_valid_reading = 'right'
        
        # PRIORITY 6: All on black (wide line or intersection)
        elif left == 0 and center == 0 and right == 0:
            # Continue in last known direction
            if self.last_valid_reading == 'left':
                self.motors.turn_left()
            elif self.last_valid_reading == 'right':
                self.motors.turn_right()
            else:
                self.motors.forward()  # Default to forward
        
        # All white (lost line or white marker)
        elif left == 1 and center == 1 and right == 1:
            # White line marker - let navigate_to_room() handle it
            pass
        
        # Lost line - stop briefly
        else:
            print("⚠ Lost line!")
            self.motors.stop()
            time.sleep(0.1)
            
        # Add to line_follower.py for testing
    def test_line_following(self):
        """Test line following for 10 seconds"""
        print("Testing line following...")
        start = time.time()
        
        while time.time() - start < 10:
            left, center, right = self.read_sensors()
            print(f"L={left} C={center} R={right}", end=" → ")
            
            if left == 1 and center == 0 and right == 1:
                print("FORWARD (perfect)")
                self.motors.forward()
            elif left == 0 and center == 0:
                print("TURN LEFT (slight)")
                self.motors.turn_left()
            elif center == 0 and right == 0:
                print("TURN RIGHT (slight)")
                self.motors.turn_right()
            elif left == 0 and center == 1:
                print("TURN LEFT (strong)")
                self.motors.turn_left()
            elif center == 1 and right == 0:
                print("TURN RIGHT (strong)")
                self.motors.turn_right()
            else:
                print("FORWARD (all black)")
                self.motors.forward()
            
            time.sleep(0.1)
        
        self.motors.stop()
    
    def navigate_to_room(self, target_room, firebase_handler, delivery_id):
        """Navigate to target room, updating Firebase along the way"""
        print(f"\n🎯 Navigating from Room {self.current_location} → Room {target_room}")
        
        rooms_to_pass = self.calculate_rooms_to_pass(target_room)
        print(f"  📏 Need to pass {rooms_to_pass} room(s)")
        
        rooms_passed = 0
        self.white_line_detected = False
        
        while rooms_passed < rooms_to_pass:
            # Debug sensors every second
            if int(time.time()) % 1 == 0:
                self.debug_sensors()
            # Check for obstacles during navigation
            if not self.obstacle_detector.is_path_clear():
                distance = self.obstacle_detector.get_distance()
                print(f"⚠ OBSTACLE! Distance: {distance}cm - Waiting...")
                self.motors.stop()
                time.sleep(1)  # Wait 1 second for obstacle to clear
                continue  # Skip this iteration and check again
            
            # Follow line
            self.follow_line()
            
            # Check for white line (room marker)
            if self.is_white_line() and not self.white_line_detected:
                print(f"  🏁 White line detected!")
                self.white_line_detected = True
                self.motors.stop()
                
                # Increment room counter
                self.current_location = (self.current_location + 1) % (ROOM_COUNT + 1)
                rooms_passed += 1
                print(f"  ✓ Passed Room {self.current_location} ({rooms_passed}/{rooms_to_pass})")
                
                # Update Firebase if delivery_id provided
                if delivery_id is not None:
                    firebase_handler.update_current_location(delivery_id, self.current_location)
                
                # Move past white line to avoid re-detection
                print(f"  → Moving past white line...")
                self.motors.forward()
                time.sleep(1.0)  # Increased time to fully pass white line
                self.motors.stop()
                
                # Reset detection flag
                self.white_line_detected = False
                time.sleep(0.3)
            
            # Small delay to prevent overwhelming CPU
            time.sleep(0.05)
        
        print(f"✓ Arrived at Room {target_room}\n")
        self.motors.stop()
    
    def calculate_rooms_to_pass(self, target_room):
        """Calculate how many rooms to pass from current location"""
        if target_room >= self.current_location:
            return target_room - self.current_location
        else:
            # Wrap around (e.g., from Room 4 to Room 0)
            return (ROOM_COUNT + 1) - self.current_location + target_room
    
    def cleanup(self):
        self.motors.stop()
        try:
            GPIO.gpiochip_close(self.h)
        except:
            pass
        print("✓ Line follower cleaned up")
