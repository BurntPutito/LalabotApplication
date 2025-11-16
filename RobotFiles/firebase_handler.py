# firebase_handler.py
import network
import urequests
import ujson
import time

class FirebaseHandler:
    def __init__(self):
        self.base_url = "https://lalabotapplication-default-rtdb.asia-southeast1.firebasedatabase.app"
        self.wlan = network.WLAN(network.STA_IF)
        
        # WiFi credentials (replace with yours)
        self.ssid = "YOUR_WIFI_SSID"
        self.password = "YOUR_WIFI_PASSWORD"
        
        self.connect_wifi()
    
    def connect_wifi(self):
        """Connect to WiFi"""
        self.wlan.active(True)
        
        if not self.wlan.isconnected():
            print(f"📶 Connecting to WiFi: {self.ssid}")
            self.wlan.connect(self.ssid, self.password)
            
            timeout = 10
            while not self.wlan.isconnected() and timeout > 0:
                time.sleep(1)
                timeout -= 1
            
            if self.wlan.isconnected():
                print(f"✅ Connected! IP: {self.wlan.ifconfig()[0]}")
            else:
                print("❌ WiFi connection failed")
    
    def is_connected(self):
        """Check if WiFi is connected"""
        return self.wlan.isconnected()
    
    def reconnect(self):
        """Reconnect to WiFi"""
        self.connect_wifi()
    
    def get_pending_deliveries(self):
        """Get all pending deliveries from Firebase"""
        try:
            url = f"{self.base_url}/delivery_requests.json"
            response = urequests.get(url)
            data = response.json()
            response.close()
            
            if data:
                # Filter only pending deliveries
                pending = {k: v for k, v in data.items() if v.get('status') == 'pending'}
                return pending
            return {}
            
        except Exception as e:
            print(f"⚠️ Error getting deliveries: {e}")
            return {}
    
    def update_delivery_compartment(self, delivery_id, compartment):
        """Update delivery's assigned compartment"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/compartment.json"
            response = urequests.patch(url, data=ujson.dumps(compartment))
            response.close()
        except Exception as e:
            print(f"⚠️ Error updating compartment: {e}")
    
    def update_delivery_stage(self, delivery_id, stage):
        """Update delivery progress stage"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/progressStage.json"
            response = urequests.patch(url, data=ujson.dumps(stage))
            response.close()
        except Exception as e:
            print(f"⚠️ Error updating stage: {e}")
    
    def update_delivery_location(self, delivery_id, location):
        """Update robot's current location for this delivery"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/currentLocation.json"
            response = urequests.patch(url, data=ujson.dumps(location))
            response.close()
        except Exception as e:
            print(f"⚠️ Error updating location: {e}")
    
    def set_files_confirmed(self, delivery_id, confirmed):
        """Set filesConfirmed status"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesConfirmed.json"
            response = urequests.patch(url, data=ujson.dumps(confirmed))
            response.close()
        except Exception as e:
            print(f"⚠️ Error setting files confirmed: {e}")
    
    def get_files_confirmed(self, delivery_id):
        """Check if files are confirmed"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesConfirmed.json"
            response = urequests.get(url)
            data = response.json()
            response.close()
            return data == True
        except:
            return False
    
    def set_confirmation_deadline(self, delivery_id, deadline):
        """Set confirmation deadline timestamp"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/confirmationDeadline.json"
            response = urequests.patch(url, data=ujson.dumps(deadline))
            response.close()
        except Exception as e:
            print(f"⚠️ Error setting deadline: {e}")
    
    def set_ready_for_pickup(self, delivery_id, ready):
        """Set readyForPickup status"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/readyForPickup.json"
            response = urequests.patch(url, data=ujson.dumps(ready))
            response.close()
        except Exception as e:
            print(f"⚠️ Error setting ready for pickup: {e}")
    
    def get_files_received(self, delivery_id):
        """Check if receiver confirmed receipt"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesReceived.json"
            response = urequests.get(url)
            data = response.json()
            response.close()
            return data == True
        except:
            return False
    
    def mark_delivery_completed(self, delivery_id):
        """Mark delivery as completed"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = urequests.patch(url, data=ujson.dumps({
                "status": "completed",
                "completedAt": time.time()
            }))
            response.close()
        except Exception as e:
            print(f"⚠️ Error marking completed: {e}")
    
    def free_compartment(self, compartment):
        """Free up a compartment in robot_status"""
        try:
            url = f"{self.base_url}/robot_status/currentDeliveries/compartment{compartment}.json"
            response = urequests.patch(url, data=ujson.dumps(""))
            response.close()
        except Exception as e:
            print(f"⚠️ Error freeing compartment: {e}")
    
    def cancel_delivery(self, delivery_id):
        """Cancel a delivery"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = urequests.patch(url, data=ujson.dumps({
                "status": "cancelled",
                "cancelledAt": time.time()
            }))
            response.close()
        except Exception as e:
            print(f"⚠️ Error cancelling delivery: {e}")
    
    def notify_confirmation_timeout(self, delivery_id):
        """Notify app about confirmation timeout"""
        # App will handle notifications, we just log it
        print(f"⏰ Confirmation timeout for {delivery_id}")
    
    def report_error(self, error, location):
        """Report critical error to Firebase"""
        try:
            url = f"{self.base_url}/robot_errors.json"
            response = urequests.post(url, data=ujson.dumps({
                "error": error,
                "location": location,
                "timestamp": time.time()
            }))
            response.close()
        except:
            pass