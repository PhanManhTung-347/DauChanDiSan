
USE master;
GO
IF DB_ID(N'DauChanDiSan') IS NULL
    EXEC(N'CREATE DATABASE [DauChanDiSan]');
GO
USE [DauChanDiSan];
GO
IF OBJECT_ID(N'dbo.PhienBanCSDL', N'U') IS NOT NULL
BEGIN
    PRINT N'DauChanDiSan đã được cài đặt. Bỏ qua script khởi tạo.';
    SET NOEXEC ON;
END
ELSE IF EXISTS (SELECT 1 FROM sys.objects WHERE is_ms_shipped = 0 AND type IN ('U','P','V','FN','IF','TF'))
BEGIN
    RAISERROR(N'DauChanDiSan đã có đối tượng khác. Hãy cài trên cơ sở dữ liệu trống.', 16, 1);
    SET NOEXEC ON;
END;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

-- 1. Tài khoản Firebase, phân quyền và danh mục

CREATE TABLE dbo.VaiTro (
    MaVaiTro varchar(24) NOT NULL PRIMARY KEY,
    Ten nvarchar(80) NOT NULL UNIQUE
);
CREATE TABLE dbo.NguoiDung (
    NguoiDungId int IDENTITY PRIMARY KEY,
    FirebaseUid nvarchar(128) COLLATE Latin1_General_100_BIN2 NOT NULL UNIQUE,
    HoTen nvarchar(120) NOT NULL,
    Email nvarchar(254) NULL,
    EmailDaXacMinh bit NOT NULL DEFAULT 0,
    AnhDaiDienPath nvarchar(450) NULL,
    HoatDong bit NOT NULL DEFAULT 1,
    ChoPhepCaNhanHoa bit NOT NULL DEFAULT 0,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LanDongBoUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CHECK (LEN(LTRIM(FirebaseUid)) > 0 AND DATALENGTH(FirebaseUid) = DATALENGTH(RTRIM(FirebaseUid))),
    CHECK (LEN(LTRIM(HoTen)) > 0 AND (EmailDaXacMinh = 0 OR Email IS NOT NULL))
);
CREATE TABLE dbo.NguoiDungVaiTro (
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    MaVaiTro varchar(24) NOT NULL REFERENCES dbo.VaiTro(MaVaiTro),
    CapLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (NguoiDungId, MaVaiTro)
);
CREATE TABLE dbo.TinhThanh (
    TinhThanhId int IDENTITY PRIMARY KEY,
    Ma varchar(32) NOT NULL UNIQUE,
    Ten nvarchar(100) NOT NULL UNIQUE,
    HoatDong bit NOT NULL DEFAULT 1
);
CREATE TABLE dbo.LoaiDiSan (
    LoaiDiSanId int IDENTITY PRIMARY KEY,
    Ma varchar(32) NOT NULL UNIQUE,
    Ten nvarchar(100) NOT NULL UNIQUE,
    MoTa nvarchar(500) NULL
);
CREATE TABLE dbo.ChuDe (
    ChuDeId int IDENTITY PRIMARY KEY,
    Ma varchar(40) NOT NULL UNIQUE,
    Ten nvarchar(100) NOT NULL UNIQUE
);
CREATE TABLE dbo.SoThichNguoiDung (
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    ChuDeId int NOT NULL REFERENCES dbo.ChuDe(ChuDeId),
    MucDo tinyint NOT NULL DEFAULT 3 CHECK (MucDo BETWEEN 1 AND 5),
    PRIMARY KEY (NguoiDungId, ChuDeId)
);

-- 2. Di sản, nội dung đã duyệt và media

CREATE TABLE dbo.DiSan (
    DiSanId int IDENTITY PRIMARY KEY,
    TinhThanhId int NOT NULL REFERENCES dbo.TinhThanh(TinhThanhId),
    LoaiDiSanId int NOT NULL REFERENCES dbo.LoaiDiSan(LoaiDiSanId),
    Slug varchar(160) NOT NULL UNIQUE,
    Ten nvarchar(200) NOT NULL,
    TomTat nvarchar(1000) NOT NULL,
    NoiDung nvarchar(max) NULL,
    DiaChi nvarchar(250) NOT NULL,
    ViDo decimal(9,6) NOT NULL CHECK (ViDo BETWEEN -90 AND 90),
    KinhDo decimal(9,6) NOT NULL CHECK (KinhDo BETWEEN -180 AND 180),
    BanKinhCheckInM smallint NOT NULL DEFAULT 150 CHECK (BanKinhCheckInM BETWEEN 10 AND 3000),
    SaiSoGpsToiDaM smallint NOT NULL DEFAULT 50 CHECK (SaiSoGpsToiDaM BETWEEN 5 AND 500),
    GiaVeThamKhao decimal(12,0) NULL CHECK (GiaVeThamKhao >= 0),
    ThoiLuongPhut smallint NULL CHECK (ThoiLuongPhut BETWEEN 1 AND 1440),
    AnhBiaPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    TrangThai varchar(16) NOT NULL DEFAULT 'NHAP' CHECK (TrangThai IN ('NHAP','CONG_KHAI','AN')),
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan rowversion NOT NULL,
    CHECK (LEN(LTRIM(Ten)) > 0 AND LEN(LTRIM(TomTat)) > 0)
);
CREATE TABLE dbo.DiSanChuDe (
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    ChuDeId int NOT NULL REFERENCES dbo.ChuDe(ChuDeId),
    PRIMARY KEY (DiSanId, ChuDeId)
);
CREATE TABLE dbo.TuLieuDiSan (
    TuLieuId int IDENTITY PRIMARY KEY,
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    TieuDe nvarchar(200) NOT NULL,
    TrichYeu nvarchar(1600) NOT NULL,
    Nguon nvarchar(200) NOT NULL,
    UrlNguon nvarchar(1000) NULL,
    NguoiDuyetId int NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    DuyetLucUtc datetime2(3) NULL,
    CongKhai bit NOT NULL DEFAULT 0,
    CHECK ((NguoiDuyetId IS NULL AND DuyetLucUtc IS NULL) OR
           (NguoiDuyetId IS NOT NULL AND DuyetLucUtc IS NOT NULL)),
    CHECK (CongKhai = 0 OR DuyetLucUtc IS NOT NULL)
);
CREATE TABLE dbo.MediaDiSan (
    MediaId int IDENTITY PRIMARY KEY,
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    ChuThich nvarchar(250) NULL,
    ThuTu smallint NOT NULL CHECK (ThuTu > 0),
    UNIQUE (DiSanId, ThuTu),
    CHECK ((AnhPath IS NOT NULL AND VideoPath IS NULL) OR (AnhPath IS NULL AND VideoPath IS NOT NULL))
);
CREATE TABLE dbo.GioThamQuan (
    GioThamQuanId int IDENTITY PRIMARY KEY,
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    ThuTrongTuan tinyint NOT NULL CHECK (ThuTrongTuan BETWEEN 1 AND 7), -- 1 là thứ Hai
    GioMo time(0) NOT NULL,
    GioDong time(0) NOT NULL,
    UNIQUE (DiSanId, ThuTrongTuan, GioMo),
    CHECK (GioDong > GioMo)
);

-- 3. Đối tác, trải nghiệm, lịch và yêu cầu tham gia

CREATE TABLE dbo.DoiTac (
    DoiTacId int IDENTITY PRIMARY KEY,
    TinhThanhId int NOT NULL REFERENCES dbo.TinhThanh(TinhThanhId),
    Ten nvarchar(160) NOT NULL,
    MoTa nvarchar(1000) NULL,
    DienThoai varchar(24) NULL,
    Email nvarchar(254) NULL,
    DiaChi nvarchar(250) NULL,
    AnhDaiDienPath nvarchar(450) NULL,
    HoatDong bit NOT NULL DEFAULT 1,
    UNIQUE (TinhThanhId, Ten),
    CHECK (DienThoai IS NOT NULL OR Email IS NOT NULL)
);
CREATE TABLE dbo.ThanhVienDoiTac (
    DoiTacId int NOT NULL REFERENCES dbo.DoiTac(DoiTacId),
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    Quyen varchar(12) NOT NULL CHECK (Quyen IN ('QUAN_LY','NHAN_VIEN')),
    HoatDong bit NOT NULL DEFAULT 1,
    PRIMARY KEY (DoiTacId, NguoiDungId)
);
CREATE TABLE dbo.TraiNghiem (
    TraiNghiemId int IDENTITY PRIMARY KEY,
    DoiTacId int NOT NULL REFERENCES dbo.DoiTac(DoiTacId),
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    Slug varchar(160) NOT NULL UNIQUE,
    Ten nvarchar(180) NOT NULL,
    MoTa nvarchar(max) NOT NULL,
    DiaDiemTapTrung nvarchar(250) NULL,
    ThoiLuongPhut smallint NOT NULL CHECK (ThoiLuongPhut BETWEEN 1 AND 1440),
    GiaMoiNguoi decimal(12,0) NULL CHECK (GiaMoiNguoi >= 0),
    AnhBiaPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    TrangThai varchar(16) NOT NULL DEFAULT 'NHAP' CHECK (TrangThai IN ('NHAP','CHO_DUYET','CONG_KHAI','AN')),
    NguoiDuyetId int NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    DuyetLucUtc datetime2(3) NULL,
    PhienBan rowversion NOT NULL,
    CHECK ((NguoiDuyetId IS NULL AND DuyetLucUtc IS NULL) OR
           (NguoiDuyetId IS NOT NULL AND DuyetLucUtc IS NOT NULL)),
    CHECK (TrangThai <> 'CONG_KHAI' OR DuyetLucUtc IS NOT NULL)
);
CREATE TABLE dbo.MediaTraiNghiem (
    MediaId int IDENTITY PRIMARY KEY,
    TraiNghiemId int NOT NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    ChuThich nvarchar(250) NULL,
    ThuTu smallint NOT NULL CHECK (ThuTu > 0),
    UNIQUE (TraiNghiemId, ThuTu),
    CHECK ((AnhPath IS NOT NULL AND VideoPath IS NULL) OR (AnhPath IS NULL AND VideoPath IS NOT NULL))
);
CREATE TABLE dbo.LichTraiNghiem (
    LichTraiNghiemId int IDENTITY PRIMARY KEY,
    TraiNghiemId int NOT NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    BatDauUtc datetime2(3) NOT NULL,
    KetThucUtc datetime2(3) NOT NULL,
    SucChua smallint NOT NULL CHECK (SucChua BETWEEN 1 AND 500),
    TrangThai varchar(12) NOT NULL DEFAULT 'MO' CHECK (TrangThai IN ('MO','DONG','HUY')),
    UNIQUE (TraiNghiemId, BatDauUtc),
    UNIQUE (LichTraiNghiemId, TraiNghiemId),
    CHECK (KetThucUtc > BatDauUtc)
);
CREATE TABLE dbo.YeuCauThamGia (
    YeuCauId bigint IDENTITY PRIMARY KEY,
    MaYeuCau uniqueidentifier NOT NULL DEFAULT NEWID(),
    LichTraiNghiemId int NOT NULL,
    TraiNghiemId int NOT NULL,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    SoNguoi smallint NOT NULL CHECK (SoNguoi BETWEEN 1 AND 500),
    DonGiaDuKien decimal(12,0) NULL CHECK (DonGiaDuKien >= 0),
    GhiChu nvarchar(500) NULL,
    TrangThai varchar(16) NOT NULL DEFAULT 'CHO_XAC_NHAN'
        CHECK (TrangThai IN ('CHO_XAC_NHAN','DA_XAC_NHAN','TU_CHOI','DA_HUY','DA_THAM_GIA')),
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    XuLyLucUtc datetime2(3) NULL,
    NguoiXuLyId int NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    UNIQUE (NguoiDungId, MaYeuCau),
    UNIQUE (YeuCauId, NguoiDungId, TraiNghiemId, TrangThai),
    FOREIGN KEY (LichTraiNghiemId, TraiNghiemId) REFERENCES dbo.LichTraiNghiem(LichTraiNghiemId, TraiNghiemId),
    CHECK ((TrangThai = 'CHO_XAC_NHAN' AND XuLyLucUtc IS NULL AND NguoiXuLyId IS NULL) OR
           (TrangThai <> 'CHO_XAC_NHAN' AND XuLyLucUtc IS NOT NULL AND NguoiXuLyId IS NOT NULL)),
    CHECK (XuLyLucUtc IS NULL OR XuLyLucUtc >= TaoLucUtc)
);
CREATE UNIQUE INDEX UX_YeuCau_HieuLuc ON dbo.YeuCauThamGia(LichTraiNghiemId, NguoiDungId)
    WHERE TrangThai IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DA_THAM_GIA');
CREATE INDEX IX_YeuCau_SucChua ON dbo.YeuCauThamGia(LichTraiNghiemId, TrangThai) INCLUDE (SoNguoi);

CREATE TABLE dbo.DiSanYeuThich (
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    LuuLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (NguoiDungId, DiSanId)
);

-- 4. Hội thoại Gemini và nguồn trích dẫn

CREATE TABLE dbo.HoiThoaiAI (
    HoiThoaiId bigint IDENTITY PRIMARY KEY,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    TieuDe nvarchar(160) NOT NULL,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DaLuuTru bit NOT NULL DEFAULT 0,
    UNIQUE (HoiThoaiId, NguoiDungId)
);
CREATE TABLE dbo.TinNhanAI (
    TinNhanAIId bigint IDENTITY PRIMARY KEY,
    HoiThoaiId bigint NOT NULL,
    NguoiDungId int NOT NULL,
    Vai varchar(12) NOT NULL CHECK (Vai IN ('USER','ASSISTANT')),
    NoiDung nvarchar(max) NOT NULL,
    NhaCungCap varchar(16) NULL CHECK (NhaCungCap = 'GEMINI'),
    MaMoHinh varchar(100) NULL,
    TokenVao int NULL CHECK (TokenVao >= 0),
    TokenRa int NULL CHECK (TokenRa >= 0),
    DeXuatHanhTrinhJson nvarchar(max) NULL CHECK (ISJSON(DeXuatHanhTrinhJson) = 1),
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (HoiThoaiId, NguoiDungId) REFERENCES dbo.HoiThoaiAI(HoiThoaiId, NguoiDungId),
    UNIQUE (TinNhanAIId, NguoiDungId, Vai),
    CHECK (LEN(LTRIM(NoiDung)) > 0),
    CHECK ((Vai = 'USER' AND NhaCungCap IS NULL AND MaMoHinh IS NULL AND TokenVao IS NULL
                            AND TokenRa IS NULL AND DeXuatHanhTrinhJson IS NULL)
        OR (Vai = 'ASSISTANT' AND NhaCungCap IS NOT NULL AND MaMoHinh IS NOT NULL
                              AND LEN(LTRIM(MaMoHinh)) > 0))
);
CREATE TABLE dbo.NguonTraLoiAI (
    TinNhanAIId bigint NOT NULL,
    NguoiDungId int NOT NULL,
    Vai varchar(12) NOT NULL DEFAULT 'ASSISTANT' CHECK (Vai = 'ASSISTANT'),
    ThuTu smallint NOT NULL CHECK (ThuTu > 0),
    DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
    TraiNghiemId int NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    TuLieuId int NULL REFERENCES dbo.TuLieuDiSan(TuLieuId),
    TieuDeKhiTrich nvarchar(220) NOT NULL,
    UrlKhiTrich nvarchar(1000) NULL,
    PRIMARY KEY (TinNhanAIId, ThuTu),
    FOREIGN KEY (TinNhanAIId, NguoiDungId, Vai) REFERENCES dbo.TinNhanAI(TinNhanAIId, NguoiDungId, Vai),
    CHECK (IIF(DiSanId IS NULL,0,1) + IIF(TraiNghiemId IS NULL,0,1) + IIF(TuLieuId IS NULL,0,1) = 1)
);

-- 5. Hành trình, GPS và sổ tay

CREATE TABLE dbo.HanhTrinh (
    HanhTrinhId bigint IDENTITY PRIMARY KEY,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    TieuDe nvarchar(160) NOT NULL,
    MoTa nvarchar(1000) NULL,
    NgayBatDau date NOT NULL,
    NgayKetThuc date NOT NULL,
    TrangThai varchar(16) NOT NULL DEFAULT 'LEN_KE_HOACH'
        CHECK (TrangThai IN ('LEN_KE_HOACH','DANG_DI','HOAN_THANH','DA_HUY')),
    AnhBiaPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    TinNhanAIId bigint NULL,
    VaiTinNhanAI varchar(12) NOT NULL DEFAULT 'ASSISTANT' CHECK (VaiTinNhanAI = 'ASSISTANT'),
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan rowversion NOT NULL,
    UNIQUE (HanhTrinhId, NguoiDungId),
    FOREIGN KEY (TinNhanAIId, NguoiDungId, VaiTinNhanAI) REFERENCES dbo.TinNhanAI(TinNhanAIId, NguoiDungId, Vai),
    CHECK (DATEDIFF(DAY, NgayBatDau, NgayKetThuc) BETWEEN 0 AND 89 AND LEN(LTRIM(TieuDe)) > 0)
);
CREATE TABLE dbo.DiemHanhTrinh (
    DiemHanhTrinhId bigint IDENTITY PRIMARY KEY,
    HanhTrinhId bigint NOT NULL REFERENCES dbo.HanhTrinh(HanhTrinhId),
    NgayDuKien date NOT NULL,
    ThuTu smallint NOT NULL CHECK (ThuTu BETWEEN 1 AND 100),
    DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
    TraiNghiemId int NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    GioDuKien time(0) NULL,
    GhiChu nvarchar(500) NULL,
    UNIQUE (HanhTrinhId, NgayDuKien, ThuTu),
    CHECK ((DiSanId IS NULL AND TraiNghiemId IS NOT NULL) OR (DiSanId IS NOT NULL AND TraiNghiemId IS NULL))
);
CREATE TABLE dbo.CheckInDiSan (
    CheckInId bigint IDENTITY PRIMARY KEY,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
    MaYeuCau uniqueidentifier NOT NULL DEFAULT NEWID(),
    ThoiGianUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    NgayDiaPhuong AS CONVERT(date, DATEADD(MINUTE, 420, ThoiGianUtc)) PERSISTED,
    ViDo decimal(9,6) NOT NULL CHECK (ViDo BETWEEN -90 AND 90),
    KinhDo decimal(9,6) NOT NULL CHECK (KinhDo BETWEEN -180 AND 180),
    SaiSoGpsM decimal(8,2) NOT NULL,
    TamViDo decimal(9,6) NOT NULL CHECK (TamViDo BETWEEN -90 AND 90),
    TamKinhDo decimal(9,6) NOT NULL CHECK (TamKinhDo BETWEEN -180 AND 180),
    BanKinhApDungM smallint NOT NULL CHECK (BanKinhApDungM BETWEEN 10 AND 3000),
    SaiSoToiDaApDungM smallint NOT NULL CHECK (SaiSoToiDaApDungM BETWEEN 5 AND 500),
    KhoangCachM decimal(12,3) NOT NULL,
    GhiChu nvarchar(500) NULL,
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    UNIQUE (NguoiDungId, MaYeuCau),
    UNIQUE (NguoiDungId, DiSanId, NgayDiaPhuong),
    UNIQUE (CheckInId, NguoiDungId),
    UNIQUE (CheckInId, NguoiDungId, DiSanId),
    CHECK (SaiSoGpsM > 0 AND SaiSoGpsM <= SaiSoToiDaApDungM),
    CHECK (KhoangCachM >= 0 AND KhoangCachM <= BanKinhApDungM)
);
CREATE TABLE dbo.HanhTrinhCheckIn (
    HanhTrinhId bigint NOT NULL,
    CheckInId bigint NOT NULL UNIQUE,
    NguoiDungId int NOT NULL,
    PRIMARY KEY (HanhTrinhId, CheckInId),
    FOREIGN KEY (HanhTrinhId, NguoiDungId) REFERENCES dbo.HanhTrinh(HanhTrinhId, NguoiDungId),
    FOREIGN KEY (CheckInId, NguoiDungId) REFERENCES dbo.CheckInDiSan(CheckInId, NguoiDungId)
);
CREATE TABLE dbo.NhatKyHanhTrinh (
    NhatKyId bigint IDENTITY PRIMARY KEY,
    HanhTrinhId bigint NOT NULL REFERENCES dbo.HanhTrinh(HanhTrinhId),
    CheckInId bigint NULL,
    TieuDe nvarchar(160) NOT NULL,
    NoiDung nvarchar(max) NOT NULL,
    NgayGhi date NOT NULL,
    AnhBiaPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    MaChiaSe uniqueidentifier NOT NULL DEFAULT NEWID() UNIQUE,
    CongKhai bit NOT NULL DEFAULT 0,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan rowversion NOT NULL,
    FOREIGN KEY (HanhTrinhId, CheckInId) REFERENCES dbo.HanhTrinhCheckIn(HanhTrinhId, CheckInId),
    CHECK (LEN(LTRIM(TieuDe)) > 0 AND LEN(LTRIM(NoiDung)) > 0)
);
CREATE TABLE dbo.MediaNhatKy (
    MediaId bigint IDENTITY PRIMARY KEY,
    NhatKyId bigint NOT NULL REFERENCES dbo.NhatKyHanhTrinh(NhatKyId),
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    ChuThich nvarchar(250) NULL,
    ThuTu smallint NOT NULL CHECK (ThuTu > 0),
    UNIQUE (NhatKyId, ThuTu),
    CHECK ((AnhPath IS NOT NULL AND VideoPath IS NULL) OR (AnhPath IS NULL AND VideoPath IS NOT NULL))
);

