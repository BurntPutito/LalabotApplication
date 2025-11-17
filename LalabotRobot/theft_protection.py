# theft_protection.py - Theft detection and alarm system
import time
import lgpio
from config import THEFT_OFF_LINE_THRESHOLD, WIFI_CHECK_INTERVAL, BUZZER_PIN

class TheftProtection:
    def __init__(self, line_follower, firebase, gpio_handle):
        self.line_follower = line_follower
        self.firebase = firebase
        self.h = gpio_handle
        
        # Buzzer setup
        self.buzzer_pin = BUZZER_PIN
        lgpio.gpio_claim_output(self.h, self.buzzer_pin)
        lgpio.gpio_write(self.h, self.buzzer_pin, 0)  # Off initially
        
        # Thresholds
        self.off_line_duration = 0
        self.off_line_threshold = THEFT_OFF_LINE_THRESHOLD
        self.wifi_check_interval = WIFI_CHECK_INTERVAL
        self.last_wifi_check = time.time()
        
        self.alarm_active = False
        print("✅ Theft protection initialized")
    
    def check_for_theft(self, is_robot_moving):
        """Main theft detection logic - returns True if theft detected"""
        
        # Check 1: Line sensors (only when not moving)
        if not is_robot_moving:
            if self.check_lifted():
                self.trigger_alarm("Robot lifted off track")
                return True
        
        # Check 2: WiFi connection (periodic check)
        current_time = time.time()
        if current_time - self.last_wifi_check > self.wifi_check_interval:
            self.last_wifi_check = current_time
            
            if not self.firebase.is_connected():
                time_disconnected = current_time - self.firebase.last_connected_time
                
                if time_disconnected > 30:  # 30 seconds without WiFi
                    self.trigger_alarm("WiFi disconnected - possible theft")
                    return True
        
        return False
    
    def check_lifted(self):
        """Check if robot is lifted using line sensors"""
        # All sensors see white = robot lifted
        if self.line_follower.all_sensors_white():
            self.off_line_duration += 0.1
            
            if self.off_line_duration > self.off_line_threshold:
                return True
        else:
            self.off_line_duration = 0  # Reset counter
        
        return False
    
    def trigger_alarm(self, reason):
        """Sound alarm and notify Firebase"""
        if self.alarm_active:
            return  # Already alarming
        
        self.alarm_active = True
        print(f"🚨🚨🚨 THEFT ALERT: {reason} 🚨🚨🚨")
        
        # Sound buzzer (loud pattern)
        for _ in range(20):
            lgpio.gpio_write(self.h, self.buzzer_pin, 1)
            time.sleep(0.1)
            lgpio.gpio_write(self.h, self.buzzer_pin, 0)
            time.sleep(0.1)
        
        # Report to Firebase
        try:
            self.firebase.report_theft(reason)
        except:
            pass
    
    def disable_alarm(self):
        """Turn off alarm (for authorized reset)"""
        self.alarm_active = False
        lgpio.gpio_write(self.h, self.buzzer_pin, 0)
        self.off_line_duration = 0
        print("✅ Theft alarm disabled")
    
    def cleanup(self):
        """Cleanup GPIO"""
        lgpio.gpio_write(self.h, self.buzzer_pin, 0)