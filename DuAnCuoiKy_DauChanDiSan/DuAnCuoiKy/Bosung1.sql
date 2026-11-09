USE [DauChanDiSan];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'DauChanDiSan:BoSungDichVuDuLich',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 10000;

    IF @LockResult < 0
        THROW 52100, N'Không lấy được khóa cập nhật CSDL. Hãy thử lại.', 1;

    IF OBJECT_ID(N'dbo.TinhThanh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.HanhTrinh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.DiemHanhTrinh', N'U') IS NULL
       OR OBJECT_ID(N'dbo.TinNhanAI', N'U') IS NULL
       OR OBJECT_ID(N'dbo.NguonTraLoiAI', N'U') IS NULL
       OR OBJECT_ID(N'dbo.vw_NguCanhGemini', N'V') IS NULL
        THROW 52101, N'CSDL thiếu bảng hoặc view gốc của Dấu Chân Di Sản.', 1;

    -------------------------------------------------------------------
    -- 1. Danh mục khách sạn, homestay, quán ăn và giá tham khảo
    -------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.DichVuDuLich', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DichVuDuLich
        (
            DichVuId int IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_DichVuDuLich PRIMARY KEY,
            TinhThanhId int NOT NULL
                CONSTRAINT FK_DichVuDuLich_TinhThanh REFERENCES dbo.TinhThanh(TinhThanhId),
            Slug varchar(160) NOT NULL
                CONSTRAINT UQ_DichVuDuLich_Slug UNIQUE,
            Loai varchar(16) NOT NULL,
            Ten nvarchar(200) NOT NULL,
            DiaChi nvarchar(300) NOT NULL,
            MoTa nvarchar(1000) NULL,
            ViDo decimal(9,6) NOT NULL,
            KinhDo decimal(9,6) NOT NULL,
            DonViTinh varchar(16) NOT NULL,
            SucChuaPhong smallint NULL,
            TrangThai varchar(16) NOT NULL
                CONSTRAINT DF_DichVuDuLich_TrangThai DEFAULT ('NHAP'),
            LaDuLieuDemo bit NOT NULL
                CONSTRAINT DF_DichVuDuLich_Demo DEFAULT (0),
            TaoLucUtc datetime2(3) NOT NULL
                CONSTRAINT DF_DichVuDuLich_TaoLuc DEFAULT SYSUTCDATETIME(),

            CONSTRAINT CK_DichVuDuLich_Ten
                CHECK (LEN(LTRIM(Ten)) > 0 AND LEN(LTRIM(DiaChi)) > 0),
            CONSTRAINT CK_DichVuDuLich_ToaDo
                CHECK (ViDo BETWEEN -90 AND 90 AND KinhDo BETWEEN -180 AND 180),
            CONSTRAINT CK_DichVuDuLich_TrangThai
                CHECK (TrangThai IN ('NHAP', 'CONG_KHAI', 'AN')),
            CONSTRAINT CK_DichVuDuLich_LoaiVaDonVi CHECK
            (
                (Loai IN ('KHACH_SAN', 'HOMESTAY')
                    AND DonViTinh = 'PHONG_DEM'
                    AND SucChuaPhong IS NOT NULL
                    AND SucChuaPhong BETWEEN 1 AND 30)
                OR
                (Loai = 'QUAN_AN'
                    AND DonViTinh = 'NGUOI_BUA'
                    AND SucChuaPhong IS NULL)
            )
        );

        CREATE INDEX IX_DichVuDuLich_Tinh_Loai_TrangThai
            ON dbo.DichVuDuLich(TinhThanhId, Loai, TrangThai);
    END;

    IF OBJECT_ID(N'dbo.GiaDichVuDuLich', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.GiaDichVuDuLich
        (
            GiaDichVuId bigint IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_GiaDichVuDuLich PRIMARY KEY,
            DichVuId int NOT NULL
                CONSTRAINT FK_GiaDichVuDuLich_DichVu REFERENCES dbo.DichVuDuLich(DichVuId),
            NgayApDung date NOT NULL,
            GiaVnd decimal(12,0) NOT NULL,
            NguonGia nvarchar(300) NOT NULL,
            CapNhatLucUtc datetime2(3) NOT NULL
                CONSTRAINT DF_GiaDichVuDuLich_CapNhat DEFAULT SYSUTCDATETIME(),

            CONSTRAINT CK_GiaDichVuDuLich_Gia CHECK (GiaVnd >= 0),
            CONSTRAINT UQ_GiaDichVuDuLich_DichVu_Ngay UNIQUE (DichVuId, NgayApDung)
        );
    END;

    -------------------------------------------------------------------
    -- 2. Ngân sách và giá dự kiến lưu cùng chuyến đi
    -------------------------------------------------------------------
    IF COL_LENGTH(N'dbo.HanhTrinh', N'SoNguoi') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD SoNguoi smallint NULL;

    IF COL_LENGTH(N'dbo.HanhTrinh', N'NganSachVnd') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD NganSachVnd decimal(14,0) NULL;

    IF COL_LENGTH(N'dbo.HanhTrinh', N'ChiPhiDiChuyenVnd') IS NULL
        ALTER TABLE dbo.HanhTrinh ADD ChiPhiDiChuyenVnd decimal(14,0) NULL;

    IF OBJECT_ID(N'dbo.CK_HanhTrinh_NganSachVaSoNguoi', N'C') IS NULL
        EXEC(N'ALTER TABLE dbo.HanhTrinh WITH CHECK
            ADD CONSTRAINT CK_HanhTrinh_NganSachVaSoNguoi CHECK
            (
                (SoNguoi IS NULL OR SoNguoi BETWEEN 1 AND 100)
                AND (NganSachVnd IS NULL OR NganSachVnd >= 0)
                AND (ChiPhiDiChuyenVnd IS NULL OR ChiPhiDiChuyenVnd >= 0)
            );');

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DichVuId') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DichVuId int NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DonGiaDuKienVnd') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DonGiaDuKienVnd decimal(12,0) NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'SoLuongTinhGia') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD SoLuongTinhGia smallint NULL;

    IF COL_LENGTH(N'dbo.DiemHanhTrinh', N'DonViTinhGia') IS NULL
        ALTER TABLE dbo.DiemHanhTrinh ADD DonViTinhGia varchar(16) NULL;

    IF OBJECT_ID(N'dbo.FK_DiemHanhTrinh_DichVuDuLich', N'F') IS NULL
        EXEC(N'ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT FK_DiemHanhTrinh_DichVuDuLich
            FOREIGN KEY (DichVuId) REFERENCES dbo.DichVuDuLich(DichVuId);');

    IF OBJECT_ID(N'dbo.CK_DiemHanhTrinh_MotLoaiDiem', N'C') IS NULL
    BEGIN
        DECLARE @OldPointCheck sysname, @PointCheckCount int;

        SELECT @OldPointCheck = MIN(name), @PointCheckCount = COUNT(*)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.DiemHanhTrinh')
          AND definition LIKE N'%DiSanId%'
          AND definition LIKE N'%TraiNghiemId%'
          AND definition NOT LIKE N'%DichVuId%';

        IF @PointCheckCount <> 1
            THROW 52102, N'Không xác định được ràng buộc loại điểm cũ của DiemHanhTrinh.', 1;

        EXEC(N'ALTER TABLE dbo.DiemHanhTrinh DROP CONSTRAINT '
            + QUOTENAME(@OldPointCheck) + N';');

        EXEC(N'ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT CK_DiemHanhTrinh_MotLoaiDiem CHECK
            (
                CASE WHEN DiSanId IS NULL THEN 0 ELSE 1 END
                + CASE WHEN TraiNghiemId IS NULL THEN 0 ELSE 1 END
                + CASE WHEN DichVuId IS NULL THEN 0 ELSE 1 END = 1
            );');
    END;

    IF OBJECT_ID(N'dbo.CK_DiemHanhTrinh_GiaDuKien', N'C') IS NULL
        EXEC(N'ALTER TABLE dbo.DiemHanhTrinh WITH CHECK
            ADD CONSTRAINT CK_DiemHanhTrinh_GiaDuKien CHECK
            (
                (DonGiaDuKienVnd IS NULL AND SoLuongTinhGia IS NULL AND DonViTinhGia IS NULL)
                OR
                (DonGiaDuKienVnd IS NOT NULL AND DonGiaDuKienVnd >= 0
                    AND SoLuongTinhGia IS NOT NULL AND SoLuongTinhGia BETWEEN 1 AND 1000
                    AND DonViTinhGia IS NOT NULL
                    AND DonViTinhGia IN (''NGUOI_LUOT'', ''PHONG_DEM'', ''NGUOI_BUA''))
            );');

    -------------------------------------------------------------------
    -- 3. Hội thoại local và nguồn trích dẫn dịch vụ
    -------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.CK_TinNhanAI_NhaCungCap', N'C') IS NULL
    BEGIN
        DECLARE @OldProviderCheck sysname, @ProviderCheckCount int;

        SELECT @OldProviderCheck = MIN(name), @ProviderCheckCount = COUNT(*)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.TinNhanAI')
          AND definition LIKE N'%NhaCungCap%'
          AND definition LIKE N'%GEMINI%'
          AND definition NOT LIKE N'%LOCAL%'
          AND definition NOT LIKE N'%MaMoHinh%';

        IF @ProviderCheckCount <> 1
            THROW 52103, N'Không xác định được ràng buộc nhà cung cấp cũ của TinNhanAI.', 1;

        EXEC(N'ALTER TABLE dbo.TinNhanAI DROP CONSTRAINT '
            + QUOTENAME(@OldProviderCheck) + N';');

        ALTER TABLE dbo.TinNhanAI WITH CHECK
            ADD CONSTRAINT CK_TinNhanAI_NhaCungCap
            CHECK (NhaCungCap IS NULL OR NhaCungCap IN ('GEMINI', 'LOCAL'));
    END;

    IF COL_LENGTH(N'dbo.NguonTraLoiAI', N'DichVuId') IS NULL
        ALTER TABLE dbo.NguonTraLoiAI ADD DichVuId int NULL;

    IF OBJECT_ID(N'dbo.FK_NguonTraLoiAI_DichVuDuLich', N'F') IS NULL
        EXEC(N'ALTER TABLE dbo.NguonTraLoiAI WITH CHECK
            ADD CONSTRAINT FK_NguonTraLoiAI_DichVuDuLich
            FOREIGN KEY (DichVuId) REFERENCES dbo.DichVuDuLich(DichVuId);');

    IF OBJECT_ID(N'dbo.CK_NguonTraLoiAI_MotLoaiNguon', N'C') IS NULL
    BEGIN
        DECLARE @OldSourceCheck sysname, @SourceCheckCount int;

        SELECT @OldSourceCheck = MIN(name), @SourceCheckCount = COUNT(*)
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.NguonTraLoiAI')
          AND definition LIKE N'%DiSanId%'
          AND definition LIKE N'%TraiNghiemId%'
          AND definition LIKE N'%TuLieuId%'
          AND definition NOT LIKE N'%DichVuId%';

        IF @SourceCheckCount <> 1
            THROW 52104, N'Không xác định được ràng buộc loại nguồn cũ của NguonTraLoiAI.', 1;

        EXEC(N'ALTER TABLE dbo.NguonTraLoiAI DROP CONSTRAINT '
            + QUOTENAME(@OldSourceCheck) + N';');

        EXEC(N'ALTER TABLE dbo.NguonTraLoiAI WITH CHECK
            ADD CONSTRAINT CK_NguonTraLoiAI_MotLoaiNguon CHECK
            (
                CASE WHEN DiSanId IS NULL THEN 0 ELSE 1 END
                + CASE WHEN TraiNghiemId IS NULL THEN 0 ELSE 1 END
                + CASE WHEN TuLieuId IS NULL THEN 0 ELSE 1 END
                + CASE WHEN DichVuId IS NULL THEN 0 ELSE 1 END = 1
            );');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-----------------------------------------------------------------------
-- 4. Tra dịch vụ công khai và giá gần nhất tại ngày được hỏi
-----------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_DichVuDuLichCongKhai AS
SELECT d.DichVuId, d.TinhThanhId, t.Ten AS TinhThanh, d.Slug, d.Loai,
       d.Ten, d.DiaChi, d.MoTa, d.ViDo, d.KinhDo,
       d.DonViTinh, d.SucChuaPhong, d.LaDuLieuDemo
FROM dbo.DichVuDuLich d
JOIN dbo.TinhThanh t ON t.TinhThanhId = d.TinhThanhId AND t.HoatDong = 1
WHERE d.TrangThai = 'CONG_KHAI';
GO

CREATE OR ALTER PROCEDURE dbo.usp_LayDichVuDuLich
    @TinhThanhId int,
    @Loai varchar(16) = NULL,
    @Ngay date = NULL,
    @GioiHan int = 20,
    @BaoGomDemo bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @TinhThanhId IS NULL OR @GioiHan IS NULL OR @GioiHan NOT BETWEEN 1 AND 100
        THROW 52105, N'Thiếu tỉnh thành hoặc giới hạn không hợp lệ.', 1;

    IF @Loai IS NOT NULL AND @Loai NOT IN ('KHACH_SAN', 'HOMESTAY', 'QUAN_AN')
        THROW 52106, N'Loại dịch vụ không hợp lệ.', 1;

    SET @Ngay = COALESCE(@Ngay, CONVERT(date, DATEADD(HOUR, 7, SYSUTCDATETIME())));

    SELECT TOP (@GioiHan) d.*,
           gia.GiaVnd, gia.NgayApDung, gia.NguonGia, gia.CapNhatLucUtc
    FROM dbo.vw_DichVuDuLichCongKhai d
    OUTER APPLY
    (
        SELECT TOP (1) g.GiaVnd, g.NgayApDung, g.NguonGia, g.CapNhatLucUtc
        FROM dbo.GiaDichVuDuLich g
        WHERE g.DichVuId = d.DichVuId AND g.NgayApDung <= @Ngay
        ORDER BY g.NgayApDung DESC
    ) gia
    WHERE d.TinhThanhId = @TinhThanhId
      AND (@Loai IS NULL OR d.Loai = @Loai)
      AND (@BaoGomDemo = 1 OR d.LaDuLieuDemo = 0)
    ORDER BY CASE WHEN gia.GiaVnd IS NULL THEN 1 ELSE 0 END,
             gia.GiaVnd, d.DichVuId;
END;
GO

CREATE OR ALTER VIEW dbo.vw_NguCanhTroLy AS
SELECT LoaiNguon, NguonId, DiSanId, TinhThanhId, TieuDe, NoiDung, UrlNguon
FROM dbo.vw_NguCanhGemini
UNION ALL
SELECT CONVERT(varchar(16), 'DICH_VU'), d.DichVuId, CONVERT(int, NULL),
       d.TinhThanhId, CONVERT(nvarchar(220), d.Ten),
       CONVERT(nvarchar(max), CONCAT(d.DiaChi, N' · ', d.MoTa)),
       CONVERT(nvarchar(1000), NULL)
FROM dbo.vw_DichVuDuLichCongKhai d;
GO

-----------------------------------------------------------------------
-- 5. Giữ tên thủ tục cũ, thêm tham số LOCAL ở cuối
-----------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_LuuTinNhanGemini
    @NguoiDungId int,
    @HoiThoaiId bigint,
    @Vai varchar(12),
    @NoiDung nvarchar(max),
    @MaMoHinh varchar(100) = NULL,
    @TokenVao int = NULL,
    @TokenRa int = NULL,
    @DeXuatHanhTrinhJson nvarchar(max) = NULL,
    @NguonJson nvarchar(max) = NULL,
    @TinNhanAIId bigint = NULL OUTPUT,
    @TraKetQua bit = 1,
    @NhaCungCap varchar(16) = 'GEMINI'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @TinNhanAIId = NULL;

    IF @Vai IS NULL OR @Vai NOT IN ('USER', 'ASSISTANT')
        THROW 52107, N'Vai trò tin nhắn không hợp lệ.', 1;

    IF @Vai = 'ASSISTANT' AND (@NhaCungCap IS NULL OR @NhaCungCap NOT IN ('GEMINI', 'LOCAL'))
        THROW 52108, N'Nhà cung cấp mô hình không hợp lệ.', 1;

    IF @NguonJson IS NOT NULL
       AND (@Vai <> 'ASSISTANT' OR ISJSON(@NguonJson) <> 1
            OR LEFT(LTRIM(@NguonJson), 1) <> '[')
        THROW 51131, N'Nguồn phải là mảng JSON của tin nhắn trợ lý.', 1;

    IF @DeXuatHanhTrinhJson IS NOT NULL
       AND (@Vai <> 'ASSISTANT' OR ISJSON(@DeXuatHanhTrinhJson) <> 1
            OR LEFT(LTRIM(@DeXuatHanhTrinhJson), 1) <> '{')
        THROW 51132, N'Kế hoạch phải là đối tượng JSON của tin nhắn trợ lý.', 1;

    DECLARE @Nguon TABLE (ThuTu int PRIMARY KEY, Loai varchar(16), Id int);

    IF @NguonJson IS NOT NULL
        INSERT @Nguon(ThuTu, Loai, Id)
        SELECT CONVERT(int, j.[key]) + 1, p.Loai, p.Id
        FROM OPENJSON(@NguonJson) j
        OUTER APPLY OPENJSON(CASE WHEN j.[type] = 5 THEN j.value ELSE N'{}' END)
            WITH (Loai varchar(16) '$.loai', Id int '$.id') p;

    IF (SELECT COUNT(*) FROM @Nguon) > 40
       OR EXISTS (SELECT 1 FROM @Nguon GROUP BY Loai, Id HAVING COUNT(*) > 1)
        THROW 51133, N'Danh sách nguồn bị trùng hoặc vượt giới hạn 40 mục.', 1;

    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.HoiThoaiAI h WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.NguoiDung u ON u.NguoiDungId = h.NguoiDungId AND u.HoatDong = 1
        WHERE h.HoiThoaiId = @HoiThoaiId
          AND h.NguoiDungId = @NguoiDungId
          AND h.DaLuuTru = 0
    )
        THROW 51134, N'Hội thoại không khả dụng cho tài khoản này.', 1;

    INSERT dbo.TinNhanAI
        (HoiThoaiId, NguoiDungId, Vai, NoiDung, NhaCungCap, MaMoHinh,
         TokenVao, TokenRa, DeXuatHanhTrinhJson)
    VALUES
        (@HoiThoaiId, @NguoiDungId, @Vai, @NoiDung,
         CASE WHEN @Vai = 'ASSISTANT' THEN @NhaCungCap END,
         @MaMoHinh, @TokenVao, @TokenRa, @DeXuatHanhTrinhJson);

    SET @TinNhanAIId = CONVERT(bigint, SCOPE_IDENTITY());

    INSERT dbo.NguonTraLoiAI
        (TinNhanAIId, NguoiDungId, ThuTu, DiSanId, TraiNghiemId,
         TuLieuId, DichVuId, TieuDeKhiTrich, UrlKhiTrich)
    SELECT @TinNhanAIId, @NguoiDungId, n.ThuTu,
           CASE WHEN n.Loai = 'DI_SAN' THEN n.Id END,
           CASE WHEN n.Loai = 'TRAI_NGHIEM' THEN n.Id END,
           CASE WHEN n.Loai = 'TU_LIEU' THEN n.Id END,
           CASE WHEN n.Loai = 'DICH_VU' THEN n.Id END,
           v.TieuDe, v.UrlNguon
    FROM @Nguon n
    JOIN dbo.vw_NguCanhTroLy v ON v.LoaiNguon = n.Loai AND v.NguonId = n.Id
    WHERE @NhaCungCap = 'LOCAL' OR n.Loai <> 'DICH_VU';

    IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Nguon)
        THROW 51135, N'Có nguồn không tồn tại, chưa công khai hoặc sai loại.', 1;

    COMMIT TRANSACTION;

    IF @TraKetQua = 1 SELECT @TinNhanAIId AS TinNhanAIId;
END;
GO

-----------------------------------------------------------------------
-- 6. Lưu hành trình có điểm ăn, ở và chi phí dự kiến
-----------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.usp_TaoHanhTrinh
    @NguoiDungId int,
    @TieuDe nvarchar(160),
    @NgayBatDau date,
    @NgayKetThuc date,
    @MoTa nvarchar(1000) = NULL,
    @TinNhanAIId bigint = NULL,
    @KeHoachJson nvarchar(max) = NULL,
    @HanhTrinhId bigint = NULL OUTPUT,
    @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @HanhTrinhId = NULL;

    DECLARE @SoNgay int = DATEDIFF(DAY, @NgayBatDau, @NgayKetThuc) + 1;

    IF @SoNgay IS NULL OR @SoNgay NOT BETWEEN 1 AND 90
        THROW 51136, N'Khoảng ngày hành trình phải hợp lệ và tối đa 90 ngày.', 1;

    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.NguoiDung
        WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1
    )
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;

    IF @TinNhanAIId IS NOT NULL
    BEGIN
        DECLARE @JsonDaLuu nvarchar(max);

        SELECT @JsonDaLuu = DeXuatHanhTrinhJson
        FROM dbo.TinNhanAI
        WHERE TinNhanAIId = @TinNhanAIId
          AND NguoiDungId = @NguoiDungId
          AND Vai = 'ASSISTANT';

        IF @JsonDaLuu IS NULL
            THROW 51137, N'Tin nhắn không chứa đề xuất hành trình của tài khoản này.', 1;

        SET @KeHoachJson = COALESCE(@KeHoachJson, @JsonDaLuu);
    END;

    DECLARE @SoNguoi smallint, @NganSachVnd decimal(14,0),
            @ChiPhiDiChuyenVnd decimal(14,0);

    DECLARE @Diem TABLE
    (
        Ngay int, ThuTu int, DiSanId int, TraiNghiemId int, DichVuId int,
        Gio nvarchar(50), GhiChu nvarchar(500), DonGiaVnd decimal(12,0),
        SoLuong smallint, DonVi varchar(16)
    );

    IF @KeHoachJson IS NOT NULL
    BEGIN
        IF ISJSON(@KeHoachJson) <> 1 OR LEFT(LTRIM(@KeHoachJson), 1) <> '{'
            THROW 51138, N'Kế hoạch phải là đối tượng JSON.', 1;

        DECLARE @Mang nvarchar(max) = JSON_QUERY(@KeHoachJson, '$.diem');

        IF @Mang IS NULL OR LEFT(LTRIM(@Mang), 1) <> '['
            THROW 51138, N'Kế hoạch phải có mảng diem.', 1;

        SELECT @SoNguoi = SoNguoi, @NganSachVnd = NganSachVnd,
               @ChiPhiDiChuyenVnd = ChiPhiDiChuyenVnd
        FROM OPENJSON(@KeHoachJson) WITH
        (
            SoNguoi smallint '$.soNguoi',
            NganSachVnd decimal(14,0) '$.nganSachVnd',
            ChiPhiDiChuyenVnd decimal(14,0) '$.chiPhiDiChuyenVnd'
        );

        INSERT @Diem
        SELECT Ngay, ThuTu, DiSanId, TraiNghiemId, DichVuId,
               Gio, GhiChu, DonGiaVnd, SoLuong, DonVi
        FROM OPENJSON(@Mang) WITH
        (
            Ngay int '$.ngay',
            ThuTu int '$.thuTu',
            DiSanId int '$.diSanId',
            TraiNghiemId int '$.traiNghiemId',
            DichVuId int '$.dichVuId',
            Gio nvarchar(50) '$.gio',
            GhiChu nvarchar(500) '$.ghiChu',
            DonGiaVnd decimal(12,0) '$.donGiaVnd',
            SoLuong smallint '$.soLuongTinhGia',
            DonVi varchar(16) '$.donViTinhGia'
        );

        IF (SELECT COUNT(*) FROM @Diem) > 300 OR EXISTS
        (
            SELECT 1
            FROM @Diem x
            LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = x.DiSanId
            LEFT JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = x.TraiNghiemId
            LEFT JOIN dbo.vw_DichVuDuLichCongKhai s ON s.DichVuId = x.DichVuId
            WHERE x.Ngay IS NULL OR x.Ngay NOT BETWEEN 1 AND @SoNgay
               OR x.ThuTu IS NULL OR x.ThuTu NOT BETWEEN 1 AND 100
               OR (x.Gio IS NOT NULL AND TRY_CONVERT(time(0), x.Gio) IS NULL)
               OR IIF(x.DiSanId IS NULL, 0, 1)
                + IIF(x.TraiNghiemId IS NULL, 0, 1)
                + IIF(x.DichVuId IS NULL, 0, 1) <> 1
               OR (x.DiSanId IS NOT NULL AND d.DiSanId IS NULL)
               OR (x.TraiNghiemId IS NOT NULL AND e.TraiNghiemId IS NULL)
               OR (x.DichVuId IS NOT NULL AND s.DichVuId IS NULL)
               OR (x.DichVuId IS NOT NULL AND x.DonVi IS NOT NULL AND x.DonVi <> s.DonViTinh)
               OR (x.DichVuId IS NULL AND x.DonVi IS NOT NULL AND x.DonVi <> 'NGUOI_LUOT')
        ) OR EXISTS (SELECT 1 FROM @Diem GROUP BY Ngay, ThuTu HAVING COUNT(*) > 1)
            THROW 51139, N'Kế hoạch có ngày, thứ tự, giờ, địa điểm hoặc đơn vị giá không hợp lệ.', 1;
    END;

    INSERT dbo.HanhTrinh
        (NguoiDungId, TieuDe, MoTa, NgayBatDau, NgayKetThuc, TinNhanAIId,
         SoNguoi, NganSachVnd, ChiPhiDiChuyenVnd)
    VALUES
        (@NguoiDungId, @TieuDe, @MoTa, @NgayBatDau, @NgayKetThuc, @TinNhanAIId,
         @SoNguoi, @NganSachVnd, @ChiPhiDiChuyenVnd);

    SET @HanhTrinhId = CONVERT(bigint, SCOPE_IDENTITY());

    INSERT dbo.DiemHanhTrinh
        (HanhTrinhId, NgayDuKien, ThuTu, DiSanId, TraiNghiemId, DichVuId,
         GioDuKien, GhiChu, DonGiaDuKienVnd, SoLuongTinhGia, DonViTinhGia)
    SELECT @HanhTrinhId, DATEADD(DAY, Ngay - 1, @NgayBatDau),
           ThuTu, DiSanId, TraiNghiemId, DichVuId,
           TRY_CONVERT(time(0), Gio), GhiChu, DonGiaVnd, SoLuong, DonVi
    FROM @Diem;

    COMMIT TRANSACTION;

    IF @TraKetQua = 1 SELECT @HanhTrinhId AS HanhTrinhId;
END;
GO

-----------------------------------------------------------------------
-- 7. Tổng dự kiến và số điểm còn thiếu giá
-----------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_ChiPhiHanhTrinh AS
SELECT h.HanhTrinhId, h.NguoiDungId, h.SoNguoi, h.NganSachVnd,
       h.ChiPhiDiChuyenVnd,
       COALESCE(c.ChiPhiCacDiemVnd, 0) AS ChiPhiCacDiemDaCoGiaVnd,
       COALESCE(c.ChiPhiCacDiemVnd, 0) + COALESCE(h.ChiPhiDiChuyenVnd, 0)
           AS TongDaCoGiaVnd,
       c.SoDiemChuaCoGia
FROM dbo.HanhTrinh h
OUTER APPLY
(
    SELECT SUM(CONVERT(decimal(19,0), d.DonGiaDuKienVnd * d.SoLuongTinhGia))
               AS ChiPhiCacDiemVnd,
           COUNT(CASE WHEN d.DonGiaDuKienVnd IS NULL THEN 1 END) AS SoDiemChuaCoGia
    FROM dbo.DiemHanhTrinh d
    WHERE d.HanhTrinhId = h.HanhTrinhId
) c;
GO

-----------------------------------------------------------------------
-- 8. Dữ liệu giả lập ở Huế. Đổi thành 0 nếu chỉ muốn tạo cấu trúc.
-----------------------------------------------------------------------
DECLARE @ThemDuLieuDemo bit = 1;
DECLARE @Hue int = (SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma = 'HUE');
DECLARE @NgayGiaDemo date = '2025-01-01';

IF @ThemDuLieuDemo = 1 AND @Hue IS NOT NULL
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @SeedLock int;
        EXEC @SeedLock = sys.sp_getapplock
            @Resource = N'DauChanDiSan:DuLieuDichVuHue',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @SeedLock < 0
            THROW 52109, N'Không lấy được khóa thêm dịch vụ demo.', 1;

        DECLARE @Demo TABLE
        (
            Slug varchar(160) PRIMARY KEY,
            Loai varchar(16),
            Ten nvarchar(200),
            ViDo decimal(9,6),
            KinhDo decimal(9,6),
            DonViTinh varchar(16),
            SucChuaPhong smallint NULL,
            GiaVnd decimal(12,0)
        );

        INSERT @Demo VALUES
            ('demo-hue-khach-san-01', 'KHACH_SAN',
             N'[DEMO] Khách sạn Bên Sông', 16.467000, 107.590000, 'PHONG_DEM', 2, 420000),
            ('demo-hue-homestay-01', 'HOMESTAY',
             N'[DEMO] Homestay Gác Nhỏ', 16.470000, 107.584000, 'PHONG_DEM', 2, 320000),
            ('demo-hue-quan-an-01', 'QUAN_AN',
             N'[DEMO] Quán Cơm Nhà', 16.466000, 107.588000, 'NGUOI_BUA', NULL, 65000),
            ('demo-hue-quan-an-02', 'QUAN_AN',
             N'[DEMO] Quán Bún Sáng', 16.472000, 107.587000, 'NGUOI_BUA', NULL, 45000);

        INSERT dbo.DichVuDuLich
            (TinhThanhId, Slug, Loai, Ten, DiaChi, MoTa, ViDo, KinhDo,
             DonViTinh, SucChuaPhong, TrangThai, LaDuLieuDemo)
        SELECT @Hue, d.Slug, d.Loai, d.Ten, N'Huế · Vị trí giả lập',
               N'Dữ liệu minh họa để thử chatbot và tính ngân sách.',
               d.ViDo, d.KinhDo, d.DonViTinh, d.SucChuaPhong, 'CONG_KHAI', 1
        FROM @Demo d
        WHERE NOT EXISTS (SELECT 1 FROM dbo.DichVuDuLich x WHERE x.Slug = d.Slug);

        INSERT dbo.GiaDichVuDuLich(DichVuId, NgayApDung, GiaVnd, NguonGia)
        SELECT x.DichVuId, @NgayGiaDemo, d.GiaVnd, N'Giá giả lập cho đồ án'
        FROM @Demo d
        JOIN dbo.DichVuDuLich x ON x.Slug = d.Slug AND x.LaDuLieuDemo = 1
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.GiaDichVuDuLich g
            WHERE g.DichVuId = x.DichVuId AND g.NgayApDung = @NgayGiaDemo
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

-- Kiểm tra danh mục và giá trong tab Results của SSMS.
IF @Hue IS NOT NULL
    EXEC dbo.usp_LayDichVuDuLich
        @TinhThanhId = @Hue, @GioiHan = 20, @BaoGomDemo = 1;
ELSE
    PRINT N'Chưa có tỉnh thành mã HUE. Dữ liệu demo Huế được bỏ qua.';
GO