-- 6. Nhiệm vụ, quiz, huy hiệu, đánh giá

CREATE TABLE dbo.HuyHieu (
    HuyHieuId int IDENTITY PRIMARY KEY,
    Ma varchar(40) NOT NULL UNIQUE,
    Ten nvarchar(120) NOT NULL,
    MoTa nvarchar(500) NULL,
    AnhPath nvarchar(450) NULL
);
CREATE TABLE dbo.NhiemVu (
    NhiemVuId int IDENTITY PRIMARY KEY,
    HuyHieuId int NULL REFERENCES dbo.HuyHieu(HuyHieuId),
    DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
    LoaiDiSanId int NULL REFERENCES dbo.LoaiDiSan(LoaiDiSanId),
    Loai varchar(16) NOT NULL CHECK (Loai IN ('QUIZ','DEM_DI_SAN')),
    Ten nvarchar(160) NOT NULL,
    MoTa nvarchar(800) NOT NULL,
    SoDiSanCanDat smallint NULL CHECK (SoDiSanCanDat BETWEEN 1 AND 500),
    BatDauUtc datetime2(3) NOT NULL,
    KetThucUtc datetime2(3) NULL,
    CongKhai bit NOT NULL DEFAULT 0,
    CHECK (KetThucUtc IS NULL OR KetThucUtc > BatDauUtc),
    CHECK ((Loai = 'QUIZ' AND SoDiSanCanDat IS NULL AND LoaiDiSanId IS NULL) OR
           (Loai = 'DEM_DI_SAN' AND SoDiSanCanDat IS NOT NULL AND DiSanId IS NULL))
);
CREATE TABLE dbo.CauHoiNhiemVu (
    CauHoiId int IDENTITY PRIMARY KEY,
    NhiemVuId int NOT NULL REFERENCES dbo.NhiemVu(NhiemVuId),
    ThuTu smallint NOT NULL CHECK (ThuTu > 0),
    NoiDung nvarchar(500) NOT NULL,
    GiaiThich nvarchar(800) NULL,
    UNIQUE (NhiemVuId, ThuTu)
);
CREATE TABLE dbo.LuaChonCauHoi (
    LuaChonId int IDENTITY PRIMARY KEY,
    CauHoiId int NOT NULL REFERENCES dbo.CauHoiNhiemVu(CauHoiId),
    ThuTu tinyint NOT NULL CHECK (ThuTu BETWEEN 1 AND 10),
    NoiDung nvarchar(400) NOT NULL,
    Dung bit NOT NULL DEFAULT 0,
    UNIQUE (CauHoiId, ThuTu),
    UNIQUE (CauHoiId, LuaChonId)
);
CREATE UNIQUE INDEX UX_LuaChon_DapAnDung ON dbo.LuaChonCauHoi(CauHoiId) WHERE Dung = 1;
CREATE TABLE dbo.KetQuaTraLoi (
    KetQuaId bigint IDENTITY PRIMARY KEY,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    CauHoiId int NOT NULL,
    LuaChonId int NOT NULL,
    DungKhiNop bit NOT NULL,
    NopLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (CauHoiId, LuaChonId) REFERENCES dbo.LuaChonCauHoi(CauHoiId, LuaChonId)
);
CREATE TABLE dbo.TienDoNhiemVu (
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    NhiemVuId int NOT NULL REFERENCES dbo.NhiemVu(NhiemVuId),
    SoDaDat int NOT NULL CHECK (SoDaDat >= 0),
    MucTieu int NOT NULL CHECK (MucTieu > 0),
    CapNhatLucUtc datetime2(3) NOT NULL,
    HoanThanhLucUtc datetime2(3) NULL,
    PRIMARY KEY (NguoiDungId, NhiemVuId),
    CHECK ((SoDaDat < MucTieu AND HoanThanhLucUtc IS NULL) OR
           (SoDaDat >= MucTieu AND HoanThanhLucUtc IS NOT NULL))
);
CREATE TABLE dbo.HuyHieuNguoiDung (
    NguoiDungId int NOT NULL,
    HuyHieuId int NOT NULL REFERENCES dbo.HuyHieu(HuyHieuId),
    NhiemVuId int NOT NULL,
    NhanLucUtc datetime2(3) NOT NULL,
    PRIMARY KEY (NguoiDungId, HuyHieuId),
    FOREIGN KEY (NguoiDungId, NhiemVuId) REFERENCES dbo.TienDoNhiemVu(NguoiDungId, NhiemVuId)
);
CREATE TABLE dbo.DanhGiaDiSan (
    NguoiDungId int NOT NULL,
    DiSanId int NOT NULL,
    CheckInId bigint NOT NULL,
    SoSao tinyint NOT NULL CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung nvarchar(1000) NULL,
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    CongKhai bit NOT NULL DEFAULT 1,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (NguoiDungId, DiSanId),
    FOREIGN KEY (CheckInId, NguoiDungId, DiSanId) REFERENCES dbo.CheckInDiSan(CheckInId, NguoiDungId, DiSanId)
);
CREATE TABLE dbo.DanhGiaTraiNghiem (
    NguoiDungId int NOT NULL,
    TraiNghiemId int NOT NULL,
    YeuCauId bigint NOT NULL,
    TrangThaiYeuCau varchar(16) NOT NULL DEFAULT 'DA_THAM_GIA' CHECK (TrangThaiYeuCau = 'DA_THAM_GIA'),
    SoSao tinyint NOT NULL CHECK (SoSao BETWEEN 1 AND 5),
    NoiDung nvarchar(1000) NULL,
    AnhPath nvarchar(450) NULL,
    VideoPath nvarchar(450) NULL,
    CongKhai bit NOT NULL DEFAULT 1,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (NguoiDungId, TraiNghiemId),
    FOREIGN KEY (YeuCauId, NguoiDungId, TraiNghiemId, TrangThaiYeuCau)
        REFERENCES dbo.YeuCauThamGia(YeuCauId, NguoiDungId, TraiNghiemId, TrangThai)
);

-- 7. Sự kiện và đầu ra cá nhân hóa/ML

CREATE TABLE dbo.MoHinhGoiY (
    MoHinhId int IDENTITY PRIMARY KEY,
    Ma varchar(48) NOT NULL,
    PhienBan varchar(32) NOT NULL,
    ThuatToan nvarchar(120) NOT NULL,
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    HuanLuyenLucUtc datetime2(3) NULL,
    ChiSoJson nvarchar(max) NULL CHECK (ISJSON(ChiSoJson) = 1),
    TepMoHinhPath nvarchar(450) NULL,
    HoatDong bit NOT NULL DEFAULT 1,
    UNIQUE (Ma, PhienBan)
);
CREATE TABLE dbo.LuotGoiY (
    LuotGoiYId bigint IDENTITY PRIMARY KEY,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    MoHinhId int NOT NULL REFERENCES dbo.MoHinhGoiY(MoHinhId),
    ViTriHienThi varchar(24) NOT NULL CHECK (ViTriHienThi IN ('TRANG_CHU','CHI_TIET','HANH_TRINH')),
    TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (LuotGoiYId, NguoiDungId)
);
CREATE TABLE dbo.ChiTietGoiY (
    LuotGoiYId bigint NOT NULL REFERENCES dbo.LuotGoiY(LuotGoiYId),
    ThuHang smallint NOT NULL CHECK (ThuHang > 0),
    DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
    TraiNghiemId int NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    Diem decimal(9,6) NOT NULL CHECK (Diem BETWEEN 0 AND 1),
    LyDo nvarchar(350) NULL,
    PRIMARY KEY (LuotGoiYId, ThuHang),
    CHECK ((DiSanId IS NULL AND TraiNghiemId IS NOT NULL) OR (DiSanId IS NOT NULL AND TraiNghiemId IS NULL))
);
CREATE UNIQUE INDEX UX_ChiTietGoiY_DiSan ON dbo.ChiTietGoiY(LuotGoiYId, DiSanId) WHERE DiSanId IS NOT NULL;
CREATE UNIQUE INDEX UX_ChiTietGoiY_TraiNghiem ON dbo.ChiTietGoiY(LuotGoiYId, TraiNghiemId) WHERE TraiNghiemId IS NOT NULL;
CREATE TABLE dbo.SuKienTuongTac (
    SuKienId bigint IDENTITY PRIMARY KEY,
    MaSuKien uniqueidentifier NOT NULL,
    NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
    Loai varchar(20) NOT NULL CHECK (Loai IN ('XEM','NHAN_GOI_Y','LUU','CHECK_IN','DANH_GIA','AN_GOI_Y')),
    DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
    TraiNghiemId int NULL REFERENCES dbo.TraiNghiem(TraiNghiemId),
    LuotGoiYId bigint NULL,
    ThuHang smallint NULL,
    SoSao tinyint NULL CHECK (SoSao BETWEEN 1 AND 5),
    ThoiGianUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (NguoiDungId, MaSuKien),
    FOREIGN KEY (LuotGoiYId, NguoiDungId) REFERENCES dbo.LuotGoiY(LuotGoiYId, NguoiDungId),
    FOREIGN KEY (LuotGoiYId, ThuHang) REFERENCES dbo.ChiTietGoiY(LuotGoiYId, ThuHang),
    CHECK ((DiSanId IS NULL AND TraiNghiemId IS NOT NULL) OR (DiSanId IS NOT NULL AND TraiNghiemId IS NULL)),
    CHECK ((Loai IN ('NHAN_GOI_Y','AN_GOI_Y') AND LuotGoiYId IS NOT NULL AND ThuHang IS NOT NULL)
        OR (Loai NOT IN ('NHAN_GOI_Y','AN_GOI_Y') AND LuotGoiYId IS NULL AND ThuHang IS NULL)),
    CHECK ((Loai = 'DANH_GIA' AND SoSao IS NOT NULL) OR (Loai <> 'DANH_GIA' AND SoSao IS NULL)),
    CHECK (Loai NOT IN ('CHECK_IN','LUU') OR DiSanId IS NOT NULL)
);

CREATE INDEX IX_DiSan_KhamPha ON dbo.DiSan(TrangThai, TinhThanhId, LoaiDiSanId) INCLUDE (Ten, ViDo, KinhDo);
CREATE INDEX IX_DiSanChuDe_ChuDe ON dbo.DiSanChuDe(ChuDeId, DiSanId);
CREATE INDEX IX_TuLieu_DiSan ON dbo.TuLieuDiSan(DiSanId, CongKhai);
CREATE INDEX IX_TraiNghiem_DiSan ON dbo.TraiNghiem(DiSanId, TrangThai);
CREATE INDEX IX_TinNhanAI_HoiThoai ON dbo.TinNhanAI(HoiThoaiId, TinNhanAIId);
CREATE INDEX IX_HanhTrinh_NguoiDung ON dbo.HanhTrinh(NguoiDungId, NgayBatDau);
CREATE INDEX IX_CheckIn_TienDo ON dbo.CheckInDiSan(NguoiDungId, ThoiGianUtc) INCLUDE (DiSanId);
CREATE INDEX IX_KetQua_TienDo ON dbo.KetQuaTraLoi(NguoiDungId, CauHoiId, DungKhiNop, NopLucUtc);
CREATE INDEX IX_SuKien_NguoiDung ON dbo.SuKienTuongTac(NguoiDungId, ThoiGianUtc);
GO
-- 8. Hàm, view cho bản đồ, Gemini và ML

CREATE OR ALTER FUNCTION dbo.fn_KhoangCachM(
    @ViDo1 decimal(9,6), @KinhDo1 decimal(9,6), @ViDo2 decimal(9,6), @KinhDo2 decimal(9,6)
)
RETURNS decimal(12,3)
AS
BEGIN
    IF @ViDo1 IS NULL OR @ViDo2 IS NULL OR @KinhDo1 IS NULL OR @KinhDo2 IS NULL
        OR @ViDo1 NOT BETWEEN -90 AND 90 OR @ViDo2 NOT BETWEEN -90 AND 90
        OR @KinhDo1 NOT BETWEEN -180 AND 180 OR @KinhDo2 NOT BETWEEN -180 AND 180
        RETURN NULL;
    DECLARE @Diem1 geography = geography::Point(@ViDo1, @KinhDo1, 4326);
    DECLARE @Diem2 geography = geography::Point(@ViDo2, @KinhDo2, 4326);
    RETURN CONVERT(decimal(12,3), @Diem1.STDistance(@Diem2));
END;
GO
CREATE OR ALTER VIEW dbo.vw_DiSanCongKhai AS
SELECT d.DiSanId, d.Slug, d.Ten, d.TomTat, d.NoiDung, d.DiaChi, d.TinhThanhId,
       t.Ten AS TinhThanh, d.LoaiDiSanId, l.Ten AS LoaiDiSan, d.ViDo, d.KinhDo,
       d.BanKinhCheckInM, d.SaiSoGpsToiDaM, d.GiaVeThamKhao, d.ThoiLuongPhut, d.AnhBiaPath, d.VideoPath
FROM dbo.DiSan d
JOIN dbo.TinhThanh t ON t.TinhThanhId = d.TinhThanhId AND t.HoatDong = 1
JOIN dbo.LoaiDiSan l ON l.LoaiDiSanId = d.LoaiDiSanId
WHERE d.TrangThai = 'CONG_KHAI';
GO
CREATE OR ALTER VIEW dbo.vw_TraiNghiemCongKhai AS
SELECT e.TraiNghiemId, e.DiSanId, e.DoiTacId, e.Slug, e.Ten, e.MoTa, e.DiaDiemTapTrung,
       e.ThoiLuongPhut, e.GiaMoiNguoi, e.AnhBiaPath, e.VideoPath, p.Ten AS TenDoiTac,
       p.DienThoai, p.Email, d.TinhThanhId
FROM dbo.TraiNghiem e
JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = e.DiSanId
JOIN dbo.DoiTac p ON p.DoiTacId = e.DoiTacId AND p.HoatDong = 1
WHERE e.TrangThai = 'CONG_KHAI';
GO
CREATE OR ALTER FUNCTION dbo.fn_DiSanGan(
    @ViDo decimal(9,6), @KinhDo decimal(9,6), @BanKinhM int
)
RETURNS TABLE AS RETURN
(
    SELECT d.DiSanId, d.Ten, d.Slug, d.TinhThanh, d.ViDo, d.KinhDo, d.AnhBiaPath, x.KhoangCachM
    FROM dbo.vw_DiSanCongKhai d
    CROSS APPLY (VALUES(dbo.fn_KhoangCachM(@ViDo,@KinhDo,d.ViDo,d.KinhDo))) x(KhoangCachM)
    WHERE @BanKinhM BETWEEN 1 AND 200000 AND x.KhoangCachM <= @BanKinhM
);
GO
CREATE OR ALTER VIEW dbo.vw_LichTraiNghiem AS
SELECT l.LichTraiNghiemId, l.TraiNghiemId, l.BatDauUtc, l.KetThucUtc, l.TrangThai, l.SucChua,
       l.SucChua - COALESCE(x.SoDaGiuCho,0) AS SoChoCon
FROM dbo.LichTraiNghiem l
JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = l.TraiNghiemId
OUTER APPLY (
    SELECT SUM(CONVERT(int, y.SoNguoi)) AS SoDaGiuCho
    FROM dbo.YeuCauThamGia y
    WHERE y.LichTraiNghiemId = l.LichTraiNghiemId AND y.TrangThai IN ('DA_XAC_NHAN','DA_THAM_GIA')
) x;
GO
CREATE OR ALTER VIEW dbo.vw_NguCanhGemini AS
SELECT CONVERT(varchar(16),'DI_SAN') AS LoaiNguon, d.DiSanId AS NguonId, d.DiSanId, d.TinhThanhId,
       CONVERT(nvarchar(220),d.Ten) AS TieuDe, CONVERT(nvarchar(max),d.TomTat) AS NoiDung,
       CONVERT(nvarchar(1000),CONCAT(N'/di-san/',d.Slug)) AS UrlNguon
FROM dbo.vw_DiSanCongKhai d
UNION ALL
SELECT 'TRAI_NGHIEM', e.TraiNghiemId, e.DiSanId, e.TinhThanhId, e.Ten, e.MoTa, CONCAT(N'/trai-nghiem/',e.Slug)
FROM dbo.vw_TraiNghiemCongKhai e
UNION ALL
SELECT 'TU_LIEU', s.TuLieuId, d.DiSanId, d.TinhThanhId, s.TieuDe, s.TrichYeu, s.UrlNguon
FROM dbo.TuLieuDiSan s
JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = s.DiSanId
WHERE s.CongKhai = 1;
GO
CREATE OR ALTER VIEW dbo.vw_DuLieuHuanLuyen AS
-- NULL = quan sát, 1 = tín hiệu tích cực, 0 = phản hồi tiêu cực có chủ ý.
-- Đây là dữ liệu đầu vào. Tách tập theo thời gian và khử lặp trong pipeline huấn luyện.
WITH TinHieu AS (
    SELECT NguoiDungId, Loai, DiSanId, TraiNghiemId, LuotGoiYId, SoSao, ThoiGianUtc
    FROM dbo.SuKienTuongTac WHERE Loai NOT IN ('LUU','DANH_GIA')
    UNION ALL
    SELECT NguoiDungId,'LUU',DiSanId,NULL,NULL,NULL,LuuLucUtc FROM dbo.DiSanYeuThich
    UNION ALL
    SELECT NguoiDungId,'DANH_GIA',DiSanId,NULL,NULL,SoSao,TaoLucUtc FROM dbo.DanhGiaDiSan
    UNION ALL
    SELECT NguoiDungId,'DANH_GIA',NULL,TraiNghiemId,NULL,SoSao,TaoLucUtc FROM dbo.DanhGiaTraiNghiem
)
SELECT s.NguoiDungId, s.Loai, s.DiSanId, s.TraiNghiemId, s.LuotGoiYId, s.SoSao, s.ThoiGianUtc,
       CONVERT(tinyint,CASE WHEN s.Loai = 'AN_GOI_Y' THEN 0 WHEN s.Loai = 'XEM' THEN NULL
           WHEN s.Loai = 'DANH_GIA' THEN CASE WHEN s.SoSao >= 4 THEN 1 WHEN s.SoSao <= 2 THEN 0 END
           ELSE 1 END) AS Nhan
