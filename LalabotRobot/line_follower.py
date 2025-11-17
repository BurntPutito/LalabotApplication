# line_follower.py - Line following controller for Raspberry Pi 5
import lgpio
import time
from config import BASE_SPEED, TURN_SPEED

class LineFollower:
    def __init__(self):
        # Open GPIO chip for RPi 5
        self.h = lgpio.gpiochip_open(4)
        
        # IR Sensor Pins
        self.sensor_left_pin = 12
        self.sensor_center_pin = 14
        self.sensor_right_pin = 27
        
        # Set as inputs
        lgpio.gpio_claim_input(self.h, self.sensor_left_pin)
        lgpio.gpio_claim_input(self.h, self.sensor_center_pin)
        lgpio.gpio_claim_input(self.h, self.sensor_right_pin)
        
        # L298N Motor Driver Pins
        self.motor_left_in1 = 16
        self.motor_left_in2 = 17
        self.motor_left_ena = 4
        
        self.motor_right_in3 = 18
        self.motor_right_in4 = 19
        self.motor_right_enb = 5
        
        # Set motor pins as outputs
        lgpio.gpio_claim_output(self.h, self.motor_left_in1)
        lgpio.gpio_claim_output(self.h, self.motor_left_in2)
        lgpio.gpio_claim_output(self.h, self.motor_left_ena)
        lgpio.gpio_claim_output(self.h, self.motor_right_in3)
        lgpio.gpio_claim_output(self.h, self.motor_right_in4)
        lgpio.gpio_claim_output(self.h, self.motor_right_enb)
        
        # PWM settings
        self.pwm_frequency = 1000  # 1kHz
        self.base_speed = BASE_SPEED
        self.turn_speed = TURN_SPEED
        
        self.running = False
        print("✅ Line follower initialized")
        
    def start(self):
        """Start line following"""
        self.running = True
        
        while self.running:
            left = lgpio.gpio_read(self.h, self.sensor_left_pin)
            center = lgpio.gpio_read(self.h, self.sensor_center_pin)
            right = lgpio.gpio_read(self.h, self.sensor_right_pin)
            
            # 0 = Black (line), 1 = White (background)
            
            if center == 0:  # On line
                self.move_forward()
            elif left == 0:  # Line on left
                self.turn_left()
            elif right == 0:  # Line on right
                self.turn_right()
            else:  # Lost line
                self.find_line()
            
            time.sleep(0.01)
    
    def detect_intersection(self):
        """Detect intersection (all sensors see black) - used for room counting"""
        left = lgpio.gpio_read(self.h, self.sensor_left_pin)
        center = lgpio.gpio_read(self.h, self.sensor_center_pin)
        right = lgpio.gpio_read(self.h, self.sensor_right_pin)
        
        # All black = intersection/room marker
        return left == 0 and center == 0 and right == 0
    
    def all_sensors_white(self):
        """Check if all sensors see white (robot lifted)"""
        left = lgpio.gpio_read(self.h, self.sensor_left_pin)
        center = lgpio.gpio_read(self.h, self.sensor_center_pin)
        right = lgpio.gpio_read(self.h, self.sensor_right_pin)
        
        return left == 1 and center == 1 and right == 1
    
    def move_forward(self):
        """Move straight - both motors forward"""
        # Left motor forward
        lgpio.gpio_write(self.h, self.motor_left_in1, 1)
        lgpio.gpio_write(self.h, self.motor_left_in2, 0)
        lgpio.tx_pwm(self.h, self.motor_left_ena, self.pwm_frequency, self.base_speed)
        
        # Right motor forward
        lgpio.gpio_write(self.h, self.motor_right_in3, 1)
        lgpio.gpio_write(self.h, self.motor_right_in4, 0)
        lgpio.tx_pwm(self.h, self.motor_right_enb, self.pwm_frequency, self.base_speed)
    
    def turn_left(self):
        """Turn left - slow down left motor"""
        # Left motor slower
        lgpio.gpio_write(self.h, self.motor_left_in1, 1)
        lgpio.gpio_write(self.h, self.motor_left_in2, 0)
        lgpio.tx_pwm(self.h, self.motor_left_ena, self.pwm_frequency, self.turn_speed)
        
        # Right motor normal
        lgpio.gpio_write(self.h, self.motor_right_in3, 1)
        lgpio.gpio_write(self.h, self.motor_right_in4, 0)
        lgpio.tx_pwm(self.h, self.motor_right_enb, self.pwm_frequency, self.base_speed)
    
    def turn_right(self):
        """Turn right - slow down right motor"""
        # Left motor normal
        lgpio.gpio_write(self.h, self.motor_left_in1, 1)
        lgpio.gpio_write(self.h, self.motor_left_in2, 0)
        lgpio.tx_pwm(self.h, self.motor_left_ena, self.pwm_frequency, self.base_speed)
        
        # Right motor slower
        lgpio.gpio_write(self.h, self.motor_right_in3, 1)
        lgpio.gpio_write(self.h, self.motor_right_in4, 0)
        lgpio.tx_pwm(self.h, self.motor_right_enb, self.pwm_frequency, self.turn_speed)
    
    def find_line(self):
        """Search for lost line - rotate in place"""
        # Left motor backward
        lgpio.gpio_write(self.h, self.motor_left_in1, 0)
        lgpio.gpio_write(self.h, self.motor_left_in2, 1)
        lgpio.tx_pwm(self.h, self.motor_left_ena, self.pwm_frequency, self.turn_speed // 2)
        
        # Right motor forward
        lgpio.gpio_write(self.h, self.motor_right_in3, 1)
        lgpio.gpio_write(self.h, self.motor_right_in4, 0)
        lgpio.tx_pwm(self.h, self.motor_right_enb, self.pwm_frequency, self.turn_speed // 2)
    
    def stop(self):
        """Stop all movement"""
        self.running = False
        
        # Stop PWM
        lgpio.tx_pwm(self.h, self.motor_left_ena, 0, 0)
        lgpio.tx_pwm(self.h, self.motor_right_enb, 0, 0)
        
        # Brake - all pins low
        lgpio.gpio_write(self.h, self.motor_left_in1, 0)
        lgpio.gpio_write(self.h, self.motor_left_in2, 0)
        lgpio.gpio_write(self.h, self.motor_right_in3, 0)
        lgpio.gpio_write(self.h, self.motor_right_in4, 0)
    
    def cleanup(self):
        """Cleanup GPIO resources"""
        self.stop()
        lgpio.gpiochip_close(self.h)