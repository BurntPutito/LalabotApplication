import pigpio
from time import sleep
from config import *

class CompartmentController:
    def __init__(self):
        self.pi = pigpio.pi()
        
        if not self.pi.connected:
            raise Exception("Failed to connect to pigpio daemon")
        
        self.servos = {
            1: SERVO1_PIN,
            2: SERVO2_PIN,
            3: SERVO3_PIN
        }
        
        # Close all compartments on startup
        self.close_all()
        sleep(1)
        
        print("✓ Compartment controller initialized (all closed)")
    
    def _set_angle(self, pin, angle):
        """Set servo angle (0-180°)"""
        # Convert angle to pulse width (500-2500 microseconds)
        pulse_width = 500 + (angle / 180.0) * 2000
        self.pi.set_servo_pulsewidth(pin, pulse_width)
        sleep(0.5)  # Wait for servo to reach position
    
    def open_compartment(self, compartment_num):
        """Open specific compartment"""
        if compartment_num in self.servos:
            print(f"  → Opening compartment {compartment_num}")
            self._set_angle(self.servos[compartment_num], SERVO_OPEN)
    
    def close_compartment(self, compartment_num):
        """Close specific compartment"""
        if compartment_num in self.servos:
            print(f"  → Closing compartment {compartment_num}")
            self._set_angle(self.servos[compartment_num], SERVO_CLOSED)
    
    def close_all(self):
        """Close all compartments"""
        for num in self.servos:
            self._set_angle(self.servos[num], SERVO_CLOSED)
    
    def cleanup(self):
        """Stop all servos and cleanup"""
        for pin in self.servos.values():
            self.pi.set_servo_pulsewidth(pin, 0)  # Turn off PWM
        self.pi.stop()
        print("✓ Compartment controller cleaned up")