FROM TinHieu s
JOIN dbo.NguoiDung u ON u.NguoiDungId = s.NguoiDungId AND u.ChoPhepCaNhanHoa = 1 AND u.HoatDong = 1;
GO
CREATE OR ALTER VIEW dbo.vw_NhatKyCongKhai AS
SELECT n.MaChiaSe, n.TieuDe, n.NoiDung, n.NgayGhi, n.AnhBiaPath, n.VideoPath, u.HoTen AS TenTacGia
FROM dbo.NhatKyHanhTrinh n
JOIN dbo.HanhTrinh h ON h.HanhTrinhId = n.HanhTrinhId
JOIN dbo.NguoiDung u ON u.NguoiDungId = h.NguoiDungId AND u.HoatDong = 1
WHERE n.CongKhai = 1;
GO

-- 9. Trigger theo tập bản ghi, áp dụng cả khi ghi qua EF Core

CREATE OR ALTER TRIGGER dbo.tr_CheckIn_KiemTraGPS ON dbo.CheckInDiSan
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51001, N'Bản ghi GPS đã tạo không được sửa. Có thể ghi bổ sung trong sổ tay.', 1;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = i.DiSanId
        JOIN dbo.NguoiDung u ON u.NguoiDungId = i.NguoiDungId
        WHERE d.DiSanId IS NULL OR u.HoatDong = 0
           OR i.TamViDo <> d.ViDo OR i.TamKinhDo <> d.KinhDo
           OR i.BanKinhApDungM <> d.BanKinhCheckInM OR i.SaiSoToiDaApDungM <> d.SaiSoGpsToiDaM
           OR i.KhoangCachM <> dbo.fn_KhoangCachM(i.ViDo,i.KinhDo,d.ViDo,d.KinhDo)
           OR i.ThoiGianUtc > CONVERT(datetime2(3),SYSUTCDATETIME())
    )
        THROW 51002, N'Tọa độ, khoảng cách, thời gian hoặc cấu hình check-in không hợp lệ.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_DiemHanhTrinh_Ngay ON dbo.DiemHanhTrinh
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.HanhTrinh h WITH (UPDLOCK, HOLDLOCK) ON h.HanhTrinhId = i.HanhTrinhId
        WHERE i.NgayDuKien NOT BETWEEN h.NgayBatDau AND h.NgayKetThuc
    )
        THROW 51003, N'Ngày dự kiến của điểm phải nằm trong hành trình.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_HanhTrinh_Ngay ON dbo.HanhTrinh
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.DiemHanhTrinh p WITH (UPDLOCK, HOLDLOCK) ON p.HanhTrinhId = i.HanhTrinhId
        WHERE p.NgayDuKien NOT BETWEEN i.NgayBatDau AND i.NgayKetThuc
    )
        THROW 51004, N'Khoảng ngày mới loại bỏ một hoặc nhiều điểm đã lên kế hoạch.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_GioThamQuan_TrungGio ON dbo.GioThamQuan
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.GioThamQuan x WITH (UPDLOCK, HOLDLOCK) ON x.DiSanId = i.DiSanId AND x.ThuTrongTuan = i.ThuTrongTuan
        WHERE x.GioThamQuanId <> i.GioThamQuanId AND x.GioMo < i.GioDong AND i.GioMo < x.GioDong
    )
        THROW 51005, N'Các khung giờ tham quan trong cùng ngày không được chồng nhau.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_YeuCau_SucChuaVaTrangThai ON dbo.YeuCauThamGia
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted d ON d.YeuCauId = i.YeuCauId
        WHERE i.NguoiDungId <> d.NguoiDungId OR i.LichTraiNghiemId <> d.LichTraiNghiemId
           OR i.SoNguoi <> d.SoNguoi OR i.MaYeuCau <> d.MaYeuCau
           OR (i.TrangThai <> d.TrangThai AND NOT (
                (d.TrangThai = 'CHO_XAC_NHAN' AND i.TrangThai IN ('DA_XAC_NHAN','TU_CHOI','DA_HUY')) OR
                (d.TrangThai = 'DA_XAC_NHAN' AND i.TrangThai IN ('DA_THAM_GIA','DA_HUY'))))
    )
        THROW 51006, N'Chuyển trạng thái hoặc sửa thông tin yêu cầu không hợp lệ.', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.LichTraiNghiem l WITH (UPDLOCK, HOLDLOCK) ON l.LichTraiNghiemId = i.LichTraiNghiemId
        LEFT JOIN deleted d ON d.YeuCauId = i.YeuCauId
        WHERE (i.TrangThai = 'DA_THAM_GIA' AND l.KetThucUtc > SYSUTCDATETIME())
           OR (i.TrangThai = 'DA_XAC_NHAN' AND (d.YeuCauId IS NULL OR d.TrangThai <> i.TrangThai)
               AND (l.TrangThai <> 'MO' OR l.BatDauUtc <= SYSUTCDATETIME()))
           OR (i.TrangThai = 'CHO_XAC_NHAN' AND d.YeuCauId IS NULL
               AND (l.TrangThai <> 'MO' OR l.BatDauUtc <= SYSUTCDATETIME() OR i.SoNguoi > l.SucChua))
    )
        THROW 51007, N'Lịch đã đóng, sai thời điểm xử lý hoặc nhóm vượt sức chứa.', 1;

    IF EXISTS (
        SELECT 1
        FROM (SELECT DISTINCT LichTraiNghiemId FROM inserted) i
        JOIN dbo.LichTraiNghiem l WITH (UPDLOCK, HOLDLOCK) ON l.LichTraiNghiemId = i.LichTraiNghiemId
        CROSS APPLY (
            SELECT SUM(CONVERT(int,y.SoNguoi)) AS DaGiu
            FROM dbo.YeuCauThamGia y WITH (UPDLOCK, HOLDLOCK)
            WHERE y.LichTraiNghiemId = i.LichTraiNghiemId AND y.TrangThai IN ('DA_XAC_NHAN','DA_THAM_GIA')
        ) x
        WHERE x.DaGiu > l.SucChua
    )
        THROW 51008, N'Lịch không còn đủ chỗ để xác nhận yêu cầu.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_LichTraiNghiem_BaoVeYeuCau ON dbo.LichTraiNghiem
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted d ON d.LichTraiNghiemId = i.LichTraiNghiemId
        CROSS APPLY (
            SELECT SUM(CASE WHEN y.TrangThai IN ('DA_XAC_NHAN','DA_THAM_GIA') THEN CONVERT(int,y.SoNguoi) ELSE 0 END) AS DaGiu,
                   MAX(CASE WHEN y.TrangThai IN ('CHO_XAC_NHAN','DA_XAC_NHAN') THEN 1 ELSE 0 END) AS CoYeuCauDangMo
            FROM dbo.YeuCauThamGia y WITH (UPDLOCK, HOLDLOCK)
            WHERE y.LichTraiNghiemId = i.LichTraiNghiemId
        ) x
        WHERE x.DaGiu > i.SucChua
           OR (x.CoYeuCauDangMo = 1 AND i.TrangThai = 'HUY')
           OR ((x.CoYeuCauDangMo = 1 OR x.DaGiu > 0) AND
              (i.BatDauUtc <> d.BatDauUtc OR i.KetThucUtc <> d.KetThucUtc))
    )
        THROW 51009, N'Không thể đổi lịch hoặc sức chứa khi còn yêu cầu liên quan.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_SuKien_XacNhanGoiY ON dbo.SuKienTuongTac
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN dbo.NguoiDung u ON u.NguoiDungId = i.NguoiDungId
        LEFT JOIN dbo.ChiTietGoiY c ON c.LuotGoiYId = i.LuotGoiYId AND c.ThuHang = i.ThuHang
        WHERE u.ChoPhepCaNhanHoa = 0 OR u.HoatDong = 0
           OR (i.LuotGoiYId IS NOT NULL AND (ISNULL(c.DiSanId,0) <> ISNULL(i.DiSanId,0)
                                         OR ISNULL(c.TraiNghiemId,0) <> ISNULL(i.TraiNghiemId,0)))
    )
        THROW 51010, N'Người dùng chưa bật cá nhân hóa hoặc sự kiện sai đối tượng gợi ý.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_CauHoi_ChiChoQuiz ON dbo.CauHoiNhiemVu
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.NhiemVu n WITH (UPDLOCK, HOLDLOCK) ON n.NhiemVuId = i.NhiemVuId WHERE n.Loai <> 'QUIZ')
        THROW 51011, N'Chỉ nhiệm vụ QUIZ được có câu hỏi.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.tr_NhiemVu_BaoVeCauHoi ON dbo.NhiemVu
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.CauHoiNhiemVu c WITH (UPDLOCK, HOLDLOCK) ON c.NhiemVuId = i.NhiemVuId WHERE i.Loai <> 'QUIZ')
        THROW 51012, N'Không thể đổi loại của nhiệm vụ đang có câu hỏi.', 1;
END;
GO

-- 10. API: đồng bộ tài khoản và check-in

CREATE OR ALTER PROCEDURE dbo.usp_DongBoFirebase
    @FirebaseUid nvarchar(128), @Email nvarchar(254) = NULL,
    @EmailDaXacMinh bit = 0, @HoTenLanDau nvarchar(120) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @FirebaseUid IS NULL OR LEN(LTRIM(@FirebaseUid)) = 0 OR DATALENGTH(@FirebaseUid) <> DATALENGTH(RTRIM(@FirebaseUid))
       OR @EmailDaXacMinh IS NULL OR (@EmailDaXacMinh = 1 AND @Email IS NULL)
        THROW 51100, N'UID hoặc thông tin Firebase không hợp lệ.', 1;
    DECLARE @Id int, @HoatDong bit;
    BEGIN TRAN;
    SELECT @Id = NguoiDungId, @HoatDong = HoatDong
    FROM dbo.NguoiDung WITH (UPDLOCK, HOLDLOCK) WHERE FirebaseUid = @FirebaseUid;
    IF @HoatDong = 0
        THROW 51101, N'Tài khoản đã bị khóa tại hệ thống.', 1;
    IF @Id IS NULL
    BEGIN
        INSERT dbo.NguoiDung(FirebaseUid,HoTen,Email,EmailDaXacMinh)
        VALUES (@FirebaseUid, COALESCE(NULLIF(TRIM(@HoTenLanDau),N''),N'Thành viên'), @Email,@EmailDaXacMinh);
        SET @Id = CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro) VALUES (@Id,'THANH_VIEN');
    END
    ELSE
        UPDATE dbo.NguoiDung SET Email = @Email, EmailDaXacMinh = @EmailDaXacMinh, LanDongBoUtc = SYSUTCDATETIME()
        WHERE NguoiDungId = @Id;
    COMMIT;
    SELECT NguoiDungId, FirebaseUid, HoTen, Email, EmailDaXacMinh, AnhDaiDienPath, ChoPhepCaNhanHoa
    FROM dbo.NguoiDung WHERE NguoiDungId = @Id;
    SELECT MaVaiTro FROM dbo.NguoiDungVaiTro WHERE NguoiDungId = @Id ORDER BY MaVaiTro;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_CheckInDiSan
    @NguoiDungId int, @DiSanId int, @MaYeuCau uniqueidentifier,
    @ViDo decimal(9,6), @KinhDo decimal(9,6), @SaiSoGpsM decimal(8,2),
    @GhiChu nvarchar(500) = NULL, @AnhPath nvarchar(450) = NULL, @VideoPath nvarchar(450) = NULL,
    @CheckInId bigint = NULL OUTPUT, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @CheckInId = NULL;
    IF @DiSanId IS NULL OR @MaYeuCau IS NULL OR @ViDo IS NULL OR @KinhDo IS NULL OR @SaiSoGpsM IS NULL
       OR @ViDo NOT BETWEEN -90 AND 90 OR @KinhDo NOT BETWEEN -180 AND 180 OR @SaiSoGpsM <= 0
        THROW 51102, N'Thiếu mã yêu cầu hoặc tọa độ/độ chính xác không hợp lệ.', 1;
    DECLARE @ChoPhep bit, @DiSanCu int, @TamViDo decimal(9,6), @TamKinhDo decimal(9,6),
            @BanKinh smallint, @SaiSoToiDa smallint, @KhoangCach decimal(12,3),
            @Luc datetime2(3) = SYSUTCDATETIME();
    BEGIN TRAN;
    SELECT @ChoPhep = ChoPhepCaNhanHoa FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK)
    WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1;
    IF @ChoPhep IS NULL
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;

    SELECT @CheckInId = CheckInId, @DiSanCu = DiSanId
    FROM dbo.CheckInDiSan WHERE NguoiDungId = @NguoiDungId AND MaYeuCau = @MaYeuCau;
    IF @CheckInId IS NOT NULL AND @DiSanCu <> @DiSanId
        THROW 51104, N'Mã yêu cầu này đã được dùng cho di sản khác.', 1;

    IF @CheckInId IS NULL
    BEGIN
        SELECT @TamViDo = d.ViDo, @TamKinhDo = d.KinhDo, @BanKinh = d.BanKinhCheckInM, @SaiSoToiDa = d.SaiSoGpsToiDaM
        FROM dbo.DiSan d WITH (HOLDLOCK)
        JOIN dbo.TinhThanh t WITH (HOLDLOCK) ON t.TinhThanhId = d.TinhThanhId AND t.HoatDong = 1
        WHERE d.DiSanId = @DiSanId AND d.TrangThai = 'CONG_KHAI';
        IF @BanKinh IS NULL
            THROW 51105, N'Di sản hiện không mở check-in.', 1;
        IF @SaiSoGpsM > @SaiSoToiDa
            THROW 51106, N'Độ chính xác GPS chưa đạt. Hãy lấy lại vị trí.', 1;
        SET @KhoangCach = dbo.fn_KhoangCachM(@ViDo,@KinhDo,@TamViDo,@TamKinhDo);
        IF @KhoangCach > @BanKinh
            THROW 51107, N'Vị trí nằm ngoài bán kính check-in.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.CheckInDiSan
            WHERE NguoiDungId = @NguoiDungId AND DiSanId = @DiSanId
              AND NgayDiaPhuong = CONVERT(date,DATEADD(MINUTE,420,@Luc))
        )
            THROW 51108, N'Bạn đã check-in di sản này trong ngày.', 1;
        INSERT dbo.CheckInDiSan(NguoiDungId,DiSanId,MaYeuCau,ThoiGianUtc,ViDo,KinhDo,SaiSoGpsM,TamViDo,TamKinhDo,
                                BanKinhApDungM,SaiSoToiDaApDungM,KhoangCachM,GhiChu,AnhPath,VideoPath)
        VALUES (@NguoiDungId,@DiSanId,@MaYeuCau,@Luc,@ViDo,@KinhDo,@SaiSoGpsM,@TamViDo,@TamKinhDo,
                @BanKinh,@SaiSoToiDa,@KhoangCach,@GhiChu,@AnhPath,@VideoPath);
        SET @CheckInId = CONVERT(bigint,SCOPE_IDENTITY());
        IF @ChoPhep = 1
            INSERT dbo.SuKienTuongTac(MaSuKien,NguoiDungId,Loai,DiSanId,ThoiGianUtc)
            VALUES (NEWID(),@NguoiDungId,'CHECK_IN',@DiSanId,@Luc);
    END;
    COMMIT;
    IF @TraKetQua = 1
        SELECT CheckInId, DiSanId, ThoiGianUtc, NgayDiaPhuong, KhoangCachM, BanKinhApDungM
        FROM dbo.CheckInDiSan WHERE CheckInId = @CheckInId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GanCheckInVaoHanhTrinh
    @NguoiDungId int, @HanhTrinhId bigint, @CheckInId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @HanhTrinhCu bigint;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (HOLDLOCK) WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.HanhTrinh WHERE HanhTrinhId = @HanhTrinhId AND NguoiDungId = @NguoiDungId)
       OR NOT EXISTS (SELECT 1 FROM dbo.CheckInDiSan WHERE CheckInId = @CheckInId AND NguoiDungId = @NguoiDungId)
        THROW 51109, N'Hành trình hoặc check-in không thuộc tài khoản.', 1;
    SELECT @HanhTrinhCu = HanhTrinhId FROM dbo.HanhTrinhCheckIn WITH (UPDLOCK,HOLDLOCK) WHERE CheckInId = @CheckInId;
    IF @HanhTrinhCu IS NOT NULL AND @HanhTrinhCu <> @HanhTrinhId
        THROW 51110, N'Check-in đã được gắn với một hành trình khác.', 1;
    IF @HanhTrinhCu IS NULL
        INSERT dbo.HanhTrinhCheckIn(HanhTrinhId,CheckInId,NguoiDungId) VALUES (@HanhTrinhId,@CheckInId,@NguoiDungId);
    COMMIT;
END;
GO

-- 11. API: trải nghiệm bản địa và sức chứa

