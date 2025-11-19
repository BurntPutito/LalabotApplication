# LALABOT Delivery System - Technical Documentation

## System Overview
A delivery robot system with .NET MAUI mobile app and Raspberry Pi-based robot communicating via Firebase Realtime Database.

---

## 🏗️ Architecture

### Components
- **Mobile App**: .NET MAUI (C#) - User interface for senders/receivers
- **Robot**: Raspberry Pi 5 with Python - Autonomous delivery vehicle
- **Database**: Firebase Realtime Database - Real-time communication hub

### Hardware (Robot)
- **Motors**: TB6612FNG motor driver (2 DC motors)
- **Line Following**: 3 IR sensors (left, center, right)
- **Obstacle Detection**: HC-SR04 ultrasonic sensor
- **Compartments**: 3 servo motors (SG90)
- **Track**: Circular black line with white line markers for rooms

---

## 📱 Mobile App Flow

### User Roles
- **Sender**: Creates delivery, places files
- **Receiver**: Enters verification code, collects files

### Key Screens
1. **Login/Create Account** - Firebase Authentication
2. **Home Screen** - Active deliveries (outgoing/incoming)
3. **Create Delivery** - Select receiver, pickup, destination, file category
4. **History Screen** - Past deliveries (delivered/cancelled/pending)
5. **Profile/Settings** - Avatar, username management

### Delivery Creation
```
1. User selects receiver from list
2. Chooses pickup location (Room 1-4)
3. Chooses destination (Room 1-4, excluding pickup)
4. Selects file category (Documents, Forms, etc.)
5. Optional message
6. App assigns available compartment (1, 2, or 3)
7. Creates delivery in Firebase with status: "pending"
```

### Pickup Confirmation (Sender)
```
1. Robot arrives at pickup location
2. Opens assigned compartment
3. App shows "Confirm Files Placed" button
4. User places files and taps button
5. Sets filesConfirmed: true in Firebase
6. Robot closes compartment and proceeds
```

### Delivery Verification (Receiver)
```
1. Robot arrives at destination
2. Opens compartment
3. App shows verification code input
4. User enters 4-digit code
5. If correct: Sets filesReceived: true
6. Robot closes compartment and marks complete
```

---

## 🤖 Robot Flow

### Startup
```
1. Initialize all components (Firebase, motors, sensors, servos)
2. Start at Base (Room 0)
3. Close all compartments
4. Listen for delivery requests every 3 seconds
```

### Main Loop
```
while running:
    1. Check Firebase for pending deliveries
    2. Filter out completed/cancelled deliveries
    3. Plan optimal circular route from current location
    4. Execute route (pickups first, then deliveries)
    5. Return to base when no deliveries remain
    6. Wait 3 seconds, repeat
```

### Route Planning Logic
```
1. Start from current location
2. Visit rooms in circular order (1→2→3→4→1...)
3. At each room:
   - Do ALL pickups first
   - Then do deliveries (only if already picked up)
4. Make up to 2 full loops (handles complex routes like Room 3→Room 1)
5. Skip rooms with no tasks
```

### Navigation
```
1. Follow black line using IR sensors
2. Detect white line markers (all 3 sensors = white)
3. Increment room counter at each white line
4. Update currentLocation in Firebase at each room
5. Stop at target room
6. Check for obstacles continuously (stop if <20cm)
```

### Pickup Process
```
1. Navigate to pickup room
2. Update status: "at_pickup"
3. Open compartment
4. Reset filesConfirmed: false
5. Wait up to 100 seconds for confirmation
6. If confirmed:
   - Close compartment
   - Update progressStage: 1 (In Transit)
   - Continue to destination
7. If timeout:
   - Close compartment
   - Cancel delivery
   - Free compartment
```

### Delivery Process
```
1. Navigate to destination
2. Update progressStage: 2 (Approaching)
3. Update progressStage: 3 (Arrived)
4. Set readyForPickup: true (triggers app)
5. Open compartment
6. Wait up to 300 seconds for verification
7. If verified:
   - Close compartment
   - Mark as completed
   - Move to delivery_history
   - Free compartment
8. If timeout:
   - Close compartment
   - Cancel delivery
   - Free compartment
```

---

## 🔥 Firebase Data Structure

### `/delivery_requests/{deliveryId}`
```json
{
  "id": "del_1763491461",
  "sender": "John",
  "senderUid": "abc123",
  "receiver": "Mary",
  "receiverUid": "def456",
  "pickup": 2,
  "destination": 4,
  "compartment": 1,
  "category": "Documents",
  "message": "Optional message",
  "verificationCode": "1234",
  "status": "pending",
  "currentLocation": 0,
  "progressStage": 0,
  "filesConfirmed": false,
  "readyForPickup": false,
  "filesReceived": false,
  "createdAt": "2024-01-01T12:00:00",
  "arrivedAt": null,
  "completedAt": null
}
```

### Status Values
- `"pending"` - Created, waiting for robot
- `"at_pickup"` - Robot at pickup, waiting for files
- `"in_progress"` - Files picked up, heading to destination
- `"completed"` - Delivered successfully
- `"cancelled"` - Timeout or user cancelled

### Progress Stages (Visual Tracker)
- `0` - Processing (at pickup)
- `1` - In Transit (files picked up)
- `2` - Approaching (near destination)
- `3` - Arrived (at destination, ready for pickup)

### `/delivery_history/{deliveryId}`
- Same structure as requests, archived after completion/cancellation

### `/robot_status/currentDeliveries`
```json
{
  "compartment1": "del_xxx",
  "compartment2": "",
  "compartment3": "del_yyy"
}
```

### `/users/{userId}`
```json
{
  "Username": "John Doe",
  "Email": "john@example.com",
  "ProfileAvatarIndex": 0,
  "CustomAvatarUrl": ""
}
```

---

## 🎯 Key Features

### Smart Route Planning
- Visits rooms in circular order (never backtracks)
- Handles multiple deliveries efficiently
- Pickups always before deliveries for same delivery
- Can accept new requests mid-route

### Safety Features
- Obstacle detection (stops if object <20cm)
- Pickup/delivery timeouts (prevents infinite waiting)
- Cancelled deliveries moved to history
- Compartment tracking (max 3 simultaneous deliveries)

### Real-Time Updates
- Robot updates `currentLocation` at each room
- App shows live progress tracker (4 stages)
- Notifications for new deliveries
- Live delivery status updates

### Error Handling
- Pickup timeout → Cancel delivery
- Delivery timeout → Cancel delivery
- Lost line detection → Recovery logic
- Firebase connection checks

---

## 📊 Example Delivery Flow

```
USER: Creates delivery (Room 2 → Room 4)
  ↓
FIREBASE: delivery_requests/del_xxx created
  ↓
ROBOT: Detects new delivery, plans route
  ↓
ROBOT: Navigates Base → Room 2
  ↓
ROBOT: Opens compartment, waits
  ↓
USER: Places files, taps "Confirm"
  ↓
FIREBASE: filesConfirmed = true
  ↓
ROBOT: Closes compartment, navigates Room 2 → Room 4
  ↓
ROBOT: Opens compartment at Room 4
  ↓
FIREBASE: readyForPickup = true
  ↓
APP: Shows verification input to receiver
  ↓
USER: Enters code, taps "Confirm Receipt"
  ↓
FIREBASE: filesReceived = true
  ↓
ROBOT: Closes compartment, marks complete
  ↓
FIREBASE: Moved to delivery_history
  ↓
ROBOT: Returns to base
```

---

## 🔧 Configuration

### Timeouts
- **Pickup**: 100 seconds
- **Delivery**: 300 seconds

### Distances
- **Obstacle threshold**: 20cm
- **White line detection**: 3 sensors

### GPIO Pins (Raspberry Pi)
- Motors: 12, 13, 22-25
- Servos: 17, 18, 19
- IR Sensors: 5, 6, 26
- Ultrasonic: 20, 21

---

## 🚀 Deployment

### Robot Startup
```bash
cd LalabotRobot
python main.py
```

### App Deployment
- Build .NET MAUI app
- Deploy to Android/iOS devices
- Configure Firebase credentials

---

**End of Documentation**
