<h1 align="center">🧭 DẤU CHÂN DI SẢN</h1>

<p align="center">
  <strong>Khám phá di sản văn hóa Việt Nam và kết nối du lịch bản địa</strong>
</p>

<p align="center">
  Khám phá địa điểm · Check-in GPS · Sổ tay hành trình · Trợ lý AI
</p>

<p align="center">
  <code>React</code> · <code>ASP.NET Core Web API</code> · <code>SQL Server</code> · <code>Firebase</code> · <code>Google Maps</code>
</p>

---

## 🏛️ Giới thiệu

**Dấu Chân Di Sản** là đề tài cuối kỳ của lớp **126LTWNC01**. Nhóm xây dựng một website để người dùng tìm hiểu các di tích, công trình kiến trúc và câu chuyện văn hóa Việt Nam, từ đó chọn những địa điểm muốn ghé thăm và ghi lại trải nghiệm của mình.

Chẳng hạn, trước chuyến đi Hội An, người dùng có thể xem các địa điểm trên bản đồ và hỏi trợ lý AI nên ghé đâu nếu chỉ có một buổi chiều. Khi đến nơi, họ có thể check-in, lưu ảnh và viết vài dòng vào sổ tay để xem lại sau chuyến đi.

## ✨ Chức năng của đề tài

| Chức năng | Người dùng có thể làm gì? |
| --- | --- |
| 🏯 **Khám phá di sản** | Tìm địa điểm và xem hình ảnh, video, câu chuyện cùng thông tin tham quan. |
| 📍 **Check-in theo vị trí** | Ghi nhận lần ghé thăm dựa trên vị trí GPS. |
| 🗺️ **Bản đồ hành trình** | Xem tuyến đường đã đi và các điểm đã ghé trên bản đồ. |
| 📓 **Sổ tay hành trình** | Lưu ảnh, ghi chú và những kỷ niệm trong chuyến đi. |
| 🎯 **Nhiệm vụ khám phá** | Thực hiện các nhiệm vụ gắn với địa điểm và theo dõi tiến độ. |
| 💬 **Trợ lý AI** | Hỏi cách sử dụng hệ thống, tìm địa điểm phù hợp và nhận gợi ý lịch trình. |
| 🤝 **Đóng góp cộng đồng** | Bình luận, đề xuất địa điểm hoặc gửi thông tin bổ sung để kiểm duyệt. |

## 👥 Vai trò trong hệ thống

| Vai trò | Quyền chính |
| --- | --- |
| **Khách** | Tra cứu địa điểm, đọc câu chuyện và xem bản đồ di sản. |
| **Người dùng** | Check-in, lưu hành trình, viết sổ tay, bình luận và gửi đóng góp. |
| **Cán bộ văn hóa** | Xem xét và duyệt nội dung di sản do cộng đồng gửi. |
| **Quản trị viên** | Quản lý tài khoản, phân quyền và nội dung trên hệ thống. |

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
| --- | --- |
| **Giao diện web** | React 19, TypeScript, Vite |
| **Backend** | C#, ASP.NET Core 10 Web API |
| **Cơ sở dữ liệu** | Microsoft SQL Server |
| **Xác thực** | Firebase Authentication |
| **Bản đồ và vị trí** | Google Maps, GPS |
| **Trợ lý AI** | Mô hình Decision Tree (Python) + Gemini |
| **Kiểm thử API** | Swagger UI |

---

## 🚀 Hướng dẫn chạy dự án

### Yêu cầu cài đặt

| Phần mềm | Phiên bản | Kiểm tra |
| --- | --- | --- |
| **.NET SDK** | 10.0 | `dotnet --version` |
| **Node.js** | 20 trở lên | `node --version` |
| **SQL Server** | 2019 trở lên | Mở SSMS kiểm tra |
| **Python** | 3.9 trở lên | `python --version` |
| **SSMS** | Bất kỳ | Để chạy script SQL |

---

### Bước 1 — Tạo cơ sở dữ liệu

Mở **SSMS**, kết nối tới `localhost`, sau đó mở và chạy lần lượt từng file SQL dưới đây (nhấn **F5** để thực thi):

```
1. DauChanDiSan.sql       ← Tạo database và toàn bộ bảng
2. Bosung1.sql            ← Bổ sung cấu trúc
3. Bosung2.sql            ← Bổ sung cấu trúc
4. SQLDuLieuMau.sql       ← Chèn dữ liệu mẫu
```