CREATE OR ALTER PROCEDURE dbo.usp_TaoYeuCauThamGia
    @NguoiDungId int, @LichTraiNghiemId int, @SoNguoi smallint,
    @MaYeuCau uniqueidentifier, @GhiChu nvarchar(500) = NULL, @YeuCauId bigint = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @YeuCauId = NULL;
    IF @LichTraiNghiemId IS NULL OR @MaYeuCau IS NULL OR @SoNguoi IS NULL OR @SoNguoi NOT BETWEEN 1 AND 500
        THROW 51120, N'Thiếu mã yêu cầu, lịch hoặc số người không hợp lệ.', 1;
    DECLARE @LichCu int, @SoNguoiCu smallint, @TraiNghiemId int, @Gia decimal(12,0);
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    SELECT @YeuCauId = YeuCauId, @LichCu = LichTraiNghiemId, @SoNguoiCu = SoNguoi
    FROM dbo.YeuCauThamGia WHERE NguoiDungId = @NguoiDungId AND MaYeuCau = @MaYeuCau;
    IF @YeuCauId IS NOT NULL AND (@LichCu <> @LichTraiNghiemId OR @SoNguoiCu <> @SoNguoi)
        THROW 51121, N'Mã yêu cầu đã dùng cho nội dung khác.', 1;
    IF @YeuCauId IS NULL
    BEGIN
        SELECT @TraiNghiemId = e.TraiNghiemId, @Gia = e.GiaMoiNguoi
        FROM dbo.LichTraiNghiem l WITH (UPDLOCK,HOLDLOCK)
        JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = l.TraiNghiemId
        WHERE l.LichTraiNghiemId = @LichTraiNghiemId AND l.TrangThai = 'MO'
          AND l.BatDauUtc > SYSUTCDATETIME() AND l.SucChua >= @SoNguoi;
        IF @TraiNghiemId IS NULL
            THROW 51122, N'Lịch không mở hoặc số khách vượt sức chứa.', 1;
        IF EXISTS (SELECT 1 FROM dbo.YeuCauThamGia WHERE LichTraiNghiemId = @LichTraiNghiemId AND NguoiDungId = @NguoiDungId
                    AND TrangThai IN ('CHO_XAC_NHAN','DA_XAC_NHAN','DA_THAM_GIA'))
            THROW 51123, N'Đã có yêu cầu còn hiệu lực cho lịch này.', 1;
        INSERT dbo.YeuCauThamGia(MaYeuCau,LichTraiNghiemId,TraiNghiemId,NguoiDungId,SoNguoi,DonGiaDuKien,GhiChu)
        VALUES (@MaYeuCau,@LichTraiNghiemId,@TraiNghiemId,@NguoiDungId,@SoNguoi,@Gia,@GhiChu);
        SET @YeuCauId = CONVERT(bigint,SCOPE_IDENTITY());
    END;
    COMMIT;
    SELECT YeuCauId, LichTraiNghiemId, SoNguoi, DonGiaDuKien, TrangThai
    FROM dbo.YeuCauThamGia WHERE YeuCauId = @YeuCauId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_DoiTrangThaiYeuCau
    @NguoiXuLyId int, @YeuCauId bigint, @TrangThaiMoi varchar(16)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @TrangThaiMoi IS NULL OR @TrangThaiMoi NOT IN ('DA_XAC_NHAN','TU_CHOI','DA_HUY','DA_THAM_GIA')
        THROW 51124, N'Trạng thái xử lý không hợp lệ.', 1;
    DECLARE @LichId int, @ChuYeuCau int, @DoiTacId int, @TrangThaiCu varchar(16), @BatDau datetime2(3);
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE NguoiDungId = @NguoiXuLyId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    SELECT @LichId = LichTraiNghiemId FROM dbo.YeuCauThamGia WHERE YeuCauId = @YeuCauId;
    IF @LichId IS NULL
        THROW 51125, N'Yêu cầu tham gia không tồn tại.', 1;
    SELECT @BatDau = BatDauUtc FROM dbo.LichTraiNghiem WITH (UPDLOCK,HOLDLOCK) WHERE LichTraiNghiemId = @LichId;
    SELECT @ChuYeuCau = y.NguoiDungId, @DoiTacId = e.DoiTacId, @TrangThaiCu = y.TrangThai
    FROM dbo.YeuCauThamGia y WITH (UPDLOCK,HOLDLOCK)
    JOIN dbo.TraiNghiem e ON e.TraiNghiemId = y.TraiNghiemId WHERE y.YeuCauId = @YeuCauId;
    IF @ChuYeuCau IS NULL
        THROW 51125, N'Yêu cầu tham gia không tồn tại.', 1;
    DECLARE @QuanTriHoacDoiTac bit = 0;
    IF EXISTS (SELECT 1 FROM dbo.NguoiDungVaiTro WHERE NguoiDungId = @NguoiXuLyId AND MaVaiTro = 'QUAN_TRI')
       OR EXISTS (SELECT 1 FROM dbo.ThanhVienDoiTac v
                  JOIN dbo.DoiTac p ON p.DoiTacId = v.DoiTacId AND p.HoatDong = 1
                  JOIN dbo.NguoiDungVaiTro r ON r.NguoiDungId = v.NguoiDungId AND r.MaVaiTro = 'DOI_TAC'
                  WHERE v.DoiTacId = @DoiTacId AND v.NguoiDungId = @NguoiXuLyId AND v.HoatDong = 1)
        SET @QuanTriHoacDoiTac = 1;
    IF @QuanTriHoacDoiTac = 0
       AND (@ChuYeuCau <> @NguoiXuLyId OR @TrangThaiMoi <> 'DA_HUY'
            OR (@TrangThaiCu <> 'DA_HUY' AND @BatDau <= SYSUTCDATETIME()))
        THROW 51126, N'Bạn không có quyền thực hiện thao tác với yêu cầu này.', 1;
    IF @TrangThaiCu <> @TrangThaiMoi
        UPDATE dbo.YeuCauThamGia SET TrangThai = @TrangThaiMoi, XuLyLucUtc = SYSUTCDATETIME(), NguoiXuLyId = @NguoiXuLyId
        WHERE YeuCauId = @YeuCauId;
    COMMIT;
    SELECT YeuCauId, TrangThai, XuLyLucUtc FROM dbo.YeuCauThamGia WHERE YeuCauId = @YeuCauId;
END;
GO

-- 12. API: Gemini, nhập lịch trình và sổ tay
-- Nguồn gửi dưới dạng [{"loai":"DI_SAN","id":1},{"loai":"TU_LIEU","id":1}].
-- Kế hoạch Gemini dùng {"diem":[{"ngay":1,"thuTu":1,"diSanId":1,"gio":"08:30"}]}.
-- ngay bắt đầu từ 1; một điểm có diSanId hoặc traiNghiemId, có thể kèm gio/ghiChu.

CREATE OR ALTER PROCEDURE dbo.usp_LayNguCanhGemini
    @TinhThanhId int = NULL, @DiSanId int = NULL, @TuKhoa nvarchar(100) = NULL, @GioiHan int = 12
AS
BEGIN
    SET NOCOUNT ON;
    IF @GioiHan IS NULL OR @GioiHan NOT BETWEEN 1 AND 40
        THROW 51130, N'Giới hạn ngữ cảnh phải từ 1 đến 40.', 1;
    SET @TuKhoa = NULLIF(TRIM(@TuKhoa),N'');
    SELECT TOP (@GioiHan) LoaiNguon, NguonId, DiSanId, TinhThanhId, TieuDe, NoiDung, UrlNguon
    FROM dbo.vw_NguCanhGemini
    WHERE (@TinhThanhId IS NULL OR TinhThanhId = @TinhThanhId)
      AND (@DiSanId IS NULL OR DiSanId = @DiSanId)
      AND (@TuKhoa IS NULL OR CHARINDEX(@TuKhoa COLLATE Latin1_General_100_CI_AI_SC,TieuDe COLLATE Latin1_General_100_CI_AI_SC) > 0
                           OR CHARINDEX(@TuKhoa COLLATE Latin1_General_100_CI_AI_SC,NoiDung COLLATE Latin1_General_100_CI_AI_SC) > 0)
    ORDER BY CASE LoaiNguon WHEN 'DI_SAN' THEN 1 WHEN 'TU_LIEU' THEN 2 ELSE 3 END, NguonId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_LuuTinNhanGemini
    @NguoiDungId int, @HoiThoaiId bigint, @Vai varchar(12), @NoiDung nvarchar(max),
    @MaMoHinh varchar(100) = NULL, @TokenVao int = NULL, @TokenRa int = NULL,
    @DeXuatHanhTrinhJson nvarchar(max) = NULL, @NguonJson nvarchar(max) = NULL,
    @TinNhanAIId bigint = NULL OUTPUT, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @TinNhanAIId = NULL;
    IF @NguonJson IS NOT NULL AND (@Vai <> 'ASSISTANT' OR ISJSON(@NguonJson) <> 1 OR LEFT(LTRIM(@NguonJson),1) <> '[')
        THROW 51131, N'Nguồn phải là mảng JSON của tin nhắn trợ lý.', 1;
    IF @DeXuatHanhTrinhJson IS NOT NULL AND (ISJSON(@DeXuatHanhTrinhJson) <> 1 OR LEFT(LTRIM(@DeXuatHanhTrinhJson),1) <> '{')
        THROW 51132, N'Kế hoạch Gemini phải là một đối tượng JSON.', 1;
    DECLARE @Nguon TABLE (ThuTu int PRIMARY KEY, Loai varchar(16), Id int);
    IF @NguonJson IS NOT NULL
        INSERT @Nguon(ThuTu,Loai,Id)
        SELECT CONVERT(int,j.[key])+1, p.Loai, p.Id
        FROM OPENJSON(@NguonJson) j
        OUTER APPLY OPENJSON(CASE WHEN j.[type]=5 THEN j.value ELSE N'{}' END)
            WITH (Loai varchar(16) '$.loai', Id int '$.id') p;
    IF (SELECT COUNT(*) FROM @Nguon) > 40
       OR EXISTS (SELECT 1 FROM @Nguon GROUP BY Loai,Id HAVING COUNT(*) > 1)
        THROW 51133, N'Danh sách nguồn bị trùng hoặc vượt giới hạn 40 mục.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.HoiThoaiAI h WITH (UPDLOCK,HOLDLOCK)
                   JOIN dbo.NguoiDung u ON u.NguoiDungId = h.NguoiDungId AND u.HoatDong = 1
                   WHERE h.HoiThoaiId = @HoiThoaiId AND h.NguoiDungId = @NguoiDungId AND h.DaLuuTru = 0)
        THROW 51134, N'Hội thoại không khả dụng cho tài khoản này.', 1;
    INSERT dbo.TinNhanAI(HoiThoaiId,NguoiDungId,Vai,NoiDung,NhaCungCap,MaMoHinh,TokenVao,TokenRa,DeXuatHanhTrinhJson)
    VALUES (@HoiThoaiId,@NguoiDungId,@Vai,@NoiDung,CASE WHEN @Vai='ASSISTANT' THEN 'GEMINI' END,
            @MaMoHinh,@TokenVao,@TokenRa,@DeXuatHanhTrinhJson);
    SET @TinNhanAIId = CONVERT(bigint,SCOPE_IDENTITY());
    INSERT dbo.NguonTraLoiAI(TinNhanAIId,NguoiDungId,ThuTu,DiSanId,TraiNghiemId,TuLieuId,TieuDeKhiTrich,UrlKhiTrich)
    SELECT @TinNhanAIId,@NguoiDungId,n.ThuTu,CASE WHEN n.Loai='DI_SAN' THEN n.Id END,
           CASE WHEN n.Loai='TRAI_NGHIEM' THEN n.Id END,CASE WHEN n.Loai='TU_LIEU' THEN n.Id END,v.TieuDe,v.UrlNguon
    FROM @Nguon n JOIN dbo.vw_NguCanhGemini v ON v.LoaiNguon = n.Loai AND v.NguonId = n.Id;
    IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Nguon)
        THROW 51135, N'Có nguồn không tồn tại hoặc chưa công khai.', 1;
    COMMIT;
    IF @TraKetQua = 1 SELECT @TinNhanAIId AS TinNhanAIId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TaoHanhTrinh
    @NguoiDungId int, @TieuDe nvarchar(160), @NgayBatDau date, @NgayKetThuc date,
    @MoTa nvarchar(1000) = NULL, @TinNhanAIId bigint = NULL,
    @KeHoachJson nvarchar(max) = NULL, @HanhTrinhId bigint = NULL OUTPUT, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @HanhTrinhId = NULL;
    IF @NgayBatDau IS NULL OR @NgayKetThuc IS NULL OR DATEDIFF(DAY,@NgayBatDau,@NgayKetThuc) NOT BETWEEN 0 AND 89
        THROW 51136, N'Khoảng ngày hành trình phải hợp lệ và tối đa 90 ngày.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    IF @TinNhanAIId IS NOT NULL
    BEGIN
        DECLARE @JsonDaLuu nvarchar(max);
        SELECT @JsonDaLuu = DeXuatHanhTrinhJson FROM dbo.TinNhanAI
        WHERE TinNhanAIId = @TinNhanAIId AND NguoiDungId = @NguoiDungId AND Vai = 'ASSISTANT';
        IF @JsonDaLuu IS NULL
            THROW 51137, N'Tin nhắn không chứa đề xuất hành trình của tài khoản này.', 1;
        SET @KeHoachJson = COALESCE(@KeHoachJson,@JsonDaLuu);
    END;
    DECLARE @Diem TABLE (Ngay int, ThuTu int, DiSanId int, TraiNghiemId int, Gio nvarchar(50), GhiChu nvarchar(500));
    IF @KeHoachJson IS NOT NULL
    BEGIN
        IF ISJSON(@KeHoachJson) <> 1 OR LEFT(LTRIM(@KeHoachJson),1) <> '{'
            THROW 51138, N'Kế hoạch phải là đối tượng JSON.', 1;
        DECLARE @Mang nvarchar(max) = JSON_QUERY(@KeHoachJson,'$.diem');
        IF @Mang IS NULL OR LEFT(LTRIM(@Mang),1) <> '['
            THROW 51138, N'Kế hoạch phải có mảng diem.', 1;
        INSERT @Diem(Ngay,ThuTu,DiSanId,TraiNghiemId,Gio,GhiChu)
        SELECT Ngay,ThuTu,DiSanId,TraiNghiemId,Gio,GhiChu
        FROM OPENJSON(@Mang) WITH (Ngay int '$.ngay', ThuTu int '$.thuTu', DiSanId int '$.diSanId',
             TraiNghiemId int '$.traiNghiemId', Gio nvarchar(50) '$.gio', GhiChu nvarchar(500) '$.ghiChu');
        IF (SELECT COUNT(*) FROM @Diem) > 300 OR EXISTS (
            SELECT 1 FROM @Diem x
            LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = x.DiSanId
            LEFT JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = x.TraiNghiemId
            WHERE x.Ngay IS NULL OR x.Ngay NOT BETWEEN 1 AND DATEDIFF(DAY,@NgayBatDau,@NgayKetThuc)+1
               OR x.ThuTu IS NULL OR x.ThuTu NOT BETWEEN 1 AND 100
               OR (x.Gio IS NOT NULL AND TRY_CONVERT(time(0),x.Gio) IS NULL)
               OR IIF(x.DiSanId IS NULL,0,1)+IIF(x.TraiNghiemId IS NULL,0,1) <> 1
               OR (x.DiSanId IS NOT NULL AND d.DiSanId IS NULL)
               OR (x.TraiNghiemId IS NOT NULL AND e.TraiNghiemId IS NULL)
        ) OR EXISTS (SELECT 1 FROM @Diem GROUP BY Ngay,ThuTu HAVING COUNT(*) > 1)
            THROW 51139, N'Kế hoạch có ngày, thứ tự, giờ hoặc địa điểm không hợp lệ.', 1;
    END;
    INSERT dbo.HanhTrinh(NguoiDungId,TieuDe,MoTa,NgayBatDau,NgayKetThuc,TinNhanAIId)
    VALUES (@NguoiDungId,@TieuDe,@MoTa,@NgayBatDau,@NgayKetThuc,@TinNhanAIId);
    SET @HanhTrinhId = CONVERT(bigint,SCOPE_IDENTITY());
    INSERT dbo.DiemHanhTrinh(HanhTrinhId,NgayDuKien,ThuTu,DiSanId,TraiNghiemId,GioDuKien,GhiChu)
    SELECT @HanhTrinhId,DATEADD(DAY,Ngay-1,@NgayBatDau),ThuTu,DiSanId,TraiNghiemId,TRY_CONVERT(time(0),Gio),GhiChu FROM @Diem;
    COMMIT;
    IF @TraKetQua = 1 SELECT @HanhTrinhId AS HanhTrinhId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TaoNhatKy
    @NguoiDungId int, @HanhTrinhId bigint, @TieuDe nvarchar(160), @NoiDung nvarchar(max),
    @NgayGhi date, @CheckInId bigint = NULL, @AnhBiaPath nvarchar(450) = NULL, @VideoPath nvarchar(450) = NULL,
    @NhatKyId bigint = NULL OUTPUT, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NhatKyId = NULL;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.HanhTrinh h
                   JOIN dbo.NguoiDung u ON u.NguoiDungId = h.NguoiDungId AND u.HoatDong = 1
                   WHERE h.HanhTrinhId = @HanhTrinhId AND h.NguoiDungId = @NguoiDungId)
        THROW 51140, N'Hành trình không khả dụng cho tài khoản này.', 1;
    IF @CheckInId IS NOT NULL
        EXEC dbo.usp_GanCheckInVaoHanhTrinh @NguoiDungId,@HanhTrinhId,@CheckInId;
    INSERT dbo.NhatKyHanhTrinh(HanhTrinhId,CheckInId,TieuDe,NoiDung,NgayGhi,AnhBiaPath,VideoPath)
    VALUES (@HanhTrinhId,@CheckInId,@TieuDe,@NoiDung,@NgayGhi,@AnhBiaPath,@VideoPath);
    SET @NhatKyId = CONVERT(bigint,SCOPE_IDENTITY());
    COMMIT;
    IF @TraKetQua = 1 SELECT NhatKyId, MaChiaSe, CongKhai FROM dbo.NhatKyHanhTrinh WHERE NhatKyId = @NhatKyId;
END;
GO

-- 13. API: tiến độ nhiệm vụ, chấm quiz và cấp huy hiệu

CREATE OR ALTER PROCEDURE dbo.usp_CapNhatTienDoNhiemVu
    @NguoiDungId int, @NhiemVuId int, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Loai varchar(16), @LoaiDiSanId int, @DiSanId int, @HuyHieuId int,
            @MucTieu int, @DaDat int, @BatDau datetime2(3), @KetThuc datetime2(3),
            @Luc datetime2(3) = SYSUTCDATETIME(), @HoanThanh datetime2(3);
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    SELECT @Loai = Loai, @LoaiDiSanId = LoaiDiSanId, @DiSanId = DiSanId, @HuyHieuId = HuyHieuId,
           @MucTieu = SoDiSanCanDat, @BatDau = BatDauUtc, @KetThuc = KetThucUtc
    FROM dbo.NhiemVu WITH (HOLDLOCK) WHERE NhiemVuId = @NhiemVuId AND CongKhai = 1;
    IF @Loai IS NULL OR @BatDau > @Luc
        THROW 51150, N'Nhiệm vụ không tồn tại hoặc chưa mở.', 1;
    SELECT @HoanThanh = HoanThanhLucUtc FROM dbo.TienDoNhiemVu
    WHERE NguoiDungId = @NguoiDungId AND NhiemVuId = @NhiemVuId;
    IF @HoanThanh IS NULL
    BEGIN
        IF @Loai = 'QUIZ'
        BEGIN
            SELECT @MucTieu = COUNT(*) FROM dbo.CauHoiNhiemVu WHERE NhiemVuId = @NhiemVuId;
            IF @MucTieu = 0
                THROW 51151, N'Nhiệm vụ quiz chưa có câu hỏi.', 1;
            SELECT @DaDat = COUNT(DISTINCT k.CauHoiId)
            FROM dbo.KetQuaTraLoi k JOIN dbo.CauHoiNhiemVu q ON q.CauHoiId = k.CauHoiId
            WHERE q.NhiemVuId = @NhiemVuId AND k.NguoiDungId = @NguoiDungId AND k.DungKhiNop = 1
              AND k.NopLucUtc >= @BatDau AND (@KetThuc IS NULL OR k.NopLucUtc < @KetThuc);
        END
        ELSE
            SELECT @DaDat = COUNT(DISTINCT k.DiSanId)
            FROM dbo.CheckInDiSan k JOIN dbo.DiSan d ON d.DiSanId = k.DiSanId
            WHERE k.NguoiDungId = @NguoiDungId AND (@LoaiDiSanId IS NULL OR d.LoaiDiSanId = @LoaiDiSanId)
              AND k.ThoiGianUtc >= @BatDau AND (@KetThuc IS NULL OR k.ThoiGianUtc < @KetThuc);

        SET @HoanThanh = CASE WHEN @DaDat >= @MucTieu THEN @Luc END;
        UPDATE dbo.TienDoNhiemVu SET SoDaDat = @DaDat, MucTieu = @MucTieu, CapNhatLucUtc = @Luc, HoanThanhLucUtc = @HoanThanh
        WHERE NguoiDungId = @NguoiDungId AND NhiemVuId = @NhiemVuId;
        IF @@ROWCOUNT = 0
            INSERT dbo.TienDoNhiemVu(NguoiDungId,NhiemVuId,SoDaDat,MucTieu,CapNhatLucUtc,HoanThanhLucUtc)
            VALUES (@NguoiDungId,@NhiemVuId,@DaDat,@MucTieu,@Luc,@HoanThanh);
        IF @HoanThanh IS NOT NULL AND @HuyHieuId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.HuyHieuNguoiDung WHERE NguoiDungId = @NguoiDungId AND HuyHieuId = @HuyHieuId)
            INSERT dbo.HuyHieuNguoiDung(NguoiDungId,HuyHieuId,NhiemVuId,NhanLucUtc)
            VALUES (@NguoiDungId,@HuyHieuId,@NhiemVuId,@Luc);
    END;
    COMMIT;
    IF @TraKetQua = 1
        SELECT NhiemVuId, SoDaDat, MucTieu, CapNhatLucUtc, HoanThanhLucUtc
        FROM dbo.TienDoNhiemVu WHERE NguoiDungId = @NguoiDungId AND NhiemVuId = @NhiemVuId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_NopCauTraLoi
    @NguoiDungId int, @CauHoiId int, @LuaChonId int, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @NhiemVuId int, @Dung bit, @Luc datetime2(3) = SYSUTCDATETIME();
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1)
        THROW 51103, N'Tài khoản không tồn tại hoặc không hoạt động.', 1;
    SELECT @NhiemVuId = n.NhiemVuId, @Dung = a.Dung
    FROM dbo.LuaChonCauHoi a WITH (HOLDLOCK)
    JOIN dbo.CauHoiNhiemVu c WITH (HOLDLOCK) ON c.CauHoiId = a.CauHoiId
    JOIN dbo.NhiemVu n WITH (HOLDLOCK) ON n.NhiemVuId = c.NhiemVuId
    WHERE a.CauHoiId = @CauHoiId AND a.LuaChonId = @LuaChonId AND n.Loai = 'QUIZ' AND n.CongKhai = 1
      AND n.BatDauUtc <= @Luc AND (n.KetThucUtc IS NULL OR @Luc < n.KetThucUtc);
    IF @NhiemVuId IS NULL
        THROW 51152, N'Lựa chọn không thuộc câu hỏi hoặc nhiệm vụ không mở.', 1;
    IF (SELECT COUNT(*) FROM dbo.LuaChonCauHoi WHERE CauHoiId = @CauHoiId) < 2
       OR NOT EXISTS (SELECT 1 FROM dbo.LuaChonCauHoi WHERE CauHoiId = @CauHoiId AND Dung = 1)
        THROW 51153, N'Câu hỏi cần từ hai lựa chọn và một đáp án đúng.', 1;
    INSERT dbo.KetQuaTraLoi(NguoiDungId,CauHoiId,LuaChonId,DungKhiNop,NopLucUtc)
    VALUES (@NguoiDungId,@CauHoiId,@LuaChonId,@Dung,@Luc);
    EXEC dbo.usp_CapNhatTienDoNhiemVu @NguoiDungId,@NhiemVuId,@TraKetQua=0;
    COMMIT;
    IF @TraKetQua = 1
        SELECT @Dung AS TraLoiDung, p.SoDaDat, p.MucTieu, p.HoanThanhLucUtc
        FROM dbo.TienDoNhiemVu p WHERE p.NguoiDungId = @NguoiDungId AND p.NhiemVuId = @NhiemVuId;
