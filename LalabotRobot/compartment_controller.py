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
        
        # Track compartment states
        self.states = {1: "closed", 2: "closed", 3: "closed"}
        
        # Close all compartments on startup
        print("🔒 Closing all compartments on startup...")
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
        """Open specific compartment ONLY if it's currently closed"""
        if compartment_num in self.servos:
            # Check current state
            if self.states[compartment_num] == "open":
                print(f"  ⚠️ Compartment {compartment_num} is already open - skipping")
                return
            
            print(f"  📂 Opening compartment {compartment_num}")
            servo = self.servos[compartment_num]
            servo.value = self._angle_to_value(SERVO_OPEN)
            sleep(1.5)  # Increased wait time
            servo.detach()
            
            # Update state
            self.states[compartment_num] = "open"
            print(f"  ✓ Compartment {compartment_num} is now OPEN")

    def close_compartment(self, compartment_num):
        """Close specific compartment ONLY if it's currently open"""
        if compartment_num in self.servos:
            # Check current state
            if self.states[compartment_num] == "closed":
                print(f"  ⚠️ Compartment {compartment_num} is already closed - skipping")
                return
            
            print(f"  🔒 Closing compartment {compartment_num}")
            servo = self.servos[compartment_num]
            servo.value = self._angle_to_value(SERVO_CLOSED)
            sleep(1.5)  # Increased wait time
            servo.detach()
            
            # Update state
            self.states[compartment_num] = "closed"
            print(f"  ✓ Compartment {compartment_num} is now CLOSED")
    
    def close_all(self):
        """Close all compartments"""
        for num in self.servos:
            servo = self.servos[num]
            servo.value = self._angle_to_value(SERVO_CLOSED)
            self.states[num] = "closed"
        sleep(1)
    
    def cleanup(self):
        """Close all and detach all servos"""
        print("🧹 Closing all compartments before cleanup...")
        self.close_all()
        for servo in self.servos.values():
            try:
                servo.detach()
            except:
                pass
        print("✓ Compartment controller cleaned up")
