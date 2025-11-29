import cv2
import mediapipe as mp
import socket
import json
import sys
import time
import io

# --- FIX LỖI FONT/ICON TRÊN WINDOWS ---
# Ép luồng xuất dữ liệu sang UTF-8 để không bị lỗi UnicodeEncodeError
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# --- CẤU HÌNH ---
UDP_IP = "127.0.0.1"
UDP_PORT_DATA = 5052  # Cổng gửi dữ liệu xương
UDP_PORT_VIDEO = 5053 # Cổng gửi hình ảnh

# --- SETUP UDP ---
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# --- SETUP AI ---
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
hands = mp_hands.Hands(min_detection_confidence=0.7, min_tracking_confidence=0.5, max_num_hands=2)

cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("❌ Error: Không mở được Camera.")
    sys.exit()

# Giờ thì in thoải mái không sợ lỗi font
print(f"✅ Server đang chạy! Dữ liệu: {UDP_PORT_DATA} | Hình ảnh: {UDP_PORT_VIDEO}")

while cap.isOpened():
    success, image = cap.read()
    if not success: break

    # 1. Lật ảnh và xử lý AI
    image = cv2.flip(image, 1)
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    results = hands.process(image_rgb)

    # 2. Gửi dữ liệu xương (JSON) qua cổng 5052
    data = {"hands": []}
    if results.multi_hand_landmarks:
        for hand_landmarks in results.multi_hand_landmarks:
            # Vẽ xương lên ảnh
            mp_drawing.draw_landmarks(image, hand_landmarks, mp_hands.HAND_CONNECTIONS)
            
            # Đóng gói toạ độ
            hand_data = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in hand_landmarks.landmark]
            data["hands"].append(hand_data)

    try:
        sock.sendto(json.dumps(data).encode(), (UDP_IP, UDP_PORT_DATA))
    except: pass

    # 3. Gửi hình ảnh qua cổng 5053
    # Resize nhỏ lại để gửi qua mạng cho mượt (320x240)
    small_frame = cv2.resize(image, (320, 240)) 
    
    # Nén ảnh JPG (chất lượng 50%)
    encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), 50]
    result, jpg_buffer = cv2.imencode('.jpg', small_frame, encode_param)

    if result:
        try:
            sock.sendto(jpg_buffer.tobytes(), (UDP_IP, UDP_PORT_VIDEO))
        except: pass

    # Đã bỏ cv2.imshow để không hiện cửa sổ Python nữa

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
cv2.destroyAllWindows()