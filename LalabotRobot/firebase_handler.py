import requests
import json
import time
from config import *

class FirebaseHandler:
    def __init__(self):
        self.base_url = FIREBASE_URL
        print("✓ Firebase handler initialized")
    
    def get_active_deliveries(self):
        """Get all pending/in_progress deliveries"""
        try:
            response = requests.get(f"{self.base_url}/delivery_requests.json")
            if response.status_code == 200:
                data = response.json()
                if data:
                    deliveries = []
                    for key, delivery in data.items():
                        if delivery.get('status') in ['pending', 'in_progress']:
                            deliveries.append(delivery)
                    return deliveries
            return []
        except Exception as e:
            print(f"❌ Firebase error: {e}")
            return []
    
    def update_status(self, delivery_id, status):
        """Update delivery status"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/status.json"
            requests.patch(url, json=status)
            print(f"  → Status updated: {status}")
        except Exception as e:
            print(f"❌ Update failed: {e}")
    
    def update_current_location(self, delivery_id, location):
        """Update robot's current location"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}/currentLocation.json"
            requests.put(url, json=location)
        except Exception as e:
            print(f"❌ Location update failed: {e}")
    
    def wait_for_files_placed(self, delivery_id, timeout=300):
        """Wait for user to confirm files are placed"""
        print(f"  ⏳ Waiting for files to be placed (timeout: {timeout}s)...")
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
                response = requests.get(url)
                if response.status_code == 200:
                    delivery = response.json()
                    if delivery and delivery.get('filesPlaced') == True:
                        print("  ✓ Files confirmed placed!")
                        return True
                time.sleep(1)
            except Exception as e:
                print(f"❌ Error checking filesPlaced: {e}")
                time.sleep(1)
        
        print("  ⚠ Timeout waiting for files!")
        return False
    
    def wait_for_verification(self, delivery_id, timeout=300):
        """Wait for receiver to verify and confirm receipt"""
        print(f"  ⏳ Waiting for verification (timeout: {timeout}s)...")
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
                response = requests.get(url)
                if response.status_code == 200:
                    delivery = response.json()
                    if delivery and delivery.get('verified') == True:
                        print("  ✓ Verification successful!")
                        return True
                time.sleep(1)
            except Exception as e:
                print(f"❌ Error checking verification: {e}")
                time.sleep(1)
        
        print("  ⚠ Timeout waiting for verification!")
        return False
    
    def mark_completed(self, delivery_id):
        """Mark delivery as completed and move to history"""
        try:
            # Get delivery data
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = requests.get(url)
            
            if response.status_code == 200:
                delivery = response.json()
                
                # Update status and completedAt
                delivery['status'] = 'completed'
                delivery['completedAt'] = time.strftime('%Y-%m-%dT%H:%M:%S')
                
                # Move to history
                history_url = f"{self.base_url}/delivery_history/{delivery_id}.json"
                requests.put(history_url, json=delivery)
                
                # Delete from active requests
                requests.delete(url)
                
                print(f"  ✓ Delivery {delivery_id} marked as completed")
        except Exception as e:
            print(f"❌ Error marking completed: {e}")
    
    def free_compartment(self, delivery_id, compartment):
        """Free up compartment in robot status"""
        try:
            url = f"{self.base_url}/robot_status/currentDeliveries/compartment{compartment}.json"
            requests.put(url, json="")
            print(f"  ✓ Compartment {compartment} freed")
        except Exception as e:
            print(f"❌ Error freeing compartment: {e}")