> ⚠️ Chạy đúng thứ tự, không bỏ bước nào.

---

### Bước 2 — Cấu hình Backend

**2.1. Firebase — Service Account**

1. Tải file key từ Firebase Console hoặc nhận từ nhóm.
2. Tạo thư mục `D:\Secrets\` trên máy và lưu file vào:
   ```
   D:\Secrets\dauchandisan-bde24-firebase-adminsdk-fbsvc-8ca5d88f9f.json
   ```
3. File `backend/DauChanDiSan.Api/Properties/launchSettings.json` đã cấu hình sẵn đúng đường dẫn này:
   ```json
   "GOOGLE_APPLICATION_CREDENTIALS": "D:\\Secrets\\dauchandisan-bde24-firebase-adminsdk-fbsvc-8ca5d88f9f.json"
   ```
   *(Nếu bạn lưu ở ổ đĩa hoặc đường dẫn khác, chỉ cần sửa lại dòng này trong `launchSettings.json` cho khớp).*

**2.2. Firebase — Project ID**

Mở file `backend/DauChanDiSan.Api/appsettings.json` và điền thông tin:

```json
"Firebase": {
  "ProjectId": "dauchandisan-bde24",
  "StorageBucket": "dauchandisan-bde24.appspot.com"
}
```

**2.3. Cài đặt Trợ lý AI (Python)**

Thư mục `HuanLuyen/` (chứa mô hình AI và code phân loại ý định) **đã được tích hợp sẵn** trong backend:
```
backend/DauChanDiSan.Api/HuanLuyen/
├── predict_intent.py
├── intent_tree.joblib
├── train_intents.py
└── dau_chan_di_san_intents.csv
```

Bạn chỉ cần cài các thư viện Python:

```powershell
pip install scikit-learn joblib pandas
```

Backend `appsettings.json` đã cấu hình sẵn:
```json
"LocalAI": {
  "ScriptPath": "HuanLuyen/predict_intent.py",
  "PythonExe": "python"
}
```

> Nếu lệnh Python trên máy là `py` hoặc `python3`, hãy đổi giá trị `PythonExe` cho phù hợp.

---

### Bước 3 — Chạy Backend

```powershell
cd backend/DauChanDiSan.Api
dotnet restore
dotnet run --launch-profile https
```

Sau khi khởi động, kiểm tra:

| Địa chỉ | Kết quả mong đợi |
| --- | --- |
| `http://localhost:5080/api/health` | `{"status":"ok"}` |
| `http://localhost:5080/api/ai/trang-thai` | `{"daCauHinh":true,"cheDo":"LOCAL"}` |
| `https://localhost:7152/swagger` | Giao diện Swagger UI |

---

### Bước 4 — Chạy Frontend

Mở **terminal mới** (giữ backend đang chạy), sau đó:

```powershell
cd frontend
npm ci
npm run dev
```

Mở trình duyệt tại: **http://localhost:5173**

> Vite đã cấu hình proxy sẵn — mọi request tới `/api` sẽ tự động chuyển sang backend tại `http://localhost:5080`.

---

### Thứ tự khởi động mỗi lần chạy lại

```
1. Đảm bảo SQL Server đang chạy
2. Terminal 1 → cd backend/DauChanDiSan.Api → dotnet run --launch-profile https
3. Terminal 2 → cd frontend → npm run dev
4. Mở http://localhost:5173
```

---

## 🎓 Thành viên nhóm

| STT | Họ và tên | Mã sinh viên | Lớp học phần |
| :---: | --- | --- | --- |
| 01 | **Lê Đình Danh** | `2415053122308` | `126LTWNC01` |
| 02 | **Ngô Thanh Nguyên** | `2415053122330` | `126LTWNC01` |
| 03 | **Nguyễn Hữu Tú** | `2415053122346` | `126LTWNC01` |
| 04 | **Phan Mạnh Tùng** | `2415053122347` | `126LTWNC01` |
| 05 | **Đoàn Ngọc Tường** | `2415053122348` | `126LTWNC01` |

---

<p align="center">
  <em>Đề tài cuối kỳ · Lớp học phần 126LTWNC01</em>
</p>
