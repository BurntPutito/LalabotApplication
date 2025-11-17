# firebase_handler.py - Firebase Realtime Database handler for Raspberry Pi 5
import requests
import json
import time
from config import FIREBASE_URL

class FirebaseHandler:
    def __init__(self):
        self.base_url = FIREBASE_URL
        self.last_connected_time = time.time()
        print("✅ Firebase handler initialized")
    
    def is_connected(self):
        """Check internet connection"""
        try:
            requests.get("https://www.google.com", timeout=3)
            self.last_connected_time = time.time()
            return True
        except:
            return False
    
    def reconnect(self):
        """Wait for internet connection"""
        print("⚠️ Waiting for internet connection...")
        while not self.is_connected():
            time.sleep(5)
        print("✅ Internet connected!")
    
    def get_pending_deliveries(self):
        """Get all pending deliveries from Firebase"""
        try:
            url = f"{self.base_url}/delivery_requests.json"
            response = requests.get(url, timeout=10)
            data = response.json()
            
            if data:
                # Filter only pending deliveries
                pending = {k: v for k, v in data.items() 
                          if v.get('status') == 'pending' and v.get('progressStage', 0) == 0}
                return pending
            return {}
            
        except Exception as e:
            print(f"⚠️ Error getting deliveries: {e}")
            return {}
    
    def update_delivery_compartment(self, delivery_id, compartment):
        """Assign compartment to delivery"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/compartment.json"
            requests.put(url, json=compartment, timeout=5)
        except Exception as e:
            print(f"⚠️ Error updating compartment: {e}")
    
    def update_delivery_stage(self, delivery_id, stage):
        """Update delivery progress stage (0-3)"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/progressStage.json"
            requests.put(url, json=stage, timeout=5)
        except Exception as e:
            print(f"⚠️ Error updating stage: {e}")
    
    def update_delivery_location(self, delivery_id, location):
        """Update robot's current location for this delivery"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/currentLocation.json"
            requests.put(url, json=location, timeout=5)
        except Exception as e:
            print(f"⚠️ Error updating location: {e}")
    
    def set_files_confirmed(self, delivery_id, confirmed):
        """Set filesConfirmed status"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesConfirmed.json"
            requests.put(url, json=confirmed, timeout=5)
        except Exception as e:
            print(f"⚠️ Error setting files confirmed: {e}")
    
    def get_files_confirmed(self, delivery_id):
        """Check if files are confirmed by sender"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesConfirmed.json"
            response = requests.get(url, timeout=5)
            return response.json() == True
        except:
            return False
    
    def set_confirmation_deadline(self, delivery_id, deadline_timestamp):
        """Set confirmation deadline timestamp"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/confirmationDeadline.json"
            requests.put(url, json=deadline_timestamp, timeout=5)
        except Exception as e:
            print(f"⚠️ Error setting deadline: {e}")
    
    def set_ready_for_pickup(self, delivery_id, ready):
        """Set readyForPickup status (arrived at destination)"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/readyForPickup.json"
            requests.put(url, json=ready, timeout=5)
        except Exception as e:
            print(f"⚠️ Error setting ready for pickup: {e}")
    
    def get_files_received(self, delivery_id):
        """Check if receiver confirmed receipt"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/filesReceived.json"
            response = requests.get(url, timeout=5)
            return response.json() == True
        except:
            return False
    
    def mark_delivery_completed(self, delivery_id):
        """Mark delivery as completed and move to history"""
        try:
            # Get delivery data
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = requests.get(url, timeout=5)
            delivery_data = response.json()
            
            # Update status
            delivery_data['status'] = 'completed'
            delivery_data['completedAt'] = time.time()
            
            # Move to history
            history_url = f"{self.base_url}/delivery_history/{delivery_id}.json"
            requests.put(history_url, json=delivery_data, timeout=5)
            
            # Delete from requests
            requests.delete(url, timeout=5)
            
        except Exception as e:
            print(f"⚠️ Error marking completed: {e}")
    
    def free_compartment(self, delivery_id):
        """Free up a compartment in robot_status"""
        try:
            # Get current status
            url = f"{self.base_url}/robot_status/currentDeliveries.json"
            response = requests.get(url, timeout=5)
            compartments = response.json() or {}
            
            # Find and clear the compartment
            for comp_num in ['compartment1', 'compartment2', 'compartment3']:
                if compartments.get(comp_num) == delivery_id:
                    comp_url = f"{self.base_url}/robot_status/currentDeliveries/{comp_num}.json"
                    requests.put(comp_url, json="", timeout=5)
                    print(f"✅ Freed {comp_num}")
                    break
                    
        except Exception as e:
            print(f"⚠️ Error freeing compartment: {e}")
    
    def cancel_delivery(self, delivery_id):
        """Cancel a delivery"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            requests.patch(url, json={
                "status": "cancelled",
                "cancelledAt": time.time()
            }, timeout=5)
        except Exception as e:
            print(f"⚠️ Error cancelling delivery: {e}")
    
    def report_theft(self, reason):
        """Report theft attempt to Firebase"""
        try:
            url = f"{self.base_url}/security_alerts.json"
            requests.post(url, json={
                "type": "THEFT_ATTEMPT",
                "reason": reason,
                "timestamp": time.time(),
                "severity": "CRITICAL"
            }, timeout=5)
            print(f"🚨 Theft reported: {reason}")
        except Exception as e:
            print(f"⚠️ Error reporting theft: {e}")
    
    def report_error(self, error, location):
        """Report critical error to Firebase"""
        try:
            url = f"{self.base_url}/robot_errors.json"
            requests.post(url, json={
                "error": str(error),
                "location": location,
                "timestamp": time.time()
            }, timeout=5)
        except:
            pass