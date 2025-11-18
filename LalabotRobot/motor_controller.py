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
    
    def forward(self, speed=MOTOR_SPEED):
        """Move forward"""
        GPIO.gpio_write(self.h, AIN1, 1)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, BIN1, 1)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMA, int(speed))
        GPIO.gpio_write(self.h, PWMB, int(speed))
    
    def stop(self):
        """Stop both motors"""
        GPIO.gpio_write(self.h, PWMA, 0)
        GPIO.gpio_write(self.h, PWMB, 0)
    
    def turn_left(self, speed=MOTOR_SPEED):
        """Turn left (only right motor moves forward)"""
        GPIO.gpio_write(self.h, AIN1, 0)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, PWMA, 0)
        
        GPIO.gpio_write(self.h, BIN1, 1)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMB, int(speed))
    
    def turn_right(self, speed=MOTOR_SPEED):
        """Turn right (only left motor moves forward)"""
        GPIO.gpio_write(self.h, AIN1, 1)
        GPIO.gpio_write(self.h, AIN2, 0)
        GPIO.gpio_write(self.h, PWMA, int(speed))
        
        GPIO.gpio_write(self.h, BIN1, 0)
        GPIO.gpio_write(self.h, BIN2, 0)
        GPIO.gpio_write(self.h, PWMB, 0)
    
    def cleanup(self):
        """Cleanup GPIO"""
        self.stop()
        GPIO.gpio_write(self.h, STBY, 0)
        GPIO.gpiochip_close(self.h)
        print("✓ Motor controller cleaned up")