END;
GO

-- 14. API: lưu kết quả ML, log phản hồi và gợi ý có thể demo ngay

CREATE OR ALTER PROCEDURE dbo.usp_LuuLuotGoiY
    @NguoiDungId int, @MoHinhId int, @ViTriHienThi varchar(24),
    @DanhSachJson nvarchar(max), @LuotGoiYId bigint = NULL OUTPUT, @TraKetQua bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @LuotGoiYId = NULL;
    IF @DanhSachJson IS NULL OR ISJSON(@DanhSachJson) <> 1 OR LEFT(LTRIM(@DanhSachJson),1) <> '['
        THROW 51160, N'Danh sách gợi ý phải là một mảng JSON.', 1;
    DECLARE @Muc TABLE (Hang int PRIMARY KEY, DiSanId int, TraiNghiemId int, Diem decimal(9,6), LyDo nvarchar(350));
    INSERT @Muc(Hang,DiSanId,TraiNghiemId,Diem,LyDo)
    SELECT CONVERT(int,j.[key])+1, p.DiSanId, p.TraiNghiemId, p.Diem, p.LyDo
    FROM OPENJSON(@DanhSachJson) j
    OUTER APPLY OPENJSON(CASE WHEN j.[type]=5 THEN j.value ELSE N'{}' END)
        WITH (DiSanId int '$.diSanId', TraiNghiemId int '$.traiNghiemId', Diem decimal(9,6) '$.diem', LyDo nvarchar(350) '$.lyDo') p;
    IF (SELECT COUNT(*) FROM @Muc) NOT BETWEEN 1 AND 50 OR EXISTS (
        SELECT 1 FROM @Muc m
        LEFT JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = m.DiSanId
        LEFT JOIN dbo.vw_TraiNghiemCongKhai e ON e.TraiNghiemId = m.TraiNghiemId
        WHERE IIF(m.DiSanId IS NULL,0,1)+IIF(m.TraiNghiemId IS NULL,0,1) <> 1
           OR (m.DiSanId IS NOT NULL AND d.DiSanId IS NULL) OR (m.TraiNghiemId IS NOT NULL AND e.TraiNghiemId IS NULL)
           OR m.Diem IS NULL OR m.Diem NOT BETWEEN 0 AND 1
    ) OR EXISTS (SELECT 1 FROM @Muc GROUP BY DiSanId,TraiNghiemId HAVING COUNT(*) > 1)
        THROW 51161, N'Danh sách gợi ý bị trùng, vượt giới hạn hoặc có mục không hợp lệ.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK)
                   WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1 AND ChoPhepCaNhanHoa = 1)
        THROW 51162, N'Tài khoản chưa bật cá nhân hóa hoặc không hoạt động.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.MoHinhGoiY WITH (HOLDLOCK) WHERE MoHinhId = @MoHinhId AND HoatDong = 1)
        THROW 51163, N'Mô hình gợi ý chưa hoạt động.', 1;
    INSERT dbo.LuotGoiY(NguoiDungId,MoHinhId,ViTriHienThi) VALUES (@NguoiDungId,@MoHinhId,@ViTriHienThi);
    SET @LuotGoiYId = CONVERT(bigint,SCOPE_IDENTITY());
    INSERT dbo.ChiTietGoiY(LuotGoiYId,ThuHang,DiSanId,TraiNghiemId,Diem,LyDo)
    SELECT @LuotGoiYId,Hang,DiSanId,TraiNghiemId,Diem,LyDo FROM @Muc;
    COMMIT;
    IF @TraKetQua = 1
        SELECT LuotGoiYId, ThuHang, DiSanId, TraiNghiemId, Diem, LyDo
        FROM dbo.ChiTietGoiY WHERE LuotGoiYId = @LuotGoiYId ORDER BY ThuHang;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GhiSuKienTuongTac
    @NguoiDungId int, @MaSuKien uniqueidentifier, @Loai varchar(20),
    @DiSanId int = NULL, @TraiNghiemId int = NULL, @LuotGoiYId bigint = NULL, @ThuHang smallint = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Loai IS NULL OR @Loai NOT IN ('XEM','NHAN_GOI_Y','AN_GOI_Y') OR @MaSuKien IS NULL
        THROW 51164, N'Mã hoặc loại sự kiện giao diện không hợp lệ.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK)
                   WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1 AND ChoPhepCaNhanHoa = 1)
        THROW 51162, N'Tài khoản chưa bật cá nhân hóa hoặc không hoạt động.', 1;
    IF EXISTS (SELECT 1 FROM dbo.SuKienTuongTac WHERE NguoiDungId = @NguoiDungId AND MaSuKien = @MaSuKien)
    BEGIN
        IF NOT EXISTS (
            SELECT @Loai,@DiSanId,@TraiNghiemId,@LuotGoiYId,@ThuHang
            INTERSECT
            SELECT Loai,DiSanId,TraiNghiemId,LuotGoiYId,ThuHang FROM dbo.SuKienTuongTac
            WHERE NguoiDungId = @NguoiDungId AND MaSuKien = @MaSuKien
        )
            THROW 51165, N'Mã sự kiện đã được dùng cho nội dung khác.', 1;
    END
    ELSE
    BEGIN
        IF @Loai = 'XEM' AND (
            (@DiSanId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.vw_DiSanCongKhai WHERE DiSanId=@DiSanId))
            OR (@TraiNghiemId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.vw_TraiNghiemCongKhai WHERE TraiNghiemId=@TraiNghiemId))
        )
            THROW 51167, N'Chỉ ghi lượt xem của nội dung công khai.', 1;
        INSERT dbo.SuKienTuongTac(MaSuKien,NguoiDungId,Loai,DiSanId,TraiNghiemId,LuotGoiYId,ThuHang)
        VALUES (@MaSuKien,@NguoiDungId,@Loai,@DiSanId,@TraiNghiemId,@LuotGoiYId,@ThuHang);
    END;
    COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GoiYTheoSoThich
    @NguoiDungId int = NULL, @TinhThanhId int = NULL, @GioiHan int = 10
AS
BEGIN
    SET NOCOUNT ON;
    IF @GioiHan IS NULL OR @GioiHan NOT BETWEEN 1 AND 50
        THROW 51166, N'Giới hạn gợi ý phải từ 1 đến 50.', 1;
    DECLARE @CaNhanHoa bit = COALESCE((SELECT ChoPhepCaNhanHoa FROM dbo.NguoiDung
                                    WHERE NguoiDungId = @NguoiDungId AND HoatDong = 1),0);
    ;WITH ChuDeQuanTam AS (
        SELECT s.ChuDeId, CONVERT(int,s.MucDo)*3 AS Diem
        FROM dbo.SoThichNguoiDung s WHERE s.NguoiDungId = @NguoiDungId AND @CaNhanHoa = 1
        UNION ALL
        SELECT c.ChuDeId, 2 FROM dbo.DiSanYeuThich f
        JOIN dbo.DiSanChuDe c ON c.DiSanId = f.DiSanId
        WHERE f.NguoiDungId = @NguoiDungId AND @CaNhanHoa = 1
        UNION ALL
        SELECT c.ChuDeId, 1 FROM (SELECT DISTINCT DiSanId FROM dbo.CheckInDiSan WHERE NguoiDungId = @NguoiDungId) k
        JOIN dbo.DiSanChuDe c ON c.DiSanId = k.DiSanId WHERE @CaNhanHoa = 1
    ),
    DiemTheoDiSan AS (
        SELECT c.DiSanId, SUM(q.Diem) AS Diem
        FROM dbo.DiSanChuDe c JOIN ChuDeQuanTam q ON q.ChuDeId = c.ChuDeId GROUP BY c.DiSanId
    ),
    LuotLuu AS (
        SELECT DiSanId, COUNT_BIG(*) AS SoLuotLuu FROM dbo.DiSanYeuThich GROUP BY DiSanId
    )
    SELECT TOP (@GioiHan) d.DiSanId, d.Ten, d.Slug, d.AnhBiaPath,
           CONVERT(decimal(9,6), COALESCE(p.Diem,0)/(COALESCE(p.Diem,0)+20.0)) AS Diem,
           CASE WHEN p.Diem > 0 THEN N'Khớp sở thích và chủ đề đã khám phá'
                ELSE N'Địa điểm đang được khám phá' END AS LyDo
    FROM dbo.vw_DiSanCongKhai d LEFT JOIN DiemTheoDiSan p ON p.DiSanId = d.DiSanId
    LEFT JOIN LuotLuu l ON l.DiSanId = d.DiSanId
    WHERE (@TinhThanhId IS NULL OR d.TinhThanhId = @TinhThanhId)
    ORDER BY COALESCE(p.Diem,0) DESC, COALESCE(l.SoLuotLuu,0) DESC, d.DiSanId;
END;
GO

-- 15. API: quản trị nội dung đưa lên danh mục và cung cấp cho Gemini

CREATE OR ALTER PROCEDURE dbo.usp_DuyetTraiNghiem
    @QuanTriId int, @TraiNghiemId int, @CongKhai bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @CongKhai IS NULL
        THROW 51170, N'Thiếu lựa chọn duyệt hoặc ẩn.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.NguoiDung u WITH (HOLDLOCK)
        JOIN dbo.NguoiDungVaiTro r WITH (HOLDLOCK) ON r.NguoiDungId = u.NguoiDungId AND r.MaVaiTro = 'QUAN_TRI'
        WHERE u.NguoiDungId = @QuanTriId AND u.HoatDong = 1
    )
        THROW 51171, N'Chỉ quản trị viên đang hoạt động được duyệt nội dung.', 1;
    UPDATE e SET TrangThai = CASE WHEN @CongKhai = 1 THEN 'CONG_KHAI' ELSE 'AN' END,
        NguoiDuyetId = @QuanTriId, DuyetLucUtc = SYSUTCDATETIME()
    FROM dbo.TraiNghiem e
    JOIN dbo.DoiTac p ON p.DoiTacId = e.DoiTacId
    WHERE e.TraiNghiemId = @TraiNghiemId
      AND ((@CongKhai = 1 AND e.TrangThai IN ('CHO_DUYET','AN','CONG_KHAI') AND p.HoatDong = 1
            AND EXISTS (SELECT 1 FROM dbo.vw_DiSanCongKhai d WHERE d.DiSanId = e.DiSanId))
           OR (@CongKhai = 0 AND e.TrangThai IN ('CHO_DUYET','CONG_KHAI','AN')));
    IF @@ROWCOUNT = 0
        THROW 51172, N'Trải nghiệm chưa gửi duyệt hoặc không còn đủ điều kiện hiển thị.', 1;
    COMMIT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_DuyetTuLieu
    @QuanTriId int, @TuLieuId int, @CongKhai bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @CongKhai IS NULL
        THROW 51170, N'Thiếu lựa chọn duyệt hoặc ẩn.', 1;
    BEGIN TRAN;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.NguoiDung u WITH (HOLDLOCK)
        JOIN dbo.NguoiDungVaiTro r WITH (HOLDLOCK) ON r.NguoiDungId = u.NguoiDungId AND r.MaVaiTro = 'QUAN_TRI'
        WHERE u.NguoiDungId = @QuanTriId AND u.HoatDong = 1
    )
        THROW 51171, N'Chỉ quản trị viên đang hoạt động được duyệt nội dung.', 1;
    UPDATE s SET CongKhai = @CongKhai,
        NguoiDuyetId = CASE WHEN @CongKhai = 1 THEN @QuanTriId ELSE NguoiDuyetId END,
        DuyetLucUtc = CASE WHEN @CongKhai = 1 THEN SYSUTCDATETIME() ELSE DuyetLucUtc END
    FROM dbo.TuLieuDiSan s
    WHERE s.TuLieuId = @TuLieuId
      AND (@CongKhai = 0 OR EXISTS (SELECT 1 FROM dbo.vw_DiSanCongKhai d WHERE d.DiSanId = s.DiSanId));
    IF @@ROWCOUNT = 0
        THROW 51173, N'Tư liệu không tồn tại hoặc di sản chưa công khai.', 1;
    COMMIT;
END;
GO
-- 16. Dữ liệu mẫu. Dùng ID tra theo mã hoặc slug cho những luồng liên bảng.

DECLARE @Luc datetime2(3) = SYSUTCDATETIME();
DECLARE @HomNay date = CONVERT(date, DATEADD(MINUTE,420,@Luc));
DECLARE @NgayMai date = DATEADD(DAY,1,@HomNay);
DECLARE @QuaKhu datetime2(3) = DATEADD(DAY,-7,@Luc);
DECLARE @TuongLai datetime2(3) = DATEADD(DAY,7,@Luc);

INSERT dbo.VaiTro(MaVaiTro,Ten)
VALUES ('THANH_VIEN',N'Thành viên'),('QUAN_TRI',N'Quản trị viên'),('DOI_TAC',N'Đối tác bản địa');
INSERT dbo.NguoiDung(FirebaseUid,HoTen,Email,AnhDaiDienPath,ChoPhepCaNhanHoa)
VALUES (N'demo-admin',N'Quản trị Dấu Chân',N'admin@example.test',NULL,0),
       (N'demo-lan',N'Nguyễn An Lan',N'lan@example.test',N'demo/nguoi-dung/lan/avatar.jpg',1),
       (N'demo-khoi',N'Trần Minh Khôi',N'khoi@example.test',NULL,1),
       (N'demo-doi-tac',N'Lê Thanh Mai',N'mai@example.test',NULL,0);
DECLARE @Admin int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-admin');
DECLARE @Lan int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-lan');
DECLARE @Khoi int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-khoi');
DECLARE @DoiTacUser int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-doi-tac');
INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro)
SELECT NguoiDungId,'THANH_VIEN' FROM dbo.NguoiDung;
INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro) VALUES (@Admin,'QUAN_TRI'),(@DoiTacUser,'DOI_TAC');

INSERT dbo.TinhThanh(Ma,Ten) VALUES ('DA_NANG',N'Đà Nẵng'),('HUE',N'Huế'),('HA_NOI',N'Hà Nội');
INSERT dbo.LoaiDiSan(Ma,Ten,MoTa)
VALUES ('KHU_DI_TICH',N'Khu di tích',N'Cụm công trình hoặc không gian di tích.'),
       ('LANG_NGHE',N'Làng nghề',N'Không gian thực hành nghề truyền thống.'),
       ('DO_THI_CO',N'Đô thị cổ',N'Không gian đô thị có giá trị văn hóa, lịch sử.');
INSERT dbo.ChuDe(Ma,Ten)
VALUES ('KIEN_TRUC',N'Kiến trúc'),('LICH_SU',N'Lịch sử'),('THU_CONG',N'Thủ công'),('GOM',N'Gốm');
DECLARE @DaNang int = (SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma='DA_NANG');
DECLARE @Hue int = (SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma='HUE');
DECLARE @HaNoi int = (SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma='HA_NOI');
DECLARE @DiTich int = (SELECT LoaiDiSanId FROM dbo.LoaiDiSan WHERE Ma='KHU_DI_TICH');
DECLARE @LangNghe int = (SELECT LoaiDiSanId FROM dbo.LoaiDiSan WHERE Ma='LANG_NGHE');
DECLARE @DoThiCo int = (SELECT LoaiDiSanId FROM dbo.LoaiDiSan WHERE Ma='DO_THI_CO');

INSERT dbo.DiSan(TinhThanhId,LoaiDiSanId,Slug,Ten,TomTat,NoiDung,DiaChi,ViDo,KinhDo,
                BanKinhCheckInM,SaiSoGpsToiDaM,ThoiLuongPhut,AnhBiaPath,VideoPath,TrangThai)
VALUES
(@DaNang,@DoThiCo,'pho-co-hoi-an',N'Phố cổ Hội An',N'Tìm hiểu không gian phố cổ và kiến trúc nhà phố.',
 N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Khu vực phố cổ Hội An',15.877002,108.326301,
 180,60,90,N'demo/di-san/hoi-an/bia.jpg',N'demo/di-san/hoi-an/gioi-thieu.mp4','CONG_KHAI'),
(@DaNang,@LangNghe,'lang-gom-thanh-ha',N'Làng gốm Thanh Hà',N'Tìm hiểu cách tạo hình và nung gốm truyền thống.',
 N'Có thể kết hợp tham quan với một buổi thực hành thủ công.',N'Khu vực làng gốm Thanh Hà',15.878641,108.294673,
 150,50,75,N'demo/di-san/thanh-ha/bia.jpg',NULL,'CONG_KHAI'),
(@DaNang,@DiTich,'thanh-dia-my-son',N'Thánh địa Mỹ Sơn',N'Khám phá cụm tháp và kiến trúc gạch.',
 NULL,N'Khu vực di tích Mỹ Sơn',15.764811,108.124485,
 200,60,100,N'demo/di-san/my-son/bia.jpg',NULL,'CONG_KHAI'),
(@Hue,@DiTich,'dai-noi-hue',N'Đại Nội Huế',N'Tìm hiểu bố cục và kiến trúc khu vực cung đình.',
 NULL,N'Khu vực Đại Nội Huế',16.469948,107.577571,
 220,60,120,N'demo/di-san/dai-noi-hue/bia.jpg',NULL,'CONG_KHAI'),
