# GPIO Pin Definitions (from your working tests)

# Motors (TB6612FNG)
PWMA = 12
AIN1 = 24
AIN2 = 23
PWMB = 13
BIN1 = 27
BIN2 = 22
STBY = 25

# Servos (Compartments)
SERVO1_PIN = 17  # Compartment 1
SERVO2_PIN = 18  # Compartment 2
SERVO3_PIN = 19  # Compartment 3

# IR Sensors (Line Following)
IR_LEFT = 5
IR_CENTER = 6
IR_RIGHT = 26

# Ultrasonic Sensor (Obstacle Detection)
TRIG = 20
ECHO = 21

# Firebase Configuration
FIREBASE_URL = "https://lalabotapplication-default-rtdb.asia-southeast1.firebasedatabase.app/"
FIREBASE_API_KEY = "AIzaSyDbUDCax6orMurh6hSBKbKg51luC8xa1GQ"

# Robot Settings
MOTOR_SPEED = 0.7  # 70% speed (adjust as needed)
SERVO_CLOSED = 90  # 9 o'clock position
SERVO_OPEN = 180   # 12 o'clock position
OBSTACLE_DISTANCE = 20  # cm - stop if obstacle closer than this
WHITE_LINE_THRESHOLD = 3  # Number of sensors needed to detect white line
ROOM_COUNT = 4  # Total rooms (1, 2, 3, 4) + base (0)
