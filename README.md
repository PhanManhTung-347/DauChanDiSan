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
| **Giao diện web** | React, TypeScript, Vite |
| **Backend** | C#, ASP.NET Core Web API, LINQ |
| **Cơ sở dữ liệu** | Microsoft SQL Server |
| **Xác thực** | Firebase Authentication |
| **Bản đồ và vị trí** | Google Maps, GPS |
| **Trợ lý AI** | Mô hình phân loại ý định câu hỏi và Gemini |
| **Kiểm thử API** | Swagger UI |

## 🚀 Chạy dự án trên máy cá nhân

1. Cài **.NET SDK**, **Node.js** và **SQL Server** theo phiên bản dự án yêu cầu.
2. Chạy script SQL của dự án để tạo cơ sở dữ liệu, sau đó cập nhật chuỗi kết nối trong cấu hình backend.
3. Cấu hình Firebase và Google Maps cho môi trường phát triển.
4. Mở terminal trong thư mục chứa file `.csproj` của Web API và chạy:

   ```bash
   dotnet run
   ```

5. Mở terminal khác trong thư mục frontend chứa `package.json` và chạy:

   ```bash
   npm install
   npm run dev
   ```

6. Mở địa chỉ Vite in ra trong terminal để vào website. Nếu frontend dùng proxy, kiểm tra cổng API trong cấu hình Vite khớp với cổng backend đang chạy.

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