(@HaNoi,@LangNghe,'lang-gom-bat-trang',N'Làng gốm Bát Tràng',N'Tìm hiểu nghề gốm và các sản phẩm thủ công.',
 NULL,N'Khu vực làng gốm Bát Tràng',20.975201,105.911732,
 150,50,90,N'demo/di-san/bat-trang/bia.jpg',NULL,'CONG_KHAI'),
(@DaNang,@DiTich,'diem-nhap-demo',N'Điểm di sản đang biên soạn',N'Bản nháp để thử bộ lọc nội dung.',
 NULL,N'Địa điểm giả lập',15.888800,108.300000,100,50,45,NULL,NULL,'NHAP');

DECLARE @HoiAn int = (SELECT DiSanId FROM dbo.DiSan WHERE Slug='pho-co-hoi-an');
DECLARE @ThanhHa int = (SELECT DiSanId FROM dbo.DiSan WHERE Slug='lang-gom-thanh-ha');
DECLARE @MySon int = (SELECT DiSanId FROM dbo.DiSan WHERE Slug='thanh-dia-my-son');
DECLARE @DaiNoi int = (SELECT DiSanId FROM dbo.DiSan WHERE Slug='dai-noi-hue');
DECLARE @BatTrang int = (SELECT DiSanId FROM dbo.DiSan WHERE Slug='lang-gom-bat-trang');
INSERT dbo.DiSanChuDe(DiSanId,ChuDeId)
SELECT m.DiSanId,c.ChuDeId
FROM (VALUES (@HoiAn,'KIEN_TRUC'),(@HoiAn,'LICH_SU'),(@ThanhHa,'THU_CONG'),(@ThanhHa,'GOM'),
             (@MySon,'KIEN_TRUC'),(@MySon,'LICH_SU'),(@DaiNoi,'KIEN_TRUC'),(@DaiNoi,'LICH_SU'),
             (@BatTrang,'THU_CONG'),(@BatTrang,'GOM')) m(DiSanId,Ma)
JOIN dbo.ChuDe c ON c.Ma=m.Ma;
INSERT dbo.SoThichNguoiDung(NguoiDungId,ChuDeId,MucDo)
SELECT m.NguoiDungId,c.ChuDeId,m.MucDo
FROM (VALUES (@Lan,'THU_CONG',5),(@Lan,'KIEN_TRUC',4),(@Khoi,'LICH_SU',5)) m(NguoiDungId,Ma,MucDo)
JOIN dbo.ChuDe c ON c.Ma=m.Ma;
INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
VALUES (@HoiAn,N'Gợi ý quan sát nhà phố',N'Quan sát mặt tiền, mái ngói và vật liệu dọc tuyến đi bộ.',
        N'Biên tập minh họa',NULL,@Admin,@Luc,1),
       (@ThanhHa,N'Các bước tạo sản phẩm gốm',N'Người tham gia thực hành tạo hình đất trước khi chuyển sang các bước tiếp theo.',
        N'Biên tập minh họa',NULL,@Admin,@Luc,1),
       (@MySon,N'Bản ghi đang biên soạn',N'Nội dung chưa xuất bản để thử bộ lọc Gemini.',
        N'Bản nháp',NULL,NULL,NULL,0);
DECLARE @TuLieu int = (SELECT TuLieuId FROM dbo.TuLieuDiSan WHERE DiSanId=@ThanhHa AND CongKhai=1);
INSERT dbo.MediaDiSan(DiSanId,AnhPath,VideoPath,ChuThich,ThuTu)
VALUES (@HoiAn,N'demo/di-san/hoi-an/pho.jpg',NULL,N'Góc phố minh họa',1),
       (@HoiAn,NULL,N'demo/di-san/hoi-an/di-bo.mp4',N'Video đi bộ minh họa',2),
       (@ThanhHa,N'demo/di-san/thanh-ha/ban-xoay.jpg',NULL,N'Bàn xoay gốm',1);

INSERT dbo.DoiTac(TinhThanhId,Ten,MoTa,DienThoai,Email,DiaChi,AnhDaiDienPath)
VALUES (@DaNang,N'Xưởng Gốm Mộc Demo',N'Đối tác giả lập để thử yêu cầu tham gia.',
        NULL,N'xuonggom@example.test',N'Khu vực Thanh Hà',N'demo/doi-tac/xuong-gom/logo.png'),
       (@DaNang,N'Nhóm Dạo Phố Demo',N'Nhóm hướng dẫn tham quan giả lập.',
        NULL,N'daopho@example.test',N'Khu vực phố cổ Hội An',NULL);
DECLARE @XuongGom int = (SELECT DoiTacId FROM dbo.DoiTac WHERE Ten=N'Xưởng Gốm Mộc Demo');
DECLARE @DaoPho int = (SELECT DoiTacId FROM dbo.DoiTac WHERE Ten=N'Nhóm Dạo Phố Demo');
INSERT dbo.ThanhVienDoiTac(DoiTacId,NguoiDungId,Quyen) VALUES (@XuongGom,@DoiTacUser,'QUAN_LY');
INSERT dbo.TraiNghiem(DoiTacId,DiSanId,Slug,Ten,MoTa,DiaDiemTapTrung,ThoiLuongPhut,GiaMoiNguoi,
                     AnhBiaPath,VideoPath,TrangThai,NguoiDuyetId,DuyetLucUtc)
VALUES (@XuongGom,@ThanhHa,'nan-gom-thanh-ha',N'Thực hành nặn gốm',N'Làm quen với bàn xoay và thử tạo hình một món đồ nhỏ.',
        N'Cổng khu trải nghiệm mẫu',60,120000,N'demo/trai-nghiem/nan-gom/bia.jpg',NULL,'CONG_KHAI',@Admin,@Luc),
       (@DaoPho,@HoiAn,'doc-kien-truc-pho-co',N'Đi bộ tìm hiểu kiến trúc phố cổ',N'Dạo phố và nghe giới thiệu các chi tiết kiến trúc.',
        N'Điểm hẹn mẫu tại phố cổ',75,150000,NULL,N'demo/trai-nghiem/dao-pho/gioi-thieu.mp4','CONG_KHAI',@Admin,@Luc),
       (@XuongGom,@ThanhHa,'to-mau-gom-demo',N'Tô màu trên gốm',N'Hoạt động chờ duyệt.',NULL,45,NULL,NULL,NULL,'CHO_DUYET',NULL,NULL);
DECLARE @Gom int = (SELECT TraiNghiemId FROM dbo.TraiNghiem WHERE Slug='nan-gom-thanh-ha');
DECLARE @Pho int = (SELECT TraiNghiemId FROM dbo.TraiNghiem WHERE Slug='doc-kien-truc-pho-co');
INSERT dbo.MediaTraiNghiem(TraiNghiemId,AnhPath,VideoPath,ChuThich,ThuTu)
VALUES (@Gom,N'demo/trai-nghiem/nan-gom/thanh-pham.jpg',NULL,N'Sản phẩm thực hành',1),
       (@Gom,NULL,N'demo/trai-nghiem/nan-gom/thao-tac.mp4',N'Thao tác trên bàn xoay',2);
INSERT dbo.LichTraiNghiem(TraiNghiemId,BatDauUtc,KetThucUtc,SucChua,TrangThai)
VALUES (@Gom,@QuaKhu,DATEADD(MINUTE,60,@QuaKhu),10,'DONG'),
       (@Gom,@TuongLai,DATEADD(MINUTE,60,@TuongLai),5,'MO'),
       (@Pho,@TuongLai,DATEADD(MINUTE,75,@TuongLai),8,'MO');
DECLARE @LichCu int = (SELECT LichTraiNghiemId FROM dbo.LichTraiNghiem WHERE TraiNghiemId=@Gom AND BatDauUtc=@QuaKhu);
DECLARE @LichMoi int = (SELECT LichTraiNghiemId FROM dbo.LichTraiNghiem WHERE TraiNghiemId=@Gom AND BatDauUtc=@TuongLai);
DECLARE @DaThamGia bigint;
INSERT dbo.YeuCauThamGia(LichTraiNghiemId,TraiNghiemId,NguoiDungId,SoNguoi,DonGiaDuKien,TrangThai,TaoLucUtc,XuLyLucUtc,NguoiXuLyId)
VALUES (@LichCu,@Gom,@Lan,2,120000,'DA_THAM_GIA',DATEADD(DAY,-1,@QuaKhu),DATEADD(MINUTE,61,@QuaKhu),@DoiTacUser);
SET @DaThamGia = CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.YeuCauThamGia(LichTraiNghiemId,TraiNghiemId,NguoiDungId,SoNguoi,DonGiaDuKien,TrangThai,TaoLucUtc,XuLyLucUtc,NguoiXuLyId)
VALUES (@LichMoi,@Gom,@Khoi,2,120000,'DA_XAC_NHAN',@Luc,@Luc,@DoiTacUser);
INSERT dbo.YeuCauThamGia(LichTraiNghiemId,TraiNghiemId,NguoiDungId,SoNguoi,DonGiaDuKien,GhiChu)
VALUES (@LichMoi,@Gom,@Lan,2,120000,N'Muốn tham gia cùng một người bạn.');
INSERT dbo.DiSanYeuThich(NguoiDungId,DiSanId) VALUES (@Lan,@ThanhHa),(@Lan,@HoiAn),(@Khoi,@MySon);

DECLARE @HoiThoai bigint, @TinNhan bigint, @HanhTrinh bigint, @NhatKy bigint, @CheckIn bigint, @CheckInThanhHa bigint;
INSERT dbo.HoiThoaiAI(NguoiDungId,TieuDe) VALUES (@Lan,N'Nửa ngày khám phá Hội An');
SET @HoiThoai = CONVERT(bigint,SCOPE_IDENTITY());
EXEC dbo.usp_LuuTinNhanGemini @Lan,@HoiThoai,'USER',N'Mình muốn ngắm phố cổ và thử làm gốm trong nửa ngày.',@TraKetQua=0;
DECLARE @DeXuat nvarchar(max) = CONCAT(N'{"diem":[{"ngay":1,"thuTu":1,"diSanId":',@HoiAn,
    N',"gio":"08:30"},{"ngay":1,"thuTu":2,"traiNghiemId":',@Gom,N',"gio":"10:30","ghiChu":"Liên hệ xưởng trước chuyến đi"}]}');
DECLARE @Nguon nvarchar(max) = CONCAT(N'[{"loai":"DI_SAN","id":',@HoiAn,
    N'},{"loai":"TRAI_NGHIEM","id":',@Gom,N'},{"loai":"TU_LIEU","id":',@TuLieu,N'}]');
EXEC dbo.usp_LuuTinNhanGemini @Lan,@HoiThoai,'ASSISTANT',
    N'Bạn có thể đi bộ trong phố cổ rồi dành một giờ thử nặn gốm tại Thanh Hà.',
    @MaMoHinh='gemini-demo',@TokenVao=120,@TokenRa=55,@DeXuatHanhTrinhJson=@DeXuat,
    @NguonJson=@Nguon,@TinNhanAIId=@TinNhan OUTPUT,@TraKetQua=0;
EXEC dbo.usp_TaoHanhTrinh @Lan,N'Một ngày ở Hội An',@HomNay,@NgayMai,
    @TinNhanAIId=@TinNhan,@HanhTrinhId=@HanhTrinh OUTPUT,@TraKetQua=0;
UPDATE dbo.HanhTrinh SET AnhBiaPath=N'demo/hanh-trinh/hoi-an/bia.jpg',TrangThai='DANG_DI' WHERE HanhTrinhId=@HanhTrinh;
EXEC dbo.usp_CheckInDiSan @Lan,@HoiAn,'00000000-0000-4000-8000-000000000101',15.877002,108.326301,12,
    @AnhPath=N'demo/check-in/hoi-an/pho.jpg',@CheckInId=@CheckIn OUTPUT,@TraKetQua=0;
EXEC dbo.usp_CheckInDiSan @Lan,@ThanhHa,'00000000-0000-4000-8000-000000000102',15.878641,108.294673,16,
    @VideoPath=N'demo/check-in/thanh-ha/ban-xoay.mp4',@CheckInId=@CheckInThanhHa OUTPUT,@TraKetQua=0;
EXEC dbo.usp_TaoNhatKy @Lan,@HanhTrinh,N'Đi bộ qua khu phố cổ',N'Mình dừng lại ngắm mái ngói và chụp vài góc phố.',
    @HomNay,@CheckIn,@AnhBiaPath=N'demo/nhat-ky/hoi-an/bia.jpg',@NhatKyId=@NhatKy OUTPUT,@TraKetQua=0;
INSERT dbo.MediaNhatKy(NhatKyId,AnhPath,VideoPath,ChuThich,ThuTu)
VALUES (@NhatKy,N'demo/nhat-ky/hoi-an/mai-ngoi.jpg',NULL,N'Mái ngói',1),
       (@NhatKy,NULL,N'demo/nhat-ky/hoi-an/duong-pho.mp4',N'Một đoạn đường đi bộ',2);
UPDATE dbo.NhatKyHanhTrinh SET CongKhai=1 WHERE NhatKyId=@NhatKy;

INSERT dbo.HuyHieu(Ma,Ten,MoTa,AnhPath)
VALUES ('HIEU_VE_GOM',N'Hiểu về gốm',N'Hoàn thành câu hỏi khám phá nghề gốm.',N'demo/huy-hieu/hieu-ve-gom.png'),
       ('DAU_CHAN_DAU',N'Dấu chân đầu tiên',N'Check-in đủ hai di sản trong nhiệm vụ.',N'demo/huy-hieu/dau-chan-dau.png');
INSERT dbo.NhiemVu(HuyHieuId,DiSanId,Loai,Ten,MoTa,SoDiSanCanDat,BatDauUtc,CongKhai)
SELECT HuyHieuId,@ThanhHa,'QUIZ',N'Tìm hiểu nghề gốm',N'Đọc tư liệu và trả lời các câu hỏi.',NULL,@QuaKhu,1
FROM dbo.HuyHieu WHERE Ma='HIEU_VE_GOM'
UNION ALL
SELECT HuyHieuId,NULL,'DEM_DI_SAN',N'Ghé hai di sản',N'Check-in tại hai di sản khác nhau.',2,@QuaKhu,1
FROM dbo.HuyHieu WHERE Ma='DAU_CHAN_DAU';
DECLARE @Quiz int = (SELECT NhiemVuId FROM dbo.NhiemVu WHERE Loai='QUIZ');
DECLARE @Dem int = (SELECT NhiemVuId FROM dbo.NhiemVu WHERE Loai='DEM_DI_SAN');
DECLARE @Cau int, @Dung int, @Sai int;
INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
VALUES (@Quiz,1,N'Dụng cụ nào hỗ trợ tạo hình khi thực hành nặn gốm?',N'Bàn xoay giúp tạo hình sản phẩm.');
SET @Cau = CONVERT(int,SCOPE_IDENTITY());
INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung)
VALUES (@Cau,1,N'Bàn xoay',1),(@Cau,2,N'La bàn',0),(@Cau,3,N'Ống nhòm',0);
SELECT @Dung=LuaChonId FROM dbo.LuaChonCauHoi WHERE CauHoiId=@Cau AND Dung=1;
SELECT @Sai=LuaChonId FROM dbo.LuaChonCauHoi WHERE CauHoiId=@Cau AND ThuTu=2;
EXEC dbo.usp_NopCauTraLoi @Lan,@Cau,@Sai,@TraKetQua=0;
EXEC dbo.usp_NopCauTraLoi @Lan,@Cau,@Dung,@TraKetQua=0;
EXEC dbo.usp_CapNhatTienDoNhiemVu @Lan,@Dem,@TraKetQua=0;

INSERT dbo.DanhGiaDiSan(NguoiDungId,DiSanId,CheckInId,SoSao,NoiDung,AnhPath)
VALUES (@Lan,@HoiAn,@CheckIn,5,N'Thích đi bộ và quan sát các dãy nhà.',N'demo/danh-gia/hoi-an/anh.jpg');
INSERT dbo.DanhGiaTraiNghiem(NguoiDungId,TraiNghiemId,YeuCauId,SoSao,NoiDung)
VALUES (@Lan,@Gom,@DaThamGia,4,N'Phần thực hành dễ theo dõi.');

INSERT dbo.MoHinhGoiY(Ma,PhienBan,ThuatToan,ChiSoJson)
VALUES ('CONTENT_BASELINE','1.0',N'Xếp hạng theo chủ đề và tương tác',N'{"trangThai":"demo"}');
DECLARE @MoHinh int = CONVERT(int,SCOPE_IDENTITY());
DECLARE @LuotGoiY bigint;
DECLARE @DanhSach nvarchar(max) = CONCAT(N'[{"diSanId":',@ThanhHa,
    N',"diem":0.92,"lyDo":"Quan tâm gốm và thủ công"},{"traiNghiemId":',@Gom,N',"diem":0.86}]');
EXEC dbo.usp_LuuLuotGoiY @Lan,@MoHinh,'TRANG_CHU',@DanhSach,@LuotGoiYId=@LuotGoiY OUTPUT,@TraKetQua=0;
EXEC dbo.usp_GhiSuKienTuongTac @Lan,'00000000-0000-4000-8000-000000000201','NHAN_GOI_Y',
    @DiSanId=@ThanhHa,@LuotGoiYId=@LuotGoiY,@ThuHang=1;
EXEC dbo.usp_GhiSuKienTuongTac @Lan,'00000000-0000-4000-8000-000000000202','AN_GOI_Y',
    @TraiNghiemId=@Gom,@LuotGoiYId=@LuotGoiY,@ThuHang=2;
EXEC dbo.usp_GhiSuKienTuongTac @Khoi,'00000000-0000-4000-8000-000000000203','XEM',@DiSanId=@MySon;

-- Marker chỉ được tạo khi kiểm tra cài đặt thành công.
GO
-- 17. Xác nhận bản cài. Giờ tham quan chỉ bổ sung khi có dữ liệu xác thực.

IF XACT_STATE() <> 1
BEGIN
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW 51190, N'Khởi tạo thất bại. Xem lỗi xuất hiện trước thông báo này.', 1;
END;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND is_ms_shipped = 0) <> 41
   OR (SELECT COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo') AND name LIKE 'usp[_]%') <> 16
   OR (SELECT COUNT(*) FROM sys.views WHERE schema_id = SCHEMA_ID('dbo') AND name LIKE 'vw[_]%') <> 6
   OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id = SCHEMA_ID('dbo') AND type IN ('FN','IF') AND name LIKE 'fn[_]%') <> 2
   OR (SELECT COUNT(*) FROM sys.triggers WHERE parent_class = 1 AND name LIKE 'tr[_]%') <> 9
   OR EXISTS (
        SELECT 1 FROM sys.tables t
        JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
        WHERE t.schema_id = SCHEMA_ID('dbo') AND t.is_ms_shipped = 0 AND t.name <> 'GioThamQuan'
        GROUP BY t.object_id HAVING SUM(p.rows) = 0
   )
BEGIN
    ROLLBACK;
    THROW 51191, N'Khởi tạo chưa đủ đối tượng hoặc dữ liệu mẫu. Toàn bộ transaction đã hoàn tác.', 1;
END;
CREATE TABLE dbo.PhienBanCSDL (
    PhienBan varchar(20) NOT NULL PRIMARY KEY,
    CaiDatLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    GhiChu nvarchar(250) NOT NULL
);
INSERT dbo.PhienBanCSDL(PhienBan,GhiChu) VALUES ('1.0.0',N'Bản khởi tạo Dấu Chân Di Sản, kèm dữ liệu demo.');
COMMIT;
PRINT N'Đã khởi tạo DauChanDiSan thành công.';
GO
SET NOEXEC OFF;
GO

