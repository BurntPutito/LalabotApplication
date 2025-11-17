# compartment_controller.py - Compartment door control for Raspberry Pi 5
import lgpio
import time
from config import SERVO_CLOSED_DUTY, SERVO_OPEN_DUTY

class CompartmentController:
    def __init__(self):
        # Open GPIO chip
        self.h = lgpio.gpiochip_open(4)
        
        # Servo pins
        self.servo1_pin = 25
        self.servo2_pin = 26
        self.servo3_pin = 33
        
        # Claim as outputs
        lgpio.gpio_claim_output(self.h, self.servo1_pin)
        lgpio.gpio_claim_output(self.h, self.servo2_pin)
        lgpio.gpio_claim_output(self.h, self.servo3_pin)
        
        # Servo PWM frequency (50Hz for servos)
        self.servo_frequency = 50
        
        # Servo duty cycles (from config)
        self.closed_duty = SERVO_CLOSED_DUTY
        self.open_duty = SERVO_OPEN_DUTY
        
        # Initialize all closed
        self.close_all()
        print("✅ Compartment controller initialized")
    
    def open(self, compartment):
        """Open specific compartment (1, 2, or 3)"""
        print(f"📂 Opening compartment {compartment}")
        
        if compartment == 1:
            lgpio.tx_pwm(self.h, self.servo1_pin, self.servo_frequency, self.open_duty)
        elif compartment == 2:
            lgpio.tx_pwm(self.h, self.servo2_pin, self.servo_frequency, self.open_duty)
        elif compartment == 3:
            lgpio.tx_pwm(self.h, self.servo3_pin, self.servo_frequency, self.open_duty)
        else:
            print(f"⚠️ Invalid compartment number: {compartment}")
            return
        
        time.sleep(0.5)  # Wait for servo to move
    
    def close(self, compartment):
        """Close specific compartment (1, 2, or 3)"""
        print(f"🔒 Closing compartment {compartment}")
        
        if compartment == 1:
            lgpio.tx_pwm(self.h, self.servo1_pin, self.servo_frequency, self.closed_duty)
        elif compartment == 2:
            lgpio.tx_pwm(self.h, self.servo2_pin, self.servo_frequency, self.closed_duty)
        elif compartment == 3:
            lgpio.tx_pwm(self.h, self.servo3_pin, self.servo_frequency, self.closed_duty)
        else:
            print(f"⚠️ Invalid compartment number: {compartment}")
            return
        
        time.sleep(0.5)  # Wait for servo to move
    
    def close_all(self):
        """Close all compartments"""
        lgpio.tx_pwm(self.h, self.servo1_pin, self.servo_frequency, self.closed_duty)
        lgpio.tx_pwm(self.h, self.servo2_pin, self.servo_frequency, self.closed_duty)
        lgpio.tx_pwm(self.h, self.servo3_pin, self.servo_frequency, self.closed_duty)
        time.sleep(0.5)
    
    def cleanup(self):
        """Cleanup GPIO resources"""
        self.close_all()
        lgpio.gpiochip_close(self.h)