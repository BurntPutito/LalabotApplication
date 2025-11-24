import lgpio as GPIO
import time
from config import *

class MotorController:
    def __init__(self):
        self.h = GPIO.gpiochip_open(0)
        
        # Setup motor pins
        GPIO.gpio_claim_output(self.h, PWMA)
        GPIO.gpio_claim_output(self.h, AIN1)
        GPIO.gpio_claim_output(self.h, AIN2)
        GPIO.gpio_claim_output(self.h, PWMB)
        GPIO.gpio_claim_output(self.h, BIN1)
        GPIO.gpio_claim_output(self.h, BIN2)
        GPIO.gpio_claim_output(self.h, STBY)
        
        # Enable motor driver
        GPIO.gpio_write(self.h, STBY, 1)
        
        print("✓ Motor controller initialized")
    
    def forward(self, speed=1):
        """Move forward at full speed"""
        # Motor A (left) is reversed - forward is 0,1
        GPIO.gpio_write(self.h, AIN1, 0)
        GPIO.gpio_write(self.h, AIN2, 1)
        
        # Motor B (right) normal - forward is 1,0
        GPIO.gpio_write(self.h, BIN1, 1)
        GPIO.gpio_write(self.h, BIN2, 0)
        
        # Enable motors at full speed
        GPIO.gpio_write(self.h, PWMA, 1)
        GPIO.gpio_write(self.h, PWMB, 1)
    
    def stop(self):
        """Stop both motors"""
        GPIO.gpio_write(self.h, PWMA, 0)
        GPIO.gpio_write(self.h, PWMB, 0)
    
    def turn_left(self, speed=1):
        """Turn left - left motor backward, right motor forward (sharp turn)"""
        # Motor A (left) reversed - backward is 1,0
        GPIO.gpio_write(self.h, AIN1, 1)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, PWMA, 1)
        
        # Motor B (right) forward - forward is 1,0
        GPIO.gpio_write(self.h, BIN1, 1)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMB, 1)
    
    def turn_right(self, speed=1):
        """Turn right - left motor forward, right motor backward (sharp turn)"""
        # Motor A (left) reversed - forward is 0,1
        GPIO.gpio_write(self.h, AIN1, 0)
        GPIO.gpio_write(self.h, AIN2, 1)
        GPIO.gpio_write(self.h, PWMA, 1)
        
        # Motor B (right) backward - backward is 0,1
        GPIO.gpio_write(self.h, BIN1, 0)
        GPIO.gpio_write(self.h, BIN2, 1)
        GPIO.gpio_write(self.h, PWMB, 1)
    
    def cleanup(self):
        """Cleanup GPIO"""
        self.stop()
        GPIO.gpio_write(self.h, STBY, 0)
        try:
            GPIO.gpiochip_close(self.h)
        except:
            pass
        print("✓ Motor controller cleaned up")