/*
    CÁCH THỬ SAU KHI CÀI ĐẶT
    SELECT * FROM dbo.vw_DiSanCongKhai;
    SELECT * FROM dbo.fn_DiSanGan(15.877002,108.326301,5000) ORDER BY KhoangCachM;
    SELECT * FROM dbo.vw_LichTraiNghiem WHERE TrangThai = 'MO' AND BatDauUtc > SYSUTCDATETIME();
    EXEC dbo.usp_LayNguCanhGemini @TuKhoa = N'gốm', @GioiHan = 12;
    SELECT * FROM dbo.vw_DuLieuHuanLuyen;
    DECLARE @Lan int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-lan');
    EXEC dbo.usp_GoiYTheoSoThich @NguoiDungId=@Lan;
    SELECT * FROM dbo.vw_NhatKyCongKhai;

    TÍCH HỢP
    - Khi mở màn nhiệm vụ hoặc sau khi check-in thành công, API gọi
      usp_CapNhatTienDoNhiemVu cho từng nhiệm vụ DEM_DI_SAN đang áp dụng.
      Quiz cập nhật và trao huy hiệu ngay trong usp_NopCauTraLoi.
    - Khi gọi Gemini, API lấy ngữ cảnh từ usp_LayNguCanhGemini, gửi dữ liệu cần thiết,
      kiểm tra ID được trả về có trong ngữ cảnh của lượt đó, rồi lưu bằng
      usp_LuuTinNhanGemini. Tạo lịch trình bằng usp_TaoHanhTrinh sau khi người dùng chọn lưu.
    - Gọi usp_GoiYTheoSoThich để demo ngay. Pipeline ML có thể đọc vw_DuLieuHuanLuyen,
      dùng DiSanChuDe/SoThichNguoiDung làm đặc trưng, huấn luyện bên ngoài SQL,
      đăng ký phiên bản vào MoHinhGoiY và lưu đầu ra bằng usp_LuuLuotGoiY.
    - Khởi tạo quản trị viên thật sau lần đăng nhập/đồng bộ đầu tiên. Chạy bằng
      tài khoản quản trị SQL và thay bằng UID thật của dự án Firebase:
      INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro)
      SELECT u.NguoiDungId,'QUAN_TRI' FROM dbo.NguoiDung u
      WHERE u.FirebaseUid=N'<UID_FIREBASE_THAT>'
        AND NOT EXISTS (SELECT 1 FROM dbo.NguoiDungVaiTro r
                        WHERE r.NguoiDungId=u.NguoiDungId AND r.MaVaiTro='QUAN_TRI');
    - Lấy FirebaseUid từ token đã xác minh. Khi bật/tắt cá nhân hóa, cập nhật
      NguoiDung.ChoPhepCaNhanHoa qua API. View huấn luyện chỉ trả người đang bật tùy chọn này.
    - API dùng mô hình dữ liệu SQL cho CRUD còn lại, kiểm tra quyền người sở hữu,
      vai trò, và quyền truy cập tệp trước khi hiển thị. Với EF Core/SQL Server,
      cấu hình các entity có trigger để EF dùng cơ chế ghi tương thích.
    - Nếu nhập dữ liệu thật, bỏ phần mẫu bằng một migration riêng hoặc thay dữ liệu mẫu
      trong một bản sao script trước lúc cài và sửa phần kiểm tra bảng có dữ liệu.
*/

-- Cập nhật cho cả bản cài mới và cơ sở dữ liệu hiện có.
IF OBJECT_ID(N'dbo.PhienBanCSDL',N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có schema gốc. Hãy kiểm tra phần khởi tạo.',16,1);
    SET NOEXEC ON;
END;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- 02_DongGopDiSan
USE [DauChanDiSan];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @LockResult int;
EXEC @LockResult = sys.sp_getapplock @Resource = 'DauChanDiSan:02_DongGopDiSan',
    @LockMode = 'Exclusive', @LockOwner = 'Transaction';
IF @LockResult < 0
    THROW 50001, N'Không khóa được bản cập nhật đóng góp di sản.', 1;

IF OBJECT_ID(N'dbo.DongGopDiSan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DongGopDiSan (
        DongGopId bigint IDENTITY CONSTRAINT PK_DongGopDiSan PRIMARY KEY,
        NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
        MaYeuCau uniqueidentifier NOT NULL,
        Loai varchar(16) NOT NULL,
        DiSanId int NULL REFERENCES dbo.DiSan(DiSanId),
        TinhThanhId int NULL REFERENCES dbo.TinhThanh(TinhThanhId),
        LoaiDiSanId int NULL REFERENCES dbo.LoaiDiSan(LoaiDiSanId),
        TieuDe nvarchar(200) NOT NULL,
        NoiDung nvarchar(1600) NOT NULL,
        DiaChi nvarchar(250) NULL,
        ViDo decimal(9,6) NULL,
        KinhDo decimal(9,6) NULL,
        AnhPath nvarchar(450) NULL,
        UrlNguon nvarchar(1000) NULL,
        TrangThai varchar(16) NOT NULL,
        LyDo nvarchar(500) NULL,
        NguoiDuyetId int NULL REFERENCES dbo.NguoiDung(NguoiDungId),
        DuyetLucUtc datetime2(3) NULL,
        DiSanCongKhaiId int NULL REFERENCES dbo.DiSan(DiSanId),
        TuLieuId int NULL REFERENCES dbo.TuLieuDiSan(TuLieuId),
        TaoLucUtc datetime2(3) NOT NULL,
        CapNhatLucUtc datetime2(3) NOT NULL,
        PhienBan rowversion NOT NULL,
        CONSTRAINT UQ_DongGopDiSan_YeuCau UNIQUE (NguoiDungId, MaYeuCau),
        CONSTRAINT CK_DongGopDiSan_NoiDung CHECK (LEN(LTRIM(TieuDe)) > 0 AND LEN(LTRIM(NoiDung)) > 0),
        CONSTRAINT CK_DongGopDiSan_DiaDiem CHECK (
            (Loai = 'DI_SAN_MOI' AND DiSanId IS NULL AND TinhThanhId IS NOT NULL
              AND LoaiDiSanId IS NOT NULL AND DiaChi IS NOT NULL AND LEN(LTRIM(DiaChi)) > 0
              AND ViDo IS NOT NULL AND ViDo BETWEEN -90 AND 90
              AND KinhDo IS NOT NULL AND KinhDo BETWEEN -180 AND 180)
            OR
            (Loai = 'BO_SUNG' AND DiSanId IS NOT NULL AND TinhThanhId IS NULL
              AND LoaiDiSanId IS NULL AND DiaChi IS NULL AND ViDo IS NULL AND KinhDo IS NULL)
        ),
        CONSTRAINT CK_DongGopDiSan_Duyet CHECK (
            (TrangThai IN ('CHO_DUYET','DA_RUT') AND NguoiDuyetId IS NULL AND DuyetLucUtc IS NULL
              AND DiSanCongKhaiId IS NULL AND TuLieuId IS NULL AND LyDo IS NULL)
            OR
            (TrangThai = 'TU_CHOI' AND NguoiDuyetId IS NOT NULL AND DuyetLucUtc IS NOT NULL
              AND DiSanCongKhaiId IS NULL AND TuLieuId IS NULL AND LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)
            OR
            (TrangThai = 'DA_DUYET' AND NguoiDuyetId IS NOT NULL AND DuyetLucUtc IS NOT NULL
              AND DiSanCongKhaiId IS NOT NULL AND LyDo IS NULL
              AND (Loai = 'DI_SAN_MOI' OR (DiSanId = DiSanCongKhaiId AND TuLieuId IS NOT NULL)))
        )
    );
    CREATE INDEX IX_DongGopDiSan_TrangThai ON dbo.DongGopDiSan (TrangThai, DongGopId DESC);
    CREATE INDEX IX_DongGopDiSan_NguoiDung ON dbo.DongGopDiSan (NguoiDungId, DongGopId DESC);
END;

COMMIT;
GO


-- 03_KiemDuyetVanHoa
USE DauChanDiSan;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WITH (UPDLOCK,HOLDLOCK) WHERE MaVaiTro='KIEM_DUYET_VAN_HOA')
    INSERT dbo.VaiTro (MaVaiTro, Ten)
    VALUES ('KIEM_DUYET_VAN_HOA',N'Cán bộ kiểm duyệt văn hóa');

-- Chỉ tạo tài khoản dùng thử khi bộ dữ liệu demo của dự án đang có.
IF EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-admin')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE FirebaseUid=N'demo-van-hoa')
        INSERT dbo.NguoiDung(FirebaseUid,HoTen,Email,HoatDong)
        VALUES (N'demo-van-hoa',N'Cán bộ kiểm duyệt demo',N'demo-van-hoa@example.test',1);

    DECLARE @DemoId int = (SELECT NguoiDungId FROM dbo.NguoiDung WHERE FirebaseUid=N'demo-van-hoa');
    INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro)
    SELECT @DemoId,role.MaVaiTro
    FROM (VALUES ('THANH_VIEN'),('KIEM_DUYET_VAN_HOA')) role(MaVaiTro)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NguoiDungVaiTro x WITH (UPDLOCK,HOLDLOCK)
                      WHERE x.NguoiDungId=@DemoId AND x.MaVaiTro=role.MaVaiTro);
END;

COMMIT;
GO

-- Gắn đường dẫn ảnh có sẵn trong frontend cho các bản ghi demo tương ứng.
-- Khi publish cùng domain, frontend/dist/images được chép vào wwwroot/images.
UPDATE place
SET AnhBiaPath=asset.Anh
FROM dbo.DiSan place
JOIN (VALUES
    ('pho-co-hoi-an', N'/images/di-san/hoi-an.webp'),
    ('thanh-dia-my-son', N'/images/di-san/my-son.webp'),
    ('dai-noi-hue', N'/images/di-san/hue.webp')
) asset(Slug,Anh) ON asset.Slug=place.Slug
WHERE place.AnhBiaPath LIKE N'demo/di-san/%';
GO


-- 04_NangCap
USE DauChanDiSan;
GO
SET XACT_ABORT ON;
BEGIN TRAN;
IF OBJECT_ID('dbo.MaDangKyEmail','U') IS NULL
    CREATE TABLE dbo.MaDangKyEmail(
        Email nvarchar(254) PRIMARY KEY, MaYeuCau uniqueidentifier NOT NULL, MaBam binary(32) NOT NULL,
        GuiLucUtc datetime2(3) NOT NULL, HetHanUtc datetime2(3) NOT NULL,
        SoLanThu tinyint NOT NULL CHECK(SoLanThu BETWEEN 0 AND 5), DaDung bit NOT NULL);
IF OBJECT_ID('dbo.BaiKiemTraDiSan','U') IS NULL
    CREATE TABLE dbo.BaiKiemTraDiSan(
        DiSanId int PRIMARY KEY REFERENCES dbo.DiSan(DiSanId), NhiemVuId int NOT NULL UNIQUE REFERENCES dbo.NhiemVu(NhiemVuId));
IF OBJECT_ID('dbo.DiemDiSan','U') IS NULL
    CREATE TABLE dbo.DiemDiSan(
        NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId), DiSanId int NOT NULL REFERENCES dbo.DiSan(DiSanId),
        Diem int NOT NULL CHECK(Diem>0), NhanLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(), PRIMARY KEY(NguoiDungId,DiSanId));
IF OBJECT_ID('dbo.DonDangKyDoiTac','U') IS NULL
BEGIN
    CREATE TABLE dbo.DonDangKyDoiTac(
        DonId int IDENTITY PRIMARY KEY, NguoiDungId int NOT NULL REFERENCES dbo.NguoiDung(NguoiDungId),
        TinhThanhId int NOT NULL REFERENCES dbo.TinhThanh(TinhThanhId), Ten nvarchar(160) NOT NULL,
        Email nvarchar(254) NOT NULL, DienThoai varchar(24) NOT NULL, DiaChi nvarchar(250) NOT NULL, MoTa nvarchar(1000) NOT NULL,
        TrangThai varchar(16) NOT NULL DEFAULT 'CHO_DUYET' CHECK(TrangThai IN ('CHO_DUYET','DA_DUYET','TU_CHOI')),
        LyDo nvarchar(500) NULL, DoiTacId int NULL REFERENCES dbo.DoiTac(DoiTacId),
        NguoiDuyetId int NULL REFERENCES dbo.NguoiDung(NguoiDungId), TaoLucUtc datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        DuyetLucUtc datetime2(3) NULL);
    CREATE UNIQUE INDEX UX_DonDoiTac_DangCho ON dbo.DonDangKyDoiTac(NguoiDungId) WHERE TrangThai='CHO_DUYET';
