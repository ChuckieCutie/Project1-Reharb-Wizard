import cv2
import mediapipe as mp
import numpy as np
import socket
import json

# --- 1. Thiết lập Socket Server ---
# Gửi dữ liệu qua cổng 5000
HOST = '127.0.0.1'  # Localhost
PORT = 5000
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.bind((HOST, PORT))
sock.listen(1)
print(f"--- SERVER ĐANG CHỜ KẾT NỐI TẠI {HOST}:{PORT} ---")
conn, addr = sock.accept()
print(f"--- UNITY ĐÃ KẾT NỐI TỪ {addr} ---")

# --- 2. Thiết lập MediaPipe ---
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5)
mp_drawing = mp.solutions.drawing_utils

# Hàm tính góc
def calculate_angle(a, b, c):
    a = np.array(a) # First
    b = np.array(b) # Mid
    c = np.array(c) # End

    radians = np.arctan2(c[1] - b[1], c[0] - b[0]) - np.arctan2(a[1] - b[1], a[0] - b[0])
    angle = np.abs(radians * 180.0 / np.pi)

    if angle > 180.0:
        angle = 360 - angle
    return angle

# --- 3. Mở Webcam và Xử lý ---
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("Lỗi: Không thể mở webcam.")
    exit()

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        print("Lỗi: Không thể đọc frame.")
        break

    # Lật ảnh (webcam thường bị ngược) và đổi màu
    frame = cv2.flip(frame, 1)
    image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    # Xử lý tư thế
    results = pose.process(image)

    # Đổi màu lại
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

    rom_angle = 0.0
    comp_angle = 0.0

    try:
        landmarks = results.pose_landmarks.landmark

        # --- 4. TÍNH TOÁN CHỈ SỐ (ROM VÀ BÙ TRỪ) ---

        # Bài tập: Giơ tay ngang (Shoulder Abduction)
        # Chúng ta cần 3 điểm: Vai, Khuỷu tay, Hông
        right_shoulder = [landmarks[mp_pose.PoseLandmark.RIGHT_SHOULDER.value].x, landmarks[mp_pose.PoseLandmark.RIGHT_SHOULDER.value].y]
        right_elbow = [landmarks[mp_pose.PoseLandmark.RIGHT_ELBOW.value].x, landmarks[mp_pose.PoseLandmark.RIGHT_ELBOW.value].y]
        right_hip = [landmarks[mp_pose.PoseLandmark.RIGHT_HIP.value].x, landmarks[mp_pose.PoseLandmark.RIGHT_HIP.value].y]

        # Tính góc ROM (góc Hông-Vai-Khuỷu tay)
        rom_angle = calculate_angle(right_hip, right_shoulder, right_elbow)

        # Phát hiện bù trừ (Compensation) - Nghiêng thân
        # Chúng ta lấy 2 điểm vai
        left_shoulder = [landmarks[mp_pose.PoseLandmark.LEFT_SHOULDER.value].x, landmarks[mp_pose.PoseLandmark.LEFT_SHOULDER.value].y]

        # Tính góc nghiêng của đường thẳng nối 2 vai so với phương ngang
        dy = left_shoulder[1] - right_shoulder[1]
        dx = left_shoulder[0] - right_shoulder[0]
        comp_angle = np.degrees(np.arctan2(dy, dx))

        # --- 5. GỬI DỮ LIỆU SANG UNITY ---
        data = {
            "rom": rom_angle,
            "comp": comp_angle
        }
        message = json.dumps(data) + "\n" # Thêm \n để Unity biết kết thúc 1 gói tin

        try:
            conn.sendall(message.encode('utf-8'))
        except socket.error as e:
            print(f"Lỗi gửi data, có thể Unity đã ngắt kết nối: {e}")
            print("--- ĐANG CHỜ KẾT NỐI MỚI ---")
            conn, addr = sock.accept()
            print(f"--- UNITY ĐÃ KẾT NỐI LẠI TỪ {addr} ---")

        # Vẽ lên màn hình (để debug)
        mp_drawing.draw_landmarks(image, results.pose_landmarks, mp_pose.POSE_CONNECTIONS)
        cv2.putText(image, f"ROM: {int(rom_angle)}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
        cv2.putText(image, f"LEAN: {int(comp_angle)}", (10, 70), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

    except Exception as e:
        # print(f"Đang chờ người: {e}")
        pass

    cv2.imshow('MediaPipe Pose (Server AI)', image)

    # Nhấn 'q' để thoát
    if cv2.waitKey(5) & 0xFF == ord('q'):
        break

# Dọn dẹp
cap.release()
cv2.destroyAllWindows()
sock.close()
print("--- SERVER ĐÃ ĐÓNG ---")