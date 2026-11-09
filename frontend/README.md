# Dấu Chân Di Sản · Frontend

Xem [hướng dẫn dự án](../HUONG_DAN_DAU_CHAN_DI_SAN.md) để cấu hình API, SQL, tài khoản và hosting.

```powershell
npm ci
npm run dev
```

`src/features` chia theo 5 nhóm của backend. Cổng dev là 5173. Vite chuyển yêu cầu `/api` sang ASP.NET Core ở cổng 5080. Chạy `npm run build` để tạo bản triển khai trong `dist`.
