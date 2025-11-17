# line_follower.py
from machine import Pin, PWM # pyright: ignore[reportMissingImports]
import time

class LineFollower:
    def __init__(self):
        # IR Sensors - CORRECTED PINS
        self.sensor_left = Pin(12, Pin.IN)
        self.sensor_center = Pin(14, Pin.IN)
        self.sensor_right = Pin(27, Pin.IN)
        
        # L298N Motor Driver - CORRECTED PINS
        # Left Motor
        self.motor_left_in1 = Pin(16, Pin.OUT)
        self.motor_left_in2 = Pin(17, Pin.OUT)
        self.motor_left_pwm = PWM(Pin(4))
        
        # Right Motor
        self.motor_right_in3 = Pin(18, Pin.OUT)
        self.motor_right_in4 = Pin(19, Pin.OUT)
        self.motor_right_pwm = PWM(Pin(5))
        
        # Set PWM frequency
        self.motor_left_pwm.freq(1000)
        self.motor_right_pwm.freq(1000)
        
        # Speed settings
        self.base_speed = 50000  # Adjust based on testing (0-65535 for duty_u16)
        self.turn_speed = 35000
        
        self.running = False
        
    def start(self):
        """Start line following"""
        self.running = True
        
        while self.running:
            left = self.sensor_left.value()
            center = self.sensor_center.value()
            right = self.sensor_right.value()
            
            # 0 = Black (line), 1 = White (background)
            
            if center == 0:  # On line
                self.move_forward()
            elif left == 0:  # Line on left
                self.turn_left()
            elif right == 0:  # Line on right
                self.turn_right()
            else:  # Lost line
                self.find_line()
            
            time.sleep(0.01)  # Small delay for stability
    
    def detect_intersection(self):
        """Detect intersection (all sensors see black)"""
        left = self.sensor_left.value()
        center = self.sensor_center.value()
        right = self.sensor_right.value()
        
        # All black = intersection/room marker
        return left == 0 and center == 0 and right == 0
    
    def move_forward(self):
        """Move straight - both motors forward"""
        # Left motor forward
        self.motor_left_in1.value(1)
        self.motor_left_in2.value(0)
        self.motor_left_pwm.duty_u16(self.base_speed)
        
        # Right motor forward
        self.motor_right_in3.value(1)
        self.motor_right_in4.value(0)
        self.motor_right_pwm.duty_u16(self.base_speed)
    
    def turn_left(self):
        """Turn left - slow down left motor"""
        # Left motor slower
        self.motor_left_in1.value(1)
        self.motor_left_in2.value(0)
        self.motor_left_pwm.duty_u16(self.turn_speed)
        
        # Right motor normal speed
        self.motor_right_in3.value(1)
        self.motor_right_in4.value(0)
        self.motor_right_pwm.duty_u16(self.base_speed)
    
    def turn_right(self):
        """Turn right - slow down right motor"""
        # Left motor normal speed
        self.motor_left_in1.value(1)
        self.motor_left_in2.value(0)
        self.motor_left_pwm.duty_u16(self.base_speed)
        
        # Right motor slower
        self.motor_right_in3.value(1)
        self.motor_right_in4.value(0)
        self.motor_right_pwm.duty_u16(self.turn_speed)
    
    def find_line(self):
        """Search for lost line - rotate in place"""
        # Left motor backward
        self.motor_left_in1.value(0)
        self.motor_left_in2.value(1)
        self.motor_left_pwm.duty_u16(self.turn_speed // 2)
        
        # Right motor forward
        self.motor_right_in3.value(1)
        self.motor_right_in4.value(0)
        self.motor_right_pwm.duty_u16(self.turn_speed // 2)
    
    def stop(self):
        """Stop all movement"""
        self.running = False
        self.motor_left_pwm.duty_u16(0)
        self.motor_right_pwm.duty_u16(0)
        
        # Brake - both directions off
        self.motor_left_in1.value(0)
        self.motor_left_in2.value(0)
        self.motor_right_in3.value(0)
        self.motor_right_in4.value(0)