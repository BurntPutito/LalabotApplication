# config.py
# Configuration file for WiFi and Firebase

# WiFi Credentials
WIFI_SSID = "YOUR_WIFI_NAME"
WIFI_PASSWORD = "YOUR_WIFI_PASSWORD"

# Firebase Configuration
FIREBASE_URL = "https://lalabotapplication-default-rtdb.asia-southeast1.firebasedatabase.app"

# Motor speeds (0-100)
BASE_SPEED = 80    # Higher = faster forward
TURN_SPEED = 50    # Higher = faster turns

# Timeouts
CONFIRMATION_TIMEOUT = 30  # Seconds to wait for file confirmation
MAX_TIMEOUT_RETRIES = 2    # Attempts before canceling

# Security
THEFT_OFF_LINE_THRESHOLD = 3  # Seconds off line before alarm
WIFI_CHECK_INTERVAL = 10      # How often to check WiFi
# BUZZER_PIN = 23              # GPIO pin for buzzer

# Servos (adjust if not opening/closing properly)
SERVO_CLOSED_DUTY = 2.5   # Lower value = more closed
SERVO_OPEN_DUTY = 12.5    # Higher value = more open

