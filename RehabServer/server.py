import cv2
import mediapipe as mp
import socket
import json
import math

# --- 1. Thiết lập Socket ---
HOST = '127.0.0.1'
PORT = 5000
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.bind((HOST, PORT))
sock.listen(1)
print(f"--- SERVER BÀN TAY ĐANG CHỜ TẠI {HOST}:{PORT} ---")

conn = None

# --- 2. Thiết lập MediaPipe HANDS ---
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    max_num_hands=1, # Chỉ theo dõi 1 tay
    min_detection_confidence=0.7,
    min_tracking_confidence=0.5)
mp_drawing = mp.solutions.drawing_utils

cap = cv2.VideoCapture(0)

# Hàm tính khoảng cách giữa 2 điểm
def get_distance(p1, p2):
    return math.sqrt((p1.x - p2.x)**2 + (p1.y - p2.y)**2)

while cap.isOpened():
    # Kết nối Unity (Non-blocking)
    if conn is None:
        try:
            sock.setblocking(False)
            conn, addr = sock.accept()
            sock.setblocking(True)
            print(f"--- UNITY ĐÃ KẾT NỐI: {addr} ---")
        except BlockingIOError:
            pass 

    ret, frame = cap.read()
    if not ret: break

    # Lật ảnh và xử lý
    frame = cv2.flip(frame, 1)
    image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    results = hands.process(image) # Xử lý BÀN TAY
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

    is_fist = False # Mặc định là không nắm
    state_text = "NO HAND"

    if results.multi_hand_landmarks:
        for hand_landmarks in results.multi_hand_landmarks:
            # Logic nhận diện Nắm/Xòe:
            # So sánh vị trí đầu ngón tay (TIP) so với khớp dưới (PIP)
            # Hoặc đơn giản: Tính khoảng cách từ đầu ngón trỏ (8) đến cổ tay (0)
            
            wrist = hand_landmarks.landmark[0]
            index_tip = hand_landmarks.landmark[8]
            middle_tip = hand_landmarks.landmark[12]
            
            # Khoảng cách tham chiếu (Cổ tay -> Khớp ngón giữa) để chuẩn hóa kích thước tay
            palm_size = get_distance(wrist, hand_landmarks.landmark[9]) 
            
            # Khoảng cách từ đầu ngón giữa đến cổ tay
            tip_distance = get_distance(wrist, middle_tip)

            # Nếu đầu ngón tay gần cổ tay (tỷ lệ < 1.0) -> NẮM
            # Nếu xa (tỷ lệ > 1.0) -> XÒE
            # (Con số 0.9 là ngưỡng, bạn có thể chỉnh nếu thấy quá nhạy/kém nhạy)
            ratio = tip_distance / palm_size
            
            if ratio < 0.9: 
                is_fist = True
                state_text = "NAM (FIST)"
                color = (0, 0, 255) # Đỏ
            else:
                is_fist = False
                state_text = "XOE (OPEN)"
                color = (0, 255, 0) # Xanh lá

            # Gửi dữ liệu sang Unity
            if conn:
                # Gửi số 1 (Nắm) hoặc 0 (Xòe)
                data = {"isFist": is_fist} 
                try:
                    message = json.dumps(data) + "\n"
                    conn.sendall(message.encode('utf-8'))
                except:
                    conn = None

            # Vẽ xương tay
            mp_drawing.draw_landmarks(image, hand_landmarks, mp_hands.HAND_CONNECTIONS)
            cv2.putText(image, f"State: {state_text}", (10, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, color, 2)
            cv2.putText(image, f"Ratio: {ratio:.2f}", (10, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

    cv2.imshow('Hand Tracking', image)
    if cv2.waitKey(5) & 0xFF == ord('q'): break

cap.release()
cv2.destroyAllWindows()
sock.close()