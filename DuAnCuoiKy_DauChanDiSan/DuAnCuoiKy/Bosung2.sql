USE [DauChanDiSan];
GO

SET NOCOUNT ON;

IF DB_NAME() <> N'DauChanDiSan' OR @@TRANCOUNT <> 0
BEGIN
    RAISERROR(N'Hãy mở New Query trong database DauChanDiSan rồi chạy lại.', 16, 1);
    RETURN;
END;

SET XACT_ABORT ON;

DECLARE @ThemDuLieuDemo bit = 1;
DECLARE @KetQuaKhoa int;
DECLARE @TenCheck sysname;
DECLARE @SoCheck int;
DECLARE @Sql nvarchar(max);

BEGIN TRY
    BEGIN TRAN;

    EXEC @KetQuaKhoa = sys.sp_getapplock
        @Resource = N'DauChanDiSan.BoSungDichVuDuLich',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 10000;

    IF @KetQuaKhoa < 0
        THROW 51200, N'Không lấy được khóa cập nhật CSDL. Hãy thử lại.', 1;

    IF OBJECT_ID(N'dbo.TinhThanh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.DiSan', N'U') IS NULL
       OR OBJECT_ID(N'dbo.HanhTrinh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.DiemHanhTrinh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.TinNhanAI', N'U') IS NULL
       OR OBJECT_ID(N'dbo.NguonTraLoiAI', N'U') IS NULL
       OR OBJECT_ID(N'dbo.vw_NguCanhGemini', N'V') IS NULL
        THROW 51201, N'Chưa tìm thấy đủ bảng và view của CSDL DauChanDiSan gốc.', 1;

    /* 1. Danh mục khách sạn, homestay và quán ăn */

    IF OBJECT_ID(N'dbo.DichVuDuLich', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DichVuDuLich (
            DichVuId int IDENTITY PRIMARY KEY,
            TinhThanhId int NOT NULL REFERENCES dbo.TinhThanh(TinhThanhId),
            Slug varchar(160) NOT NULL UNIQUE,
            Loai varchar(16) NOT NULL,
            Ten nvarchar(200) NOT NULL,
            KhuVuc nvarchar(120) NULL,
            DiaChi nvarchar(250) NOT NULL,
            MoTa nvarchar(1000) NULL,
            ViDo decimal(9,6) NULL,
            KinhDo decimal(9,6) NULL,
            DonViTinh varchar(16) NOT NULL,
            SucChuaPhong smallint NULL,
            TrangThai varchar(16) NOT NULL DEFAULT 'NHAP',
            LaDuLieuDemo bit NOT NULL DEFAULT 0,
            TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
            CHECK (TrangThai IN ('NHAP', 'CONG_KHAI', 'AN')),
            CHECK (
                (ViDo IS NULL AND KinhDo IS NULL)
                OR (ViDo IS NOT NULL AND KinhDo IS NOT NULL
                    AND ViDo BETWEEN -90 AND 90
                    AND KinhDo BETWEEN -180 AND 180)
            ),
            CHECK (
                (Loai IN ('KHACH_SAN', 'HOMESTAY') AND DonViTinh = 'PHONG_DEM'
                 AND SucChuaPhong IS NOT NULL AND SucChuaPhong BETWEEN 1 AND 30)
                OR (Loai = 'QUAN_AN' AND DonViTinh = 'NGUOI_BUA'
                    AND SucChuaPhong IS NULL)
            )
        );
    END;

    IF COL_LENGTH(N'dbo.DichVuDuLich', N'KhuVuc') IS NULL
        ALTER TABLE dbo.DichVuDuLich ADD KhuVuc nvarchar(120) NULL;

    IF OBJECT_ID(N'dbo.GiaDichVuDuLich', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.GiaDichVuDuLich (
            GiaDichVuId int IDENTITY PRIMARY KEY,
            DichVuId int NOT NULL REFERENCES dbo.DichVuDuLich(DichVuId),
            NgayApDung date NOT NULL,
            GiaVnd decimal(14,0) NOT NULL CHECK (GiaVnd >= 0),
            NguonGia nvarchar(300) NULL,
            CapNhatLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
            UNIQUE (DichVuId, NgayApDung)
        );
    END;

    /* 2. Mở rộng hành trình và nguồn trả lời AI */

    IF COL_LENGTH(N'dbo.HanhTrinh', N'SoNguoi') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD SoNguoi smallint NULL;

    IF COL_LENGTH(N'dbo.HanhTrinh', N'NganSachVnd') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD NganSachVnd decimal(14,0) NULL;

    IF COL_LENGTH(N'dbo.HanhTrinh', N'ChiPhiDiChuyenVnd') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD ChiPhiDiChuyenVnd decimal(14,0) NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DichVuId') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DichVuId int NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DonGiaDuKienVnd') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DonGiaDuKienVnd decimal(14,0) NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'SoLuongTinhGia') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD SoLuongTinhGia int NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DonViTinhGia') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DonViTinhGia varchar(16) NULL;

    IF COL_LENGTH(N'dbo.NguonTraLoiAI', N'DichVuId') IS NULL
        ALTER TABLE dbo.NguonTraLoiAI ADD DichVuId int NULL;

    IF OBJECT_ID(N'dbo.FK_DiemHanhTrinh_DichVuDuLich', N'F') IS NULL
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT FK_DiemHanhTrinh_DichVuDuLich
                FOREIGN KEY (DichVuId) REFERENCES dbo.DichVuDuLich(DichVuId);';

    IF OBJECT_ID(N'dbo.FK_NguonTraLoiAI_DichVuDuLich', N'F') IS NULL
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.NguonTraLoiAI WITH CHECK
            ADD CONSTRAINT FK_NguonTraLoiAI_DichVuDuLich
                FOREIGN KEY (DichVuId) REFERENCES dbo.DichVuDuLich(DichVuId);';

    IF OBJECT_ID(N'dbo.CK_HanhTrinh_ChiPhi', N'C') IS NULL
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.HanhTrinh WITH CHECK
            ADD CONSTRAINT CK_HanhTrinh_ChiPhi CHECK (
                (SoNguoi IS NULL OR SoNguoi BETWEEN 1 AND 100)
                AND (NganSachVnd IS NULL OR NganSachVnd >= 0)
                AND (ChiPhiDiChuyenVnd IS NULL OR ChiPhiDiChuyenVnd >= 0)
            );';

    IF OBJECT_ID(N'dbo.CK_DiemHanhTrinh_ChiPhi', N'C') IS NULL
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT CK_DiemHanhTrinh_ChiPhi CHECK (
                (DonGiaDuKienVnd IS NULL AND SoLuongTinhGia IS NULL AND DonViTinhGia IS NULL)
                OR (
                    DonGiaDuKienVnd IS NOT NULL AND DonGiaDuKienVnd >= 0
                    AND SoLuongTinhGia IS NOT NULL AND SoLuongTinhGia > 0
                    AND DonViTinhGia IS NOT NULL
                    AND DonViTinhGia IN (''NGUOI_LUOT'', ''PHONG_DEM'', ''NGUOI_BUA'')
                )
            );';

    IF OBJECT_ID(N'dbo.CK_DiemHanhTrinh_MotLoaiDiem', N'C') IS NULL
    BEGIN
        SELECT @SoCheck = COUNT(*), @TenCheck = MAX(name)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.DiemHanhTrinh')
          AND definition LIKE N'%DiSanId%'
          AND definition LIKE N'%TraiNghiemId%'
          AND definition NOT LIKE N'%DichVuId%';

        IF @SoCheck <> 1
            THROW 51202, N'Cần kiểm tra ràng buộc loại điểm trên DiemHanhTrinh.', 1;

        SET @Sql = N'ALTER TABLE dbo.DiemHanhTrinh DROP CONSTRAINT ' + QUOTENAME(@TenCheck) + N';';
        EXEC sys.sp_executesql @Sql;

        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT CK_DiemHanhTrinh_MotLoaiDiem CHECK (
                IIF(DiSanId IS NULL, 0, 1) + IIF(TraiNghiemId IS NULL, 0, 1)
                + IIF(DichVuId IS NULL, 0, 1) = 1
            );';
    END;

    IF OBJECT_ID(N'dbo.CK_NguonTraLoiAI_MotLoaiNguon', N'C') IS NULL
    BEGIN
        SELECT @SoCheck = COUNT(*), @TenCheck = MAX(name)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.NguonTraLoiAI')
          AND definition LIKE N'%DiSanId%'
          AND definition LIKE N'%TraiNghiemId%'
          AND definition LIKE N'%TuLieuId%'
          AND definition NOT LIKE N'%DichVuId%';

        IF @SoCheck <> 1
            THROW 51203, N'Cần kiểm tra ràng buộc loại nguồn trên NguonTraLoiAI.', 1;

        SET @Sql = N'ALTER TABLE dbo.NguonTraLoiAI DROP CONSTRAINT ' + QUOTENAME(@TenCheck) + N';';
        EXEC sys.sp_executesql @Sql;

        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.NguonTraLoiAI WITH CHECK
            ADD CONSTRAINT CK_NguonTraLoiAI_MotLoaiNguon CHECK (
                IIF(DiSanId IS NULL, 0, 1) + IIF(TraiNghiemId IS NULL, 0, 1)
                + IIF(TuLieuId IS NULL, 0, 1) + IIF(DichVuId IS NULL, 0, 1) = 1
            );';
    END;

    IF OBJECT_ID(N'dbo.CK_TinNhanAI_NhaCungCap', N'C') IS NULL
    BEGIN
        SELECT @SoCheck = COUNT(*), @TenCheck = MAX(name)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.TinNhanAI')
          AND definition LIKE N'%NhaCungCap%'
          AND definition LIKE N'%GEMINI%'
          AND definition NOT LIKE N'%MaMoHinh%'
          AND definition NOT LIKE N'%LOCAL%';

        IF @SoCheck <> 1
            THROW 51204, N'Cần kiểm tra ràng buộc nhà cung cấp trên TinNhanAI.', 1;

        SET @Sql = N'ALTER TABLE dbo.TinNhanAI DROP CONSTRAINT ' + QUOTENAME(@TenCheck) + N';';
        EXEC sys.sp_executesql @Sql;

        ALTER TABLE dbo.TinNhanAI WITH CHECK
        ADD CONSTRAINT CK_TinNhanAI_NhaCungCap CHECK (NhaCungCap IN ('GEMINI', 'LOCAL'));
    END;

    /* 3. View và thủ tục đọc dữ liệu */

    EXEC sys.sp_executesql N'
    CREATE OR ALTER VIEW dbo.vw_DichVuDuLichCongKhai AS
    SELECT d.DichVuId, d.TinhThanhId, t.Ten AS TenTinhThanh, d.Slug, d.Loai,
           d.Ten, d.DiaChi, d.MoTa, d.ViDo, d.KinhDo, d.DonViTinh,
           d.SucChuaPhong, d.LaDuLieuDemo, d.KhuVuc
    FROM dbo.DichVuDuLich d
    JOIN dbo.TinhThanh t ON t.TinhThanhId = d.TinhThanhId
    WHERE d.TrangThai = ''CONG_KHAI'' AND t.HoatDong = 1;';

    EXEC sys.sp_executesql N'
    CREATE OR ALTER VIEW dbo.vw_NguCanhTroLy AS
    SELECT LoaiNguon, NguonId, DiSanId, TinhThanhId, TieuDe, NoiDung, UrlNguon,
           CONVERT(bit, 0) AS LaDuLieuDemo
    FROM dbo.vw_NguCanhGemini
    UNION ALL
    SELECT ''DICH_VU'', DichVuId, NULL, TinhThanhId,
           CONVERT(nvarchar(220), CONCAT(CASE WHEN LaDuLieuDemo = 1 THEN N''[DEMO] '' END, Ten)),
           CONVERT(nvarchar(max), CONCAT(KhuVuc, N'' | '', DiaChi, N'' | '', MoTa)),
           CONVERT(nvarchar(1000), NULL),
           LaDuLieuDemo
    FROM dbo.vw_DichVuDuLichCongKhai;';

    EXEC sys.sp_executesql N'
    CREATE OR ALTER PROCEDURE dbo.usp_LayDichVuDuLich
        @TinhThanhId int, @Loai varchar(16) = NULL, @Ngay date = NULL,
        @GioiHan int = 20, @BaoGomDemo bit = 0, @KhuVuc nvarchar(120) = NULL
    AS
    BEGIN
        SET NOCOUNT ON;

        IF @GioiHan IS NULL OR @GioiHan NOT BETWEEN 1 AND 100
            THROW 51205, N''Giới hạn kết quả phải từ 1 đến 100.'', 1;

        SET @Ngay = COALESCE(@Ngay, CONVERT(date, DATEADD(MINUTE, 420, SYSUTCDATETIME())));

        SELECT TOP (@GioiHan) d.*, g.GiaVnd, g.NgayApDung, g.NguonGia
        FROM dbo.vw_DichVuDuLichCongKhai d
        OUTER APPLY (
            SELECT TOP (1) g.GiaVnd, g.NgayApDung, g.NguonGia
            FROM dbo.GiaDichVuDuLich g
            WHERE g.DichVuId = d.DichVuId AND g.NgayApDung <= @Ngay
            ORDER BY g.NgayApDung DESC
        ) g
        WHERE d.TinhThanhId = @TinhThanhId
          AND (@Loai IS NULL OR d.Loai = @Loai)
          AND (@KhuVuc IS NULL OR d.KhuVuc = @KhuVuc)
          AND (@BaoGomDemo = 1 OR d.LaDuLieuDemo = 0)
        ORDER BY d.LaDuLieuDemo, CASE WHEN g.GiaVnd IS NULL THEN 1 ELSE 0 END,
                 g.GiaVnd, d.DichVuId;
    END;';

    EXEC sys.sp_executesql N'
    CREATE OR ALTER VIEW dbo.vw_ChiPhiHanhTrinh AS
    SELECT h.HanhTrinhId, h.NguoiDungId, h.SoNguoi, h.NganSachVnd, h.ChiPhiDiChuyenVnd,
           COALESCE(p.ChiPhiCacDiemVnd, 0) AS ChiPhiCacDiemVnd,
           COALESCE(p.ChiPhiCacDiemVnd, 0) + COALESCE(h.ChiPhiDiChuyenVnd, 0) AS TamTinhVnd,
           COALESCE(p.SoDiemChuaCoGia, 0) AS SoDiemChuaCoGia
    FROM dbo.HanhTrinh h
    LEFT JOIN (
        SELECT HanhTrinhId,
               SUM(DonGiaDuKienVnd * SoLuongTinhGia) AS ChiPhiCacDiemVnd,
               COUNT_BIG(CASE WHEN DonGiaDuKienVnd IS NULL THEN 1 END) AS SoDiemChuaCoGia
        FROM dbo.DiemHanhTrinh
        GROUP BY HanhTrinhId
    ) p ON p.HanhTrinhId = h.HanhTrinhId;';

    /* 4. Lưu tin nhắn trợ lý, giữ nguyên cách gọi Gemini hiện tại */

    EXEC sys.sp_executesql N'
    CREATE OR ALTER PROCEDURE dbo.usp_LuuTinNhanGemini
        @NguoiDungId int, @HoiThoaiId bigint, @Vai varchar(12), @NoiDung nvarchar(max),
        @MaMoHinh varchar(100) = NULL, @TokenVao int = NULL, @TokenRa int = NULL,
        @DeXuatHanhTrinhJson nvarchar(max) = NULL, @NguonJson nvarchar(max) = NULL,
        @TinNhanAIId bigint = NULL OUTPUT, @TraKetQua bit = 1,
        @NhaCungCap varchar(16) = ''GEMINI''
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        SET @TinNhanAIId = NULL;

        IF @NguonJson IS NOT NULL AND (
            @Vai <> ''ASSISTANT'' OR ISJSON(@NguonJson) <> 1
            OR LEFT(LTRIM(@NguonJson), 1) <> ''[''
        )
            THROW 51131, N''Nguồn phải là mảng JSON của tin nhắn trợ lý.'', 1;

        IF @DeXuatHanhTrinhJson IS NOT NULL AND (
            ISJSON(@DeXuatHanhTrinhJson) <> 1 OR LEFT(LTRIM(@DeXuatHanhTrinhJson), 1) <> ''{''
        )
            THROW 51132, N''Kế hoạch phải là một đối tượng JSON.'', 1;

        DECLARE @Nguon TABLE (ThuTu int PRIMARY KEY, Loai varchar(16), Id int);

        IF @NguonJson IS NOT NULL
            INSERT @Nguon (ThuTu, Loai, Id)
            SELECT CONVERT(int, j.[key]) + 1, p.Loai, p.Id
            FROM OPENJSON(@NguonJson) j
            OUTER APPLY OPENJSON(CASE WHEN j.[type] = 5 THEN j.value ELSE N''{}'' END)
                WITH (Loai varchar(16) ''$.loai'', Id int ''$.id'') p;

        IF (SELECT COUNT(*) FROM @Nguon) > 40
           OR EXISTS (SELECT 1 FROM @Nguon GROUP BY Loai, Id HAVING COUNT(*) > 1)
            THROW 51133, N''Danh sách nguồn bị trùng hoặc vượt giới hạn 40 mục.'', 1;

        BEGIN TRAN;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.HoiThoaiAI h WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.NguoiDung u ON u.NguoiDungId = h.NguoiDungId AND u.HoatDong = 1
            WHERE h.HoiThoaiId = @HoiThoaiId AND h.NguoiDungId = @NguoiDungId AND h.DaLuuTru = 0
        )
            THROW 51134, N''Hội thoại không khả dụng cho tài khoản này.'', 1;

        INSERT dbo.TinNhanAI (
            HoiThoaiId, NguoiDungId, Vai, NoiDung, NhaCungCap, MaMoHinh,
            TokenVao, TokenRa, DeXuatHanhTrinhJson
        )
        VALUES (
            @HoiThoaiId, @NguoiDungId, @Vai, @NoiDung,
            CASE WHEN @Vai = ''ASSISTANT'' THEN @NhaCungCap END,
            @MaMoHinh, @TokenVao, @TokenRa, @DeXuatHanhTrinhJson
        );

        SET @TinNhanAIId = CONVERT(bigint, SCOPE_IDENTITY());

        INSERT dbo.NguonTraLoiAI (
            TinNhanAIId, NguoiDungId, ThuTu, DiSanId, TraiNghiemId, TuLieuId,
            TieuDeKhiTrich, UrlKhiTrich, DichVuId
        )
        SELECT @TinNhanAIId, @NguoiDungId, n.ThuTu,
               CASE WHEN n.Loai = ''DI_SAN'' THEN n.Id END,
               CASE WHEN n.Loai = ''TRAI_NGHIEM'' THEN n.Id END,
               CASE WHEN n.Loai = ''TU_LIEU'' THEN n.Id END,
               v.TieuDe, v.UrlNguon,
               CASE WHEN n.Loai = ''DICH_VU'' THEN n.Id END
        FROM @Nguon n
        JOIN dbo.vw_NguCanhTroLy v ON v.LoaiNguon = n.Loai AND v.NguonId = n.Id
        WHERE n.Loai <> ''DICH_VU'' OR @NhaCungCap = ''LOCAL'';

        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Nguon)
            THROW 51135, N''Có nguồn không tồn tại, chưa công khai hoặc sai loại.'', 1;

        COMMIT;
        IF @TraKetQua = 1 SELECT @TinNhanAIId AS TinNhanAIId;
    END;';

    /* 5. Lưu hành trình có khách sạn, quán ăn và dự toán */

    EXEC sys.sp_executesql N'
    CREATE OR ALTER PROCEDURE dbo.usp_TaoHanhTrinh
        @NguoiDungId int, @TieuDe nvarchar(160), @NgayBatDau date, @NgayKetThuc date,
        @MoTa nvarchar(1000) = NULL, @TinNhanAIId bigint = NULL,
        @KeHoachJson nvarchar(max) = NULL, @HanhTrinhId bigint = NULL OUTPUT, @TraKetQua bit = 1
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        SET @HanhTrinhId = NULL;

        DECLARE @SoNgay int = DATEDIFF(DAY, @NgayBatDau, @NgayKetThuc) + 1;
        IF @SoNgay IS NULL OR @SoNgay NOT BETWEEN 1 AND 90
            THROW 51136, N''Hành trình phải từ 1 đến 90 ngày.'', 1;

        BEGIN TRAN;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.NguoiDung WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1
        )
            THROW 51103, N''Tài khoản không tồn tại hoặc không hoạt động.'', 1;

        IF @TinNhanAIId IS NOT NULL
        BEGIN
            DECLARE @JsonDaLuu nvarchar(max);

            SELECT @JsonDaLuu = DeXuatHanhTrinhJson
            FROM dbo.TinNhanAI
            WHERE TinNhanAIId = @TinNhanAIId AND NguoiDungId = @NguoiDungId AND Vai = ''ASSISTANT'';

            IF @JsonDaLuu IS NULL
                THROW 51137, N''Tin nhắn không chứa đề xuất của tài khoản này.'', 1;

            SET @KeHoachJson = COALESCE(@KeHoachJson, @JsonDaLuu);
        END;

        DECLARE @SoNguoi smallint, @NganSachVnd decimal(14,0), @ChiPhiDiChuyenVnd decimal(14,0);
        DECLARE @Diem TABLE (
            Ngay int, ThuTu int, DiSanId int, TraiNghiemId int, DichVuId int,
            Gio nvarchar(50), GhiChu nvarchar(500),
            DonGiaVnd decimal(14,0), SoLuongTinhGia int, DonViTinhGia varchar(16)
        );

        IF @KeHoachJson IS NOT NULL
        BEGIN
            IF ISJSON(@KeHoachJson) <> 1 OR LEFT(LTRIM(@KeHoachJson), 1) <> ''{''
                THROW 51138, N''Kế hoạch phải là đối tượng JSON.'', 1;

            DECLARE @Mang nvarchar(max) = JSON_QUERY(@KeHoachJson, ''$.diem'');

            IF @Mang IS NULL OR LEFT(LTRIM(@Mang), 1) <> ''[''
                THROW 51138, N''Kế hoạch phải có mảng diem.'', 1;

            SELECT @SoNguoi = SoNguoi, @NganSachVnd = NganSachVnd, @ChiPhiDiChuyenVnd = ChiPhiDiChuyenVnd
            FROM OPENJSON(@KeHoachJson) WITH (
                SoNguoi smallint ''$.soNguoi'',
                NganSachVnd decimal(14,0) ''$.nganSachVnd'',
                ChiPhiDiChuyenVnd decimal(14,0) ''$.chiPhiDiChuyenVnd''
            );

            INSERT @Diem
            SELECT Ngay, ThuTu, DiSanId, TraiNghiemId, DichVuId, Gio, GhiChu,
                   DonGiaVnd, SoLuongTinhGia, DonViTinhGia
            FROM OPENJSON(@Mang) WITH (
                Ngay int ''$.ngay'', ThuTu int ''$.thuTu'',
                DiSanId int ''$.diSanId'', TraiNghiemId int ''$.traiNghiemId'',
                DichVuId int ''$.dichVuId'', Gio nvarchar(50) ''$.gio'',
                GhiChu nvarchar(500) ''$.ghiChu'', DonGiaVnd decimal(14,0) ''$.donGiaVnd'',
                SoLuongTinhGia int ''$.soLuongTinhGia'', DonViTinhGia varchar(16) ''$.donViTinhGia''
            );

            IF (SELECT COUNT(*) FROM @Diem) > 300
               OR EXISTS (SELECT 1 FROM @Diem GROUP BY Ngay, ThuTu HAVING COUNT(*) > 1)
               OR EXISTS (
                   SELECT 1
                   FROM @Diem x
                   LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = x.DiSanId
                   LEFT JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = x.TraiNghiemId
                   LEFT JOIN dbo.vw_DichVuDuLichCongKhai v ON v.DichVuId = x.DichVuId
                   WHERE x.Ngay IS NULL OR x.Ngay NOT BETWEEN 1 AND @SoNgay
                      OR x.ThuTu IS NULL OR x.ThuTu NOT BETWEEN 1 AND 100
                      OR (x.Gio IS NOT NULL AND TRY_CONVERT(time(0), x.Gio) IS NULL)
                      OR IIF(x.DiSanId IS NULL, 0, 1) + IIF(x.TraiNghiemId IS NULL, 0, 1)
                         + IIF(x.DichVuId IS NULL, 0, 1) <> 1
                      OR (x.DiSanId IS NOT NULL AND d.DiSanId IS NULL)
                      OR (x.TraiNghiemId IS NOT NULL AND e.TraiNghiemId IS NULL)
                      OR (x.DichVuId IS NOT NULL AND v.DichVuId IS NULL)
                      OR (x.DichVuId IS NOT NULL AND x.DonViTinhGia IS NOT NULL
                          AND x.DonViTinhGia <> v.DonViTinh)
               )
                THROW 51139, N''Ngày, thứ tự, giờ, địa điểm hoặc đơn vị tính giá không hợp lệ.'', 1;
        END;

        INSERT dbo.HanhTrinh (
            NguoiDungId, TieuDe, MoTa, NgayBatDau, NgayKetThuc, TinNhanAIId,
            SoNguoi, NganSachVnd, ChiPhiDiChuyenVnd
        )
        VALUES (
            @NguoiDungId, @TieuDe, @MoTa, @NgayBatDau, @NgayKetThuc, @TinNhanAIId,
            @SoNguoi, @NganSachVnd, @ChiPhiDiChuyenVnd
        );

        SET @HanhTrinhId = CONVERT(bigint, SCOPE_IDENTITY());

        INSERT dbo.DiemHanhTrinh (
            HanhTrinhId, NgayDuKien, ThuTu, DiSanId, TraiNghiemId, GioDuKien, GhiChu,
            DichVuId, DonGiaDuKienVnd, SoLuongTinhGia, DonViTinhGia
        )
        SELECT @HanhTrinhId, DATEADD(DAY, Ngay - 1, @NgayBatDau), ThuTu,
               DiSanId, TraiNghiemId, TRY_CONVERT(time(0), Gio), GhiChu,
               DichVuId, DonGiaVnd, SoLuongTinhGia, DonViTinhGia
        FROM @Diem;

        COMMIT;
        IF @TraKetQua = 1 SELECT @HanhTrinhId AS HanhTrinhId;
    END;';

    /* 6. Dữ liệu giả lập cho các khu vực demo */

    IF @ThemDuLieuDemo = 1
    BEGIN
        EXEC sys.sp_executesql N'
        DECLARE @Khu TABLE (
            MaKhu varchar(16) PRIMARY KEY, KhuVuc nvarchar(120),
            SlugDiSan varchar(160), MaTinhMacDinh varchar(32),
            ViDo decimal(9,6), KinhDo decimal(9,6), TinhThanhId int NULL
        );

        INSERT @Khu (MaKhu, KhuVuc, SlugDiSan, MaTinhMacDinh, ViDo, KinhDo)
        VALUES
            (''HUE'',      N''Đại Nội Huế'',          ''dai-noi-hue'',       ''HUE'',     16.467000, 107.590000),
            (''DA_NANG'',  N''Trung tâm Đà Nẵng'',    NULL,                  ''DA_NANG'', 16.067500, 108.211500),
            (''HOI_AN'',   N''Phố cổ Hội An'',        ''pho-co-hoi-an'',     ''DA_NANG'', 15.879000, 108.328000),
            (''THANH_HA'', N''Làng gốm Thanh Hà'',    ''lang-gom-thanh-ha'',  ''DA_NANG'', 15.880000, 108.296000),
            (''MY_SON'',   N''Thánh địa Mỹ Sơn'',     ''thanh-dia-my-son'',  ''DA_NANG'', 15.778000, 108.133000),
            (''BAT_TRANG'',N''Làng gốm Bát Tràng'',   ''lang-gom-bat-trang'', ''HA_NOI'',  20.977000, 105.913000);

        UPDATE k
        SET TinhThanhId = COALESCE(d.TinhThanhId, t.TinhThanhId)
        FROM @Khu k
        LEFT JOIN dbo.DiSan d ON d.Slug = k.SlugDiSan
        LEFT JOIN dbo.TinhThanh t ON t.Ma = k.MaTinhMacDinh;

        IF EXISTS (SELECT 1 FROM @Khu WHERE TinhThanhId IS NULL)
            THROW 51206, N''Thiếu tỉnh thành hoặc di sản cần để gắn dịch vụ mẫu.'', 1;

        DECLARE @Mau TABLE (
            MaKhu varchar(16), Slug varchar(160) PRIMARY KEY, Loai varchar(16),
            Ten nvarchar(200), GiaVnd decimal(14,0), SucChuaPhong smallint NULL
        );

        INSERT @Mau VALUES
            (''HUE'', ''demo-hue-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Sông Hương'', 420000, 2),
            (''HUE'', ''demo-hue-homestay-01'',  ''HOMESTAY'',  N''Homestay Vườn Huế'',   320000, 2),
            (''HUE'', ''demo-hue-quan-an-01'',   ''QUAN_AN'',   N''Bếp Cố Đô'',            65000, NULL),

            (''DA_NANG'', ''demo-da-nang-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Bờ Hàn'', 490000, 2),
            (''DA_NANG'', ''demo-da-nang-homestay-01'',  ''HOMESTAY'',  N''Homestay Gió Sông'', 370000, 2),
            (''DA_NANG'', ''demo-da-nang-quan-an-01'',   ''QUAN_AN'',   N''Bếp Mì Quảng'',      60000, NULL),

            (''HOI_AN'', ''demo-hoi-an-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Đèn Phố'', 460000, 2),
            (''HOI_AN'', ''demo-hoi-an-homestay-01'',  ''HOMESTAY'',  N''Homestay Hiên Ngói'', 350000, 2),
            (''HOI_AN'', ''demo-hoi-an-quan-an-01'',   ''QUAN_AN'',   N''Bếp Cao Lầu Phố'',    70000, NULL),

            (''THANH_HA'', ''demo-thanh-ha-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Bến Gốm'', 400000, 2),
            (''THANH_HA'', ''demo-thanh-ha-homestay-01'',  ''HOMESTAY'',  N''Homestay Sân Gốm'',  290000, 2),
            (''THANH_HA'', ''demo-thanh-ha-quan-an-01'',   ''QUAN_AN'',   N''Bếp Nhà Gốm'',        55000, NULL),

            (''MY_SON'', ''demo-my-son-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Đường Tháp'', 390000, 2),
            (''MY_SON'', ''demo-my-son-homestay-01'',  ''HOMESTAY'',  N''Homestay Vườn Tháp'',   280000, 2),
            (''MY_SON'', ''demo-my-son-quan-an-01'',   ''QUAN_AN'',   N''Bếp Xứ Quảng'',           60000, NULL),

            (''BAT_TRANG'', ''demo-bat-trang-khach-san-01'', ''KHACH_SAN'', N''Khách sạn Bến Sông'', 440000, 2),
            (''BAT_TRANG'', ''demo-bat-trang-homestay-01'',  ''HOMESTAY'',  N''Homestay Sân Men'',   330000, 2),
            (''BAT_TRANG'', ''demo-bat-trang-quan-an-01'',   ''QUAN_AN'',   N''Bếp Ven Làng'',         65000, NULL);

        INSERT dbo.DichVuDuLich (
            TinhThanhId, Slug, Loai, Ten, KhuVuc, DiaChi, MoTa,
            ViDo, KinhDo, DonViTinh, SucChuaPhong, TrangThai, LaDuLieuDemo
        )
        SELECT k.TinhThanhId, m.Slug, m.Loai, m.Ten, k.KhuVuc,
               CONCAT(N''Vị trí giả lập quanh '', k.KhuVuc),
               CASE WHEN m.Loai = ''QUAN_AN''
                    THEN N''Quán ăn giả lập. Giá tính cho một người trong một bữa.''
                    ELSE N''Chỗ ở giả lập. Giá tính theo phòng hai người cho một đêm.'' END,
               k.ViDo + CASE m.Loai WHEN ''HOMESTAY'' THEN 0.002 WHEN ''QUAN_AN'' THEN -0.001 ELSE 0 END,
               k.KinhDo + CASE m.Loai WHEN ''HOMESTAY'' THEN -0.002 WHEN ''QUAN_AN'' THEN 0.001 ELSE 0 END,
               CASE WHEN m.Loai = ''QUAN_AN'' THEN ''NGUOI_BUA'' ELSE ''PHONG_DEM'' END,
               m.SucChuaPhong, ''CONG_KHAI'', 1
        FROM @Mau m
        JOIN @Khu k ON k.MaKhu = m.MaKhu
        WHERE NOT EXISTS (SELECT 1 FROM dbo.DichVuDuLich d WHERE d.Slug = m.Slug);

        -- Gắn khu vực cho dữ liệu Huế đã thêm ở lần chạy trước, nếu có.
        UPDATE d SET KhuVuc = N''Đại Nội Huế''
        FROM dbo.DichVuDuLich d
        WHERE d.LaDuLieuDemo = 1 AND d.KhuVuc IS NULL
          AND d.Slug IN (
              ''demo-hue-khach-san-01'', ''demo-hue-homestay-01'',
              ''demo-hue-quan-an-01'', ''demo-hue-quan-an-02''
          );

        DECLARE @NgayGia date = CONVERT(date, ''20250101'', 112);

        INSERT dbo.GiaDichVuDuLich (DichVuId, NgayApDung, GiaVnd, NguonGia)
        SELECT d.DichVuId, @NgayGia, m.GiaVnd, N''Giá giả lập cho đồ án Dấu Chân Di Sản''
        FROM @Mau m
        JOIN dbo.DichVuDuLich d ON d.Slug = m.Slug AND d.LaDuLieuDemo = 1
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.GiaDichVuDuLich g
            WHERE g.DichVuId = d.DichVuId AND g.NgayApDung = @NgayGia
        );';
    END;

    COMMIT;
    PRINT N'Đã cập nhật CSDL hành trình và dịch vụ du lịch.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;