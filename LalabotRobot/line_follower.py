
import lgpio as GPIO
import time
from config import *

class LineFollower:
    def __init__(self, motor_controller):
        self.h = GPIO.gpiochip_open(0)
        self.motors = motor_controller
        self.current_location = 0
        
        # Setup IR sensors
        GPIO.gpio_claim_input(self.h, IR_LEFT)
        GPIO.gpio_claim_input(self.h, IR_CENTER)
        GPIO.gpio_claim_input(self.h, IR_RIGHT)
        
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
        return white_count >= WHITE_LINE_THRESHOLD
    
    def follow_line(self):
        """Follow black line using IR sensors"""
        left, center, right = self.read_sensors()
        
        # 0 = on black line, 1 = off black line
        if center == 0:
            # On track - go forward
            self.motors.forward()
        elif left == 0:
            # Line is to the left - turn left
            self.motors.turn_left()
        elif right == 0:
            # Line is to the right - turn right
            self.motors.turn_right()
        else:
            # Lost line - stop
            self.motors.stop()
            print("⚠ Warning: Lost line!")
    
    def navigate_to_room(self, target_room, firebase_handler, delivery_id):
        """Navigate to target room, updating Firebase along the way"""
        print(f"\n🎯 Navigating from Room {self.current_location} → Room {target_room}")
        
        while self.current_location != target_room:
            # Follow line
            self.follow_line()
            
            # Check for white line (room marker)
            if self.is_white_line():
                self.motors.stop()
                
                # Move past white line to avoid re-detection
                time.sleep(0.2)
                self.motors.forward()
                time.sleep(0.5)
                self.motors.stop()
                
                # Increment location (circular: 0→1→2→3→4→0→...)
                self.current_location = (self.current_location + 1) % (ROOM_COUNT + 1)
                print(f"  ✓ Passed Room {self.current_location}")
                
                # Update Firebase ONLY if delivery_id is provided
                if delivery_id is not None:
                    firebase_handler.update_current_location(delivery_id, self.current_location)
                
                # Brief pause before continuing
                time.sleep(0.5)
        
        print(f"✓ Arrived at Room {target_room}\n")
        self.motors.stop()
    
    def cleanup(self):
        self.motors.stop()
        try:
            GPIO.gpiochip_close(self.h)
        except:
            pass #handle already closed
        print("✓ Line follower cleaned up")

