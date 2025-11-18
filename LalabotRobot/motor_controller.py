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
        # Set direction: both motors forward
        GPIO.gpio_write(self.h, AIN1, 1)
        GPIO.gpio_write(self.h, AIN2, 0)
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
        """Turn left - stop left motor, run right motor"""
        # Left motor: stop
        GPIO.gpio_write(self.h, AIN1, 0)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, PWMA, 0)
        
        # Right motor: forward
        GPIO.gpio_write(self.h, BIN1, 1)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMB, 1)
    
    def turn_right(self, speed=1):
        """Turn right - run left motor, stop right motor"""
        # Left motor: forward
        GPIO.gpio_write(self.h, AIN1, 1)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, PWMA, 1)
        
        # Right motor: stop
        GPIO.gpio_write(self.h, BIN1, 0)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMB, 0)
    
    def cleanup(self):
        """Cleanup GPIO"""
        self.stop()
        GPIO.gpio_write(self.h, STBY, 0)
        try:
            GPIO.gpiochip_close(self.h)
        except:
            pass
        print("✓ Motor controller cleaned up")
