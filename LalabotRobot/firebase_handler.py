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
            
    def cancel_delivery(self, delivery_id, reason):
        """Cancel a delivery and move it to history"""
        try:
            # Get delivery data
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = requests.get(url)
            
            if response.status_code == 200:
                delivery = response.json()
                
                if delivery:
                    # Update status
                    delivery['status'] = 'cancelled'
                    delivery['cancelledAt'] = time.strftime('%Y-%m-%dT%H:%M:%S')
                    delivery['cancellationReason'] = reason
                    
                    # Move to history
                    history_url = f"{self.base_url}/delivery_history/{delivery_id}.json"
                    requests.put(history_url, json=delivery)
                    
                    # Delete from active requests
                    requests.delete(url)
                    
                    print(f"  ✗ Delivery {delivery_id} cancelled: {reason}")
                    return True
            
            return False
        except Exception as e:
            print(f"❌ Error cancelling delivery: {e}")
            return False
    
    def update_current_location(self, delivery_id, location):
        """Update robot's current location"""
        try:
            # Use the full path without .json extension for PATCH
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = requests.patch(url, json={"currentLocation": location})
            
            if response.status_code == 200:
                print(f"  → Location updated: Room {location}")
            else:
                print(f"  ⚠ Location update failed: {response.status_code}")
        except Exception as e:
            print(f"❌ Location update failed: {e}")
    
    def wait_for_files_placed(self, delivery_id, timeout=100): #time for user to place the file
        """Wait for user to confirm files are placed"""
        print(f"  ⏳ Waiting for file confirmation (timeout: {timeout}s)...")
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
                response = requests.get(url)
                if response.status_code == 200:
                    delivery = response.json()
                    # Changed from filesPlaced to filesConfirmed
                    if delivery and delivery.get('filesConfirmed') == True:
                        print("  ✓ Files confirmed!")
                        return True
                time.sleep(1)
            except Exception as e:
                print(f"❌ Error checking filesConfirmed: {e}")
                time.sleep(1)
        
        print("  ⚠ Timeout waiting for file confirmation!")
        return False
    
    def update_progress_stage(self, delivery_id, stage):
        """Update delivery progress stage (0-3)"""
        try:
            url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
            response = requests.patch(url, json={"progressStage": stage})
            
            if response.status_code == 200:
                stage_names = {
                    0: "Processing",
                    1: "In Transit",
                    2: "Approaching",
                    3: "Arrived"
                }
                print(f"  → Progress: {stage_names.get(stage, 'Unknown')}")
            else:
                print(f"  ⚠ Progress update failed: {response.status_code}")
        except Exception as e:
            print(f"❌ Progress update failed: {e}")
    
    def wait_for_verification(self, delivery_id, timeout=300):
        """Wait for receiver to verify code - does NOT open compartment"""
        print(f"  ⏳ Waiting for receiver verification (timeout: {timeout}s)...")
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            try:
                url = f"{self.base_url}/delivery_requests/{delivery_id}.json"
                response = requests.get(url)
                if response.status_code == 200:
                    delivery = response.json()
                    # ONLY check if code was verified, NOT filesReceived
                    if delivery and delivery.get('codeVerified') == True:
                        print("  ✓ Verification code accepted!")
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
