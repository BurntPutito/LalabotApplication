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
    
    def follow_line(self):
        """Follow black line using IR sensors with obstacle detection"""
        # Check for obstacles FIRST before moving
        if not self.obstacle_detector.is_path_clear():
            distance = self.obstacle_detector.get_distance()
            print(f"⚠ OBSTACLE DETECTED! Distance: {distance}cm - STOPPING")
            self.motors.stop()
            time.sleep(0.5)
            return  # Don't proceed with movement
        
        left, center, right = self.read_sensors()
        
        # 0 = on black line, 1 = off black line (white surface)
        
        if center == 0:
            # Center sensor on black line - go straight
            self.motors.forward()
            self.last_valid_reading = 'center'
            
        elif left == 0 and right == 1:
            # Only left sensor sees black - turn left
            self.motors.turn_left()
            self.last_valid_reading = 'left'
            
        elif right == 0 and left == 1:
            # Only right sensor sees black - turn right
            self.motors.turn_right()
            self.last_valid_reading = 'right'
            
        elif left == 0 and center == 0:
            # Left + center on black - turn left slightly
            self.motors.turn_left()
            self.last_valid_reading = 'left'
            
        elif right == 0 and center == 0:
            # Right + center on black - turn right slightly
            self.motors.turn_right()
            self.last_valid_reading = 'right'
            
        else:
            # All sensors see white or lost line
            if left == 1 and center == 1 and right == 1:
                # This is a white line marker - do nothing, let navigate_to_room() handle it
                pass
            else:
                # Lost line - stop briefly
                print("⚠ Lost line!")
                self.motors.stop()
                time.sleep(0.2)
    
    def navigate_to_room(self, target_room, firebase_handler, delivery_id):
        """Navigate to target room, updating Firebase along the way"""
        print(f"\n🎯 Navigating from Room {self.current_location} → Room {target_room}")
        
        rooms_to_pass = self.calculate_rooms_to_pass(target_room)
        print(f"  📏 Need to pass {rooms_to_pass} room(s)")
        
        rooms_passed = 0
        self.white_line_detected = False
        
        while rooms_passed < rooms_to_pass:
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
