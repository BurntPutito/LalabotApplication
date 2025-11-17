# compartment_controller.py
from machine import Pin, PWM
import time

class CompartmentController:
    def __init__(self):
        # Servo motors for each compartment - CORRECTED PINS
        self.servo1 = PWM(Pin(25))
        self.servo2 = PWM(Pin(26))
        self.servo3 = PWM(Pin(33))
        
        # Set PWM frequency for servos (50Hz standard)
        self.servo1.freq(50)
        self.servo2.freq(50)
        self.servo3.freq(50)
        
        # Servo positions (duty cycle for SG90)
        # These values work for most SG90 servos, but test and adjust if needed
        self.closed_position = 2500  # ~0 degrees (closed)
        self.open_position = 7500     # ~180 degrees (open)
        
        # Initialize all closed
        self.close_all()
        print("✅ Compartment controller initialized")
    
    def open(self, compartment):
        """Open specific compartment"""
        print(f"📂 Opening compartment {compartment}")
        
        if compartment == 1:
            self.servo1.duty_u16(self.open_position)
        elif compartment == 2:
            self.servo2.duty_u16(self.open_position)
        elif compartment == 3:
            self.servo3.duty_u16(self.open_position)
        
        time.sleep(0.5)  # Wait for servo to move
    
    def close(self, compartment):
        """Close specific compartment"""
        print(f"🔒 Closing compartment {compartment}")
        
        if compartment == 1:
            self.servo1.duty_u16(self.closed_position)
        elif compartment == 2:
            self.servo2.duty_u16(self.closed_position)
        elif compartment == 3:
            self.servo3.duty_u16(self.closed_position)
        
        time.sleep(0.5)  # Wait for servo to move
    
    def close_all(self):
        """Close all compartments"""
        self.servo1.duty_u16(self.closed_position)
        self.servo2.duty_u16(self.closed_position)
        self.servo3.duty_u16(self.closed_position)
        time.sleep(0.5)