END;
COMMIT;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TaoMaDangKy @Email nvarchar(254), @MaYeuCau uniqueidentifier, @MaBam binary(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Gui datetime2(3), @Now datetime2(3)=SYSUTCDATETIME();
    BEGIN TRAN;
    SELECT @Gui=GuiLucUtc FROM dbo.MaDangKyEmail WITH (UPDLOCK,HOLDLOCK) WHERE Email=@Email;
    IF @Gui>DATEADD(second,-60,@Now)
    BEGIN
        COMMIT;
        SELECT CAST(0 AS bit) AS DuocGui;
        RETURN;
    END;
    IF @Gui IS NULL
        INSERT dbo.MaDangKyEmail(Email,MaYeuCau,MaBam,GuiLucUtc,HetHanUtc,SoLanThu,DaDung)
        VALUES(@Email,@MaYeuCau,@MaBam,@Now,DATEADD(minute,10,@Now),0,0);
    ELSE
        UPDATE dbo.MaDangKyEmail SET MaYeuCau=@MaYeuCau,MaBam=@MaBam,GuiLucUtc=@Now,
            HetHanUtc=DATEADD(minute,10,@Now),SoLanThu=0,DaDung=0 WHERE Email=@Email;
    COMMIT;
    SELECT CAST(1 AS bit) AS DuocGui;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_XacNhanMaDangKy @Email nvarchar(254), @MaYeuCau uniqueidentifier, @MaBam binary(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Hash binary(32), @Valid bit=0;
    BEGIN TRAN;
    SELECT @Hash=MaBam FROM dbo.MaDangKyEmail WITH (UPDLOCK,HOLDLOCK)
    WHERE Email=@Email AND MaYeuCau=@MaYeuCau AND DaDung=0 AND SoLanThu<5 AND HetHanUtc>SYSUTCDATETIME();
    IF @Hash IS NOT NULL
    BEGIN
        IF @Hash=@MaBam SET @Valid=1;
        UPDATE dbo.MaDangKyEmail SET SoLanThu=SoLanThu+1,DaDung=@Valid WHERE Email=@Email;
    END;
    COMMIT;
    SELECT @Valid AS HopLe;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_DangKyDoiTac @NguoiDungId int, @TinhThanhId int, @Ten nvarchar(160),
    @Email nvarchar(254), @DienThoai varchar(24), @DiaChi nvarchar(250), @MoTa nvarchar(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
    IF NOT EXISTS(SELECT 1 FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK)
        WHERE NguoiDungId=@NguoiDungId AND HoatDong=1 AND EmailDaXacMinh=1)
        THROW 51201,N'Cần tài khoản hoạt động và email đã xác minh.',1;
    IF EXISTS(SELECT 1 FROM dbo.NguoiDungVaiTro WHERE NguoiDungId=@NguoiDungId AND MaVaiTro='DOI_TAC')
       OR EXISTS(SELECT 1 FROM dbo.DonDangKyDoiTac WHERE NguoiDungId=@NguoiDungId AND TrangThai='CHO_DUYET')
        THROW 51202,N'Bạn đã có quyền đối tác hoặc đã có đơn đang chờ.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.TinhThanh WHERE TinhThanhId=@TinhThanhId AND HoatDong=1)
        THROW 51203,N'Địa bàn không hợp lệ.',1;
    INSERT dbo.DonDangKyDoiTac(NguoiDungId,TinhThanhId,Ten,Email,DienThoai,DiaChi,MoTa)
    VALUES(@NguoiDungId,@TinhThanhId,@Ten,@Email,@DienThoai,@DiaChi,@MoTa);
    DECLARE @Id int=CONVERT(int,SCOPE_IDENTITY());
    COMMIT;
    SELECT @Id AS DonId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_DuyetDonDoiTac @DonId int, @NguoiDuyetId int, @Duyet bit, @LyDo nvarchar(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @User int,@Status varchar(16),@Partner int,@Active bit;
    BEGIN TRAN;
    IF NOT EXISTS(SELECT 1 FROM dbo.NguoiDungVaiTro r JOIN dbo.NguoiDung n ON n.NguoiDungId=r.NguoiDungId
        WHERE r.NguoiDungId=@NguoiDuyetId AND r.MaVaiTro='QUAN_TRI' AND n.HoatDong=1)
        THROW 51201,N'Cần quyền quản trị viên để xét duyệt.',1;
    SELECT @User=NguoiDungId FROM dbo.DonDangKyDoiTac WHERE DonId=@DonId;
    IF @User IS NULL THROW 51204,N'Không tìm thấy đơn.',1;
    SELECT @Active=HoatDong FROM dbo.NguoiDung WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId=@User;
    SELECT @Status=TrangThai FROM dbo.DonDangKyDoiTac WITH (UPDLOCK,HOLDLOCK) WHERE DonId=@DonId;
    IF @Status<>'CHO_DUYET'
    BEGIN
        IF (@Status='DA_DUYET' AND @Duyet=1) OR (@Status='TU_CHOI' AND @Duyet=0)
        BEGIN
            COMMIT;
            RETURN;
        END;
        THROW 51202,N'Đơn đã được xử lý.',1;
    END;
    IF @Duyet=1
    BEGIN
        IF @Active=0 THROW 51202,N'Tài khoản người gửi đang bị khóa.',1;
        IF EXISTS(SELECT 1 FROM dbo.DonDangKyDoiTac a JOIN dbo.DoiTac p WITH (UPDLOCK,HOLDLOCK)
            ON p.TinhThanhId=a.TinhThanhId AND p.Ten=a.Ten WHERE a.DonId=@DonId)
            THROW 51202,N'Đơn vị đã tồn tại. Hãy kiểm tra hồ sơ.',1;
        INSERT dbo.DoiTac(TinhThanhId,Ten,MoTa,DienThoai,Email,DiaChi,HoatDong)
        SELECT TinhThanhId,Ten,MoTa,DienThoai,Email,DiaChi,1 FROM dbo.DonDangKyDoiTac WHERE DonId=@DonId;
        SET @Partner=CONVERT(int,SCOPE_IDENTITY());
        INSERT dbo.ThanhVienDoiTac(DoiTacId,NguoiDungId,Quyen,HoatDong) VALUES(@Partner,@User,'QUAN_LY',1);
        INSERT dbo.NguoiDungVaiTro(NguoiDungId,MaVaiTro) SELECT @User,'DOI_TAC'
        WHERE NOT EXISTS(SELECT 1 FROM dbo.NguoiDungVaiTro WITH (UPDLOCK,HOLDLOCK) WHERE NguoiDungId=@User AND MaVaiTro='DOI_TAC');
    END
    ELSE IF NULLIF(TRIM(@LyDo),N'') IS NULL THROW 51203,N'Nhập lý do từ chối.',1;
    UPDATE dbo.DonDangKyDoiTac SET TrangThai=CASE WHEN @Duyet=1 THEN 'DA_DUYET' ELSE 'TU_CHOI' END,
        LyDo=CASE WHEN @Duyet=0 THEN @LyDo END,DoiTacId=@Partner,NguoiDuyetId=@NguoiDuyetId,DuyetLucUtc=SYSUTCDATETIME()
    WHERE DonId=@DonId;
    COMMIT;
END;
GO
CREATE OR ALTER VIEW dbo.vw_XepHangDiSan AS
SELECT DENSE_RANK() OVER(ORDER BY SUM(p.Diem) DESC) AS Hang,n.NguoiDungId,n.HoTen,n.AnhDaiDienPath,
       SUM(p.Diem) AS Diem,COUNT(*) AS SoBai
FROM dbo.DiemDiSan p JOIN dbo.NguoiDung n ON n.NguoiDungId=p.NguoiDungId AND n.HoatDong=1
GROUP BY n.NguoiDungId,n.HoTen,n.AnhDaiDienPath;
GO


SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource='DauChanDiSan:04', @LockMode='Exclusive', @LockOwner='Transaction';
IF @Lock<0 THROW 50001,N'Không khóa được bản cập nhật.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.PhienBanCSDL WHERE PhienBan='1.4.0')
BEGIN
    DECLARE @Reviewer int=(SELECT MIN(NguoiDungId) FROM dbo.NguoiDungVaiTro WHERE MaVaiTro='QUAN_TRI');
    DECLARE @Place int,@Mission int,@Question int;
    DECLARE @Hanoi int=(SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma='HA_NOI');
    DECLARE @Site int=(SELECT LoaiDiSanId FROM dbo.LoaiDiSan WHERE Ma='KHU_DI_TICH');
    IF @Hanoi IS NOT NULL AND @Site IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.DiSan WHERE Slug='hoang-thanh-thang-long')
        INSERT dbo.DiSan(TinhThanhId,LoaiDiSanId,Slug,Ten,TomTat,DiaChi,ViDo,KinhDo,BanKinhCheckInM,SaiSoGpsToiDaM,ThoiLuongPhut,AnhBiaPath,TrangThai)
        VALUES(@Hanoi,@Site,'hoang-thanh-thang-long',N'Hoàng thành Thăng Long',N'Các lớp khảo cổ giữa Hà Nội ghi lại lịch sử của một trung tâm quyền lực.',
               N'19C Hoàng Diệu, Hà Nội',21.03752,105.84044,180,60,90,N'/images/di-san/thang-long.webp','CONG_KHAI');
    UPDATE dbo.DiSan SET VideoPath=NULL WHERE VideoPath LIKE N'demo/di-san/%';
    UPDATE dbo.NguoiDung SET AnhDaiDienPath=NULL WHERE FirebaseUid LIKE N'demo-%' AND AnhDaiDienPath LIKE N'demo/%';
    UPDATE dbo.NguoiDung SET EmailDaXacMinh=1 WHERE FirebaseUid=N'demo-lan' AND Email=N'lan@example.test';
    UPDATE dbo.TuLieuDiSan SET CongKhai=0 WHERE Nguon=N'Biên tập minh họa' AND UrlNguon IS NULL
        AND TieuDe IN (N'Gợi ý quan sát nhà phố',N'Các bước tạo sản phẩm gốm');
    DELETE r FROM dbo.DanhGiaDiSan r JOIN dbo.NguoiDung u ON u.NguoiDungId=r.NguoiDungId
    WHERE u.FirebaseUid=N'demo-lan' AND r.AnhPath=N'demo/danh-gia/hoi-an/anh.jpg' AND r.NoiDung=N'Thích đi bộ và quan sát các dãy nhà.';
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='pho-co-hoi-an';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Thương cảng bên dòng Thu Bồn
Từ thế kỷ XV đến XIX, **Hội An** là một thương cảng quốc tế. Sông Thu Bồn nối đô thị với vùng nội địa và cửa biển. Quá trình giao thương để lại dấu vết trong bố cục đường phố, tín ngưỡng và kiến trúc của đô thị.

## Nhà phố và Chùa Cầu
Nhiều nhà phố có gian buôn bán quay ra đường và sân trong lấy sáng. Hội quán gắn với sinh hoạt của các cộng đồng cư dân. **Chùa Cầu** gợi nhắc mối giao lưu của thương cảng với người Nhật. UNESCO ghi danh Phố cổ Hội An năm **1999**.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://whc.unesco.org/en/list/948')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Phố cổ Hội An',N'Từ thế kỷ XV đến XIX, Hội An là một thương cảng quốc tế. Sông Thu Bồn nối đô thị với vùng nội địa và cửa biển. Quá trình giao thương để lại dấu vết trong bố cục đường phố, tín ngưỡng và kiến trúc của đô thị.',N'UNESCO',N'https://whc.unesco.org/en/list/948',@Reviewer,SYSUTCDATETIME(),1);
        IF NOT EXISTS(SELECT 1 FROM dbo.MediaDiSan WHERE DiSanId=@Place AND AnhPath=N'/images/di-san/cau-chua.webp')
            INSERT dbo.MediaDiSan(DiSanId,AnhPath,ChuThich,ThuTu)
            SELECT @Place,N'/images/di-san/cau-chua.webp',N'Chùa Cầu vào buổi tối',COALESCE(MAX(ThuTu),0)+1 FROM dbo.MediaDiSan WHERE DiSanId=@Place;
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Phố cổ Hội An',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Hội An từng phát triển với vai trò gì?',N'Đáp án: Thương cảng quốc tế.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Thương cảng quốc tế',1), (@Question,2,N'Kinh đô triều Nguyễn',0), (@Question,3,N'Trung tâm khai mỏ',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'UNESCO ghi danh Hội An năm nào?',N'Đáp án: 1999.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'1993',0), (@Question,2,N'1999',1), (@Question,3,N'2010',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Chùa Cầu gợi nhắc cộng đồng nào?',N'Đáp án: Người Nhật.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Người Nhật',1), (@Question,2,N'Người Khmer',0), (@Question,3,N'Người Hà Lan',0);
        END;
    END;
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='thanh-dia-my-son';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Thung lũng của Chăm Pa
Mỹ Sơn nằm trong thung lũng được núi bao quanh. Nơi đây từng là trung tâm tôn giáo và chính trị của **Chăm Pa**. Tàn tích cho thấy sự phát triển của kiến trúc đền tháp trong nhiều thế kỷ, từ khoảng thế kỷ IV đến XIII.

## Đọc dấu vết trên thân tháp
Nhiều tháp được xây bằng **gạch**, kết hợp chi tiết sa thạch như trụ cửa và phù điêu. Hãy quan sát chân tháp, bố cục cửa và tầng mái khi đi giữa các cụm để nhận ra khác biệt qua từng giai đoạn.

## Tín ngưỡng và bảo tồn
Nhiều công trình gắn với Ấn Độ giáo và việc thờ **thần Shiva**. Chiến tranh làm hư hại một phần quần thể. UNESCO ghi danh Mỹ Sơn năm **1999**. Khảo cổ và tu bổ tiếp tục giúp làm rõ những phần đã mất.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://whc.unesco.org/en/list/949/')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Thánh địa Mỹ Sơn',N'Mỹ Sơn nằm trong thung lũng được núi bao quanh. Nơi đây từng là trung tâm tôn giáo và chính trị của Chăm Pa. Tàn tích cho thấy sự phát triển của kiến trúc đền tháp trong nhiều thế kỷ, từ khoảng thế kỷ IV đến XIII.',N'UNESCO',N'https://whc.unesco.org/en/list/949/',@Reviewer,SYSUTCDATETIME(),1);
        UPDATE dbo.DiSan SET VideoPath=N'/images/di-san/my-son-motion.mp4' WHERE DiSanId=@Place AND VideoPath IS NULL;
        IF NOT EXISTS(SELECT 1 FROM dbo.MediaDiSan WHERE DiSanId=@Place AND AnhPath=N'/images/di-san/my-son-bcd.webp')
            INSERT dbo.MediaDiSan(DiSanId,AnhPath,ChuThich,ThuTu)
            SELECT @Place,N'/images/di-san/my-son-bcd.webp',N'Cụm B–C–D ở Mỹ Sơn · Ms Sarah Welch',COALESCE(MAX(ThuTu),0)+1 FROM dbo.MediaDiSan WHERE DiSanId=@Place;
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Thánh địa Mỹ Sơn',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Mỹ Sơn gắn với vương quốc nào?',N'Đáp án: Chăm Pa.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Chăm Pa',1), (@Question,2,N'Đại Cồ Việt',0), (@Question,3,N'Chân Lạp',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'Nhiều tháp dùng vật liệu chính nào?',N'Đáp án: Gạch.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Đá granit',0), (@Question,2,N'Gỗ lim',0), (@Question,3,N'Gạch',1);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Mỹ Sơn được ghi danh năm nào?',N'Đáp án: 1999.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'1999',1), (@Question,2,N'1993',0), (@Question,3,N'2010',0);
        END;
    END;
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='dai-noi-hue';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Kinh đô bên sông Hương
Huế trở thành kinh đô năm **1802** dưới triều Nguyễn. Sông Hương và các yếu tố địa hình góp phần định hình không gian kinh đô. Kinh thành, Hoàng thành và Tử Cấm Thành là các lớp không gian có chức năng khác nhau.

## Từ Ngọ Môn vào cung đình
**Ngọ Môn** là cửa chính phía nam Hoàng thành. Trục dẫn từ cổng tới điện Thái Hòa cho thấy cách tổ chức không gian nghi lễ. UNESCO ghi danh **Quần thể di tích cố đô Huế** năm **1993**. Đại Nội là một phần của quần thể.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://whc.unesco.org/en/list/678')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Đại Nội Huế',N'Huế trở thành kinh đô năm 1802 dưới triều Nguyễn. Sông Hương và các yếu tố địa hình góp phần định hình không gian kinh đô. Kinh thành, Hoàng thành và Tử Cấm Thành là các lớp không gian có chức năng khác nhau.',N'UNESCO',N'https://whc.unesco.org/en/list/678',@Reviewer,SYSUTCDATETIME(),1);
        IF NOT EXISTS(SELECT 1 FROM dbo.MediaDiSan WHERE DiSanId=@Place AND AnhPath=N'/images/di-san/ngo-mon.webp')
            INSERT dbo.MediaDiSan(DiSanId,AnhPath,ChuThich,ThuTu)
            SELECT @Place,N'/images/di-san/ngo-mon.webp',N'Ngọ Môn nhìn từ quảng trường · Lưu Ly',COALESCE(MAX(ThuTu),0)+1 FROM dbo.MediaDiSan WHERE DiSanId=@Place;
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Đại Nội Huế',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Huế trở thành kinh đô năm nào?',N'Đáp án: 1802.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'1945',0), (@Question,2,N'1802',1), (@Question,3,N'1993',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'Cổng chính phía nam của Hoàng thành tên gì?',N'Đáp án: Ngọ Môn.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Đoan Môn',0), (@Question,2,N'Hiển Nhơn',0), (@Question,3,N'Ngọ Môn',1);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Quần thể cố đô Huế được ghi danh năm nào?',N'Đáp án: 1993.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'1993',1), (@Question,2,N'1999',0), (@Question,3,N'2010',0);
        END;
    END;
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='hoang-thanh-thang-long';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Kinh đô Đại Việt
Hoàng thành Thăng Long được xây dựng dưới **triều Lý vào thế kỷ XI** trên nền một thành lũy có từ trước. Khu vực trung tâm tiếp tục là nơi tập trung quyền lực qua nhiều giai đoạn lịch sử.

## Đoan Môn và các lớp khảo cổ
Từ **Đoan Môn** có thể tìm hiểu về các cổng và không gian dẫn vào cung thành. Nền móng trong khu khảo cổ **18 Hoàng Diệu** cho thấy nhiều lớp kiến trúc được xây dựng và thay đổi qua thời gian. Khu trung tâm được UNESCO ghi danh năm **2010**.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://whc.unesco.org/en/list/1328')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Hoàng thành Thăng Long',N'Hoàng thành Thăng Long được xây dựng dưới triều Lý vào thế kỷ XI trên nền một thành lũy có từ trước. Khu vực trung tâm tiếp tục là nơi tập trung quyền lực qua nhiều giai đoạn lịch sử.',N'UNESCO',N'https://whc.unesco.org/en/list/1328',@Reviewer,SYSUTCDATETIME(),1);
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Hoàng thành Thăng Long',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Hoàng thành được xây dựng dưới triều nào vào thế kỷ XI?',N'Đáp án: Lý.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Trần',0), (@Question,2,N'Lý',1), (@Question,3,N'Nguyễn',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'Địa chỉ khu khảo cổ được nhắc đến?',N'Đáp án: 18 Hoàng Diệu.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'18 Hoàng Diệu',1), (@Question,2,N'18 Trần Phú',0), (@Question,3,N'18 Lê Lợi',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Hoàng thành được ghi danh năm nào?',N'Đáp án: 2010.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'1993',0), (@Question,2,N'2010',1), (@Question,3,N'1999',0);
        END;
    END;
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='lang-gom-thanh-ha';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Nghề gốm và đời sống hằng ngày
Làng nằm ở **phía tây phố cổ Hội An**. Những người thợ làm đồ dùng bằng đất nung cho sinh hoạt. Các sản phẩm gia dụng giúp hình dung nghề thủ công gắn với đời sống của những làng quanh thương cảng.

## Câu chuyện từ bàn xoay
Hãy quan sát cách nghệ nhân phối hợp đôi tay và **bàn xoay** khi tạo hình. Từ chọn đất, nhào, phơi đến **nung**, mỗi công đoạn đều ảnh hưởng tới thành phẩm. Lễ tưởng nhớ tổ nghề góp phần kết nối cộng đồng làng.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://hoianheritage.danang.gov.vn/vi/trao-doi-chuyen-nganh/chuyen-de-nghien-cuu-trao-doi/lang-nghe-gom-thanh-ha-hoi-an-net-dac-trung-tu-mot-lang-nghe-gom-truyen-thong-o-mien-trung-996.html')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Làng gốm Thanh Hà',N'Làng nằm ở phía tây phố cổ Hội An. Những người thợ làm đồ dùng bằng đất nung cho sinh hoạt. Các sản phẩm gia dụng giúp hình dung nghề thủ công gắn với đời sống của những làng quanh thương cảng.',N'Trung tâm Quản lý Bảo tồn Di sản Văn hóa Hội An',N'https://hoianheritage.danang.gov.vn/vi/trao-doi-chuyen-nganh/chuyen-de-nghien-cuu-trao-doi/lang-nghe-gom-thanh-ha-hoi-an-net-dac-trung-tu-mot-lang-nghe-gom-truyen-thong-o-mien-trung-996.html',@Reviewer,SYSUTCDATETIME(),1);
        UPDATE dbo.DiSan SET AnhBiaPath=N'/images/di-san/thanh-ha.webp' WHERE DiSanId=@Place AND AnhBiaPath LIKE N'demo/%';
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Làng gốm Thanh Hà',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Thanh Hà nằm hướng nào so với phố cổ?',N'Đáp án: Phía tây.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Phía tây',1), (@Question,2,N'Phía đông',0), (@Question,3,N'Phía bắc',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'Dụng cụ tạo hình gốm được nhắc đến?',N'Đáp án: Bàn xoay.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Khung cửi',0), (@Question,2,N'Bàn xoay',1), (@Question,3,N'Đe sắt',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Công đoạn làm đất đã tạo hình thành gốm?',N'Đáp án: Nung.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Nung',1), (@Question,2,N'Nhuộm',0), (@Question,3,N'Đan',0);
        END;
    END;
    SET @Place=NULL;
    SELECT @Place=DiSanId FROM dbo.DiSan WHERE Slug='lang-gom-bat-trang';
    IF @Place IS NOT NULL
    BEGIN
        UPDATE dbo.DiSan SET NoiDung=N'## Làng nghề bên sông Hồng
Bát Tràng nằm ở **bờ tả ngạn sông Hồng**. Một món gốm trải qua nhiều công đoạn như tạo dáng, trang trí, phủ men và nung. Hình dáng, hoa văn và màu men cho biết công dụng của món đồ.

## Lễ hội và cộng đồng
**Lễ hội làng Bát Tràng** được ghi nhận là di sản văn hóa phi vật thể quốc gia. Đặt câu chuyện lễ hội bên cạnh nghề gốm giúp hiểu cách người làng duy trì nghề thủ công và sinh hoạt cộng đồng.' WHERE DiSanId=@Place
          AND (NoiDung IS NULL OR NoiDung IN (N'Đi bộ trong khu phố để quan sát các dãy nhà, mái ngói và mặt tiền.',N'Có thể kết hợp tham quan với một buổi thực hành thủ công.'));
        IF @Reviewer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TuLieuDiSan WHERE DiSanId=@Place AND UrlNguon=N'https://battrang.hanoi.gov.vn/di-tich-lich-su-va-van-hoa/le-hoi-lang-bat-trang-di-san-van-hoa-phi-vat-the-quoc-gia-2757250705165659004.htm')
            INSERT dbo.TuLieuDiSan(DiSanId,TieuDe,TrichYeu,Nguon,UrlNguon,NguoiDuyetId,DuyetLucUtc,CongKhai)
            VALUES(@Place,N'Tư liệu về Làng gốm Bát Tràng',N'Bát Tràng nằm ở bờ tả ngạn sông Hồng. Một món gốm trải qua nhiều công đoạn như tạo dáng, trang trí, phủ men và nung. Hình dáng, hoa văn và màu men cho biết công dụng của món đồ.',N'UBND xã Bát Tràng',N'https://battrang.hanoi.gov.vn/di-tich-lich-su-va-van-hoa/le-hoi-lang-bat-trang-di-san-van-hoa-phi-vat-the-quoc-gia-2757250705165659004.htm',@Reviewer,SYSUTCDATETIME(),1);
        UPDATE dbo.DiSan SET AnhBiaPath=N'/images/di-san/bat-trang.webp' WHERE DiSanId=@Place AND AnhBiaPath LIKE N'demo/%';
        IF NOT EXISTS(SELECT 1 FROM dbo.BaiKiemTraDiSan WHERE DiSanId=@Place)
        BEGIN
            INSERT dbo.NhiemVu(DiSanId,Loai,Ten,MoTa,BatDauUtc,CongKhai)
            VALUES(@Place,'QUIZ',N'Đọc và nhớ · Làng gốm Bát Tràng',N'Bài kiểm tra tự chọn sau khi đọc câu chuyện di sản.','2020-01-01',1);
            SET @Mission=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.BaiKiemTraDiSan(DiSanId,NhiemVuId) VALUES(@Place,@Mission);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,1,N'Bát Tràng nằm bên sông nào?',N'Đáp án: Sông Hồng.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Sông Hương',0), (@Question,2,N'Sông Thu Bồn',0), (@Question,3,N'Sông Hồng',1);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,2,N'Lễ hội làng thuộc loại di sản nào?',N'Đáp án: Phi vật thể.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Phi vật thể',1), (@Question,2,N'Địa chất',0), (@Question,3,N'Thiên nhiên',0);
            INSERT dbo.CauHoiNhiemVu(NhiemVuId,ThuTu,NoiDung,GiaiThich)
            VALUES(@Mission,3,N'Công đoạn đưa men lên bề mặt gốm?',N'Đáp án: Phủ men.');
            SET @Question=CONVERT(int,SCOPE_IDENTITY());
            INSERT dbo.LuaChonCauHoi(CauHoiId,ThuTu,NoiDung,Dung) VALUES (@Question,1,N'Tạo dáng',0), (@Question,2,N'Phủ men',1), (@Question,3,N'Nhào đất',0);
        END;
    END;
    INSERT dbo.PhienBanCSDL(PhienBan,GhiChu) VALUES('1.4.0',N'Tài khoản, học tập, xếp hạng, đối tác và tư liệu.');
END;
COMMIT;
GO
SET NOEXEC OFF;
GO
