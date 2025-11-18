import lgpio as GPIO
import time
from config import *

class ObstacleDetector:
    def __init__(self):
        self.h = GPIO.gpiochip_open(0)
        GPIO.gpio_claim_output(self.h, TRIG)
        GPIO.gpio_claim_input(self.h, ECHO)
        print("✓ Obstacle detector initialized")
    
    def get_distance(self):
        """Measure distance in centimeters"""
        GPIO.gpio_write(self.h, TRIG, 0)
        time.sleep(0.00001)
        GPIO.gpio_write(self.h, TRIG, 1)
        time.sleep(0.00001)
        GPIO.gpio_write(self.h, TRIG, 0)
        
        timeout = time.time() + 0.1
        pulse_start = time.time()
        while GPIO.gpio_read(self.h, ECHO) == 0:
            pulse_start = time.time()
            if pulse_start > timeout:
                return None
        
        timeout = time.time() + 0.1
        pulse_end = time.time()
        while GPIO.gpio_read(self.h, ECHO) == 1:
            pulse_end = time.time()
            if pulse_end > timeout:
                return None
        
        pulse_duration = pulse_end - pulse_start
        distance = round(pulse_duration * 17150, 2)
        
        return distance if 2 <= distance <= 400 else None
    
    def is_path_clear(self):
        """Check if path is clear (no obstacle within threshold)"""
        dist = self.get_distance()
        if dist is None:
            return True  # Assume clear if sensor fails
        return dist > OBSTACLE_DISTANCE
    
    def cleanup(self):
        try:
            GPIO.gpiochip_close(self.h)
        except:
            pass
        print("✓ Obstacle detector cleaned up")
