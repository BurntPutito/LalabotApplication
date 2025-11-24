from gpiozero import Servo, Device
from gpiozero.pins.lgpio import LGPIOFactory
from time import sleep
from config import *
import warnings

warnings.filterwarnings('ignore', category=RuntimeWarning)
Device.pin_factory = LGPIOFactory()

class CompartmentController:
    def __init__(self):
        # Initialize servos with correct pulse widths
        self.servos = {
            1: Servo(SERVO1_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000),
            2: Servo(SERVO2_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000),
            3: Servo(SERVO3_PIN, min_pulse_width=1/1000, max_pulse_width=2/1000)
        }
        
        # Close all compartments on startup
        self.close_all()
        
        # Detach servos after positioning to stop jitter
        sleep(1)
        for servo in self.servos.values():
            servo.detach()
        
        print("✓ Compartment controller initialized (all closed)")
    
    def _angle_to_value(self, angle):
        """Convert angle (0-180) to servo value (-1 to 1)"""
        return (angle - 90) / 90
    
    def open_compartment(self, compartment_num):
        """Open specific compartment (90° → 180°)"""
        if compartment_num in self.servos:
            print(f"  → Opening compartment {compartment_num}")
            servo = self.servos[compartment_num]
            servo.value = self._angle_to_value(SERVO_OPEN)
            sleep(1)  # Wait for servo to reach position
            servo.detach()  # Stop driving to prevent jitter
    
    def close_compartment(self, compartment_num):
        """Close specific compartment (180° → 90°)"""
        if compartment_num in self.servos:
            print(f"  → Closing compartment {compartment_num}")
            servo = self.servos[compartment_num]
            servo.value = self._angle_to_value(SERVO_CLOSED)
            sleep(1)
            servo.detach()  # Stop driving to prevent jitter
    
    def close_all(self):
        """Close all compartments"""
        for num in self.servos:
            self.servos[num].value = self._angle_to_value(SERVO_CLOSED)
        sleep(1)
    
    def cleanup(self):
        """Detach all servos"""
        for servo in self.servos.values():
            try:
                servo.detach()
            except:
                pass
        print("✓ Compartment controller cleaned up")
