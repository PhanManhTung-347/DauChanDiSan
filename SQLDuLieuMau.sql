USE [DauChanDiSan];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

-- Cấu hình tài khoản. Nếu đã có FirebaseUid thì ưu tiên dùng UID.
-- Đặt cả hai thành NULL nếu chỉ muốn bổ sung cho các tài khoản demo có sẵn.
DECLARE @TenHoSo nvarchar(120) = N'Mạnh Tùng Phan';
DECLARE @FirebaseUid nvarchar(128) = NULL;

DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @Today date = CONVERT(date, DATEADD(HOUR, 7, @Now));
DECLARE @HomeKey uniqueidentifier = '10550200-0000-4000-8000-000000000001';
DECLARE @Compass nvarchar(450) = N'/images/theme/la-ban.svg';
DECLARE @TripName nvarchar(160) = N'[DEMO] Hai ngày từ My Home đến Hội An';

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = 'DauChanDiSan:SeedMyHomeHoiAn',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 10000;

    IF @LockResult < 0
        THROW 52000, N'Không lấy được khóa để chạy script demo. Hãy thử lại.', 1;

    ----------------------------------------------------------------------
    -- 1. Tìm tài khoản nhận dữ liệu
    ----------------------------------------------------------------------
    DECLARE @ProfileId int;

    IF @FirebaseUid IS NOT NULL
    BEGIN
        SELECT @ProfileId = NguoiDungId
        FROM dbo.NguoiDung
        WHERE FirebaseUid = @FirebaseUid AND HoatDong = 1;

        IF @ProfileId IS NULL
            THROW 52001, N'UID chưa có hồ sơ hoạt động. Hãy đăng nhập website rồi kiểm tra lại UID.', 1;
    END
    ELSE IF @TenHoSo IS NOT NULL
    BEGIN
        DECLARE @MatchCount int;

        SELECT @MatchCount = COUNT(*), @ProfileId = MIN(NguoiDungId)
        FROM dbo.NguoiDung
        WHERE HoTen = @TenHoSo AND HoatDong = 1;

        IF @MatchCount = 0
            THROW 52002, N'Không tìm thấy hồ sơ theo @TenHoSo. Hãy kiểm tra tên hiển thị hoặc điền FirebaseUid.', 1;

        IF @MatchCount > 1
            THROW 52003, N'Có nhiều hồ sơ trùng tên. Hãy điền FirebaseUid của đúng tài khoản.', 1;
    END;

    DECLARE @Targets TABLE (NguoiDungId int PRIMARY KEY);

    INSERT @Targets(NguoiDungId)
    SELECT NguoiDungId
    FROM dbo.NguoiDung
    WHERE HoatDong = 1 AND (
        NguoiDungId = @ProfileId
        OR FirebaseUid IN (
            N'demo-lan', N'demo-khoi', N'demo-admin',
            N'demo-doi-tac', N'demo-van-hoa'
        )
    );

    DECLARE @Admin int = (
        SELECT NguoiDungId FROM dbo.NguoiDung
        WHERE FirebaseUid = N'demo-admin' AND HoatDong = 1
    );
    DECLARE @Moderator int = (
        SELECT NguoiDungId FROM dbo.NguoiDung
        WHERE FirebaseUid = N'demo-van-hoa' AND HoatDong = 1
    );
    DECLARE @PartnerUser int = (
        SELECT NguoiDungId FROM dbo.NguoiDung
        WHERE FirebaseUid = N'demo-doi-tac' AND HoatDong = 1
    );
    DECLARE @DaNang int = (
        SELECT TinhThanhId FROM dbo.TinhThanh WHERE Ma = 'DA_NANG' AND HoatDong = 1
    );
    DECLARE @Loai int = (
        SELECT LoaiDiSanId FROM dbo.LoaiDiSan WHERE Ma = 'KHU_DI_TICH'
    );
    DECLARE @HoiAn int = (
        SELECT DiSanId FROM dbo.vw_DiSanCongKhai WHERE Slug = 'pho-co-hoi-an'
    );
    DECLARE @ThanhHa int = (
        SELECT DiSanId FROM dbo.vw_DiSanCongKhai WHERE Slug = 'lang-gom-thanh-ha'
    );
    DECLARE @MySon int = (
        SELECT DiSanId FROM dbo.vw_DiSanCongKhai WHERE Slug = 'thanh-dia-my-son'
    );

    IF @Admin IS NULL OR @Moderator IS NULL OR @PartnerUser IS NULL
       OR @DaNang IS NULL OR @Loai IS NULL
       OR @HoiAn IS NULL OR @ThanhHa IS NULL OR @MySon IS NULL
        THROW 52004, N'Thiếu tài khoản demo hoặc địa điểm gốc trong CSDL.', 1;

    ----------------------------------------------------------------------
    -- 2. My Home, tư liệu, huy hiệu và bài kiểm tra
    ----------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.DiSan WHERE Slug = 'my-home-demo-thanh-khe')
        INSERT dbo.DiSan
            (TinhThanhId, LoaiDiSanId, Slug, Ten, TomTat, NoiDung,
             DiaChi, ViDo, KinhDo, BanKinhCheckInM, SaiSoGpsToiDaM,
             ThoiLuongPhut, AnhBiaPath, TrangThai)
        VALUES
            (@DaNang, @Loai, 'my-home-demo-thanh-khe', N'My Home [DEMO]',
             N'Điểm xuất phát giả lập tại Thanh Khê.',
             N'**My Home** là điểm xuất phát mẫu của chuyến Hội An và Mỹ Sơn.',
             N'Thanh Khê, Đà Nẵng · Điểm giả lập',
             16.068904, 108.197188, 150, 60, 15, @Compass, 'CONG_KHAI');

    DECLARE @Home int = (
        SELECT DiSanId FROM dbo.vw_DiSanCongKhai
        WHERE Slug = 'my-home-demo-thanh-khe'
          AND ViDo = 16.068904 AND KinhDo = 108.197188
    );

    IF @Home IS NULL
        THROW 52005, N'My Home đang bị ẩn hoặc đã được đổi tọa độ.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.MediaDiSan WHERE DiSanId = @Home AND ThuTu = 1)
        INSERT dbo.MediaDiSan(DiSanId, AnhPath, ChuThich, ThuTu)
        VALUES (@Home, @Compass, N'Ảnh minh họa điểm xuất phát', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.GioThamQuan WHERE DiSanId = @Home)
        INSERT dbo.GioThamQuan(DiSanId, ThuTrongTuan, GioMo, GioDong)
        SELECT @Home, v.Thu, '07:00', '20:00'
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7)) v(Thu);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.TuLieuDiSan
        WHERE DiSanId = @HoiAn AND TieuDe = N'[DEMO] Gợi ý quan sát phố cổ'
    )
        INSERT dbo.TuLieuDiSan
            (DiSanId, TieuDe, TrichYeu, Nguon, NguoiDuyetId, DuyetLucUtc, CongKhai)
        VALUES
            (@HoiAn, N'[DEMO] Gợi ý quan sát phố cổ',
             N'Ghi chép mẫu về mái ngói, mặt tiền nhà phố và đoạn đi bộ quanh Chùa Cầu.',
             N'Dữ liệu minh họa', @Moderator, @Now, 1);

    DECLARE @Material int = (
        SELECT MIN(TuLieuId) FROM dbo.TuLieuDiSan
        WHERE DiSanId = @HoiAn AND TieuDe = N'[DEMO] Gợi ý quan sát phố cổ'
    );

    IF NOT EXISTS (SELECT 1 FROM dbo.HuyHieu WHERE Ma = 'DEMO_HOI_AN')
        INSERT dbo.HuyHieu(Ma, Ten, MoTa, AnhPath)
        VALUES ('DEMO_HOI_AN', N'Dấu chân Hội An [DEMO]',
                N'Huy hiệu minh họa cho bài kiểm tra mẫu.', @Compass);

    DECLARE @Badge int = (
        SELECT HuyHieuId FROM dbo.HuyHieu WHERE Ma = 'DEMO_HOI_AN'
    );
    DECLARE @Quiz int = (
        SELECT NhiemVuId FROM dbo.BaiKiemTraDiSan WHERE DiSanId = @Home
    );

    IF @Quiz IS NULL
    BEGIN
        INSERT dbo.NhiemVu(HuyHieuId, DiSanId, Loai, Ten, MoTa, BatDauUtc, CongKhai)
        VALUES (@Badge, @Home, 'QUIZ', N'[DEMO] Điểm xuất phát',
                N'Bài kiểm tra mẫu cho chuyến Hội An.', DATEADD(DAY, -30, @Now), 1);
        SET @Quiz = CONVERT(int, SCOPE_IDENTITY());

        INSERT dbo.BaiKiemTraDiSan(DiSanId, NhiemVuId) VALUES (@Home, @Quiz);

        INSERT dbo.CauHoiNhiemVu(NhiemVuId, ThuTu, NoiDung, GiaiThich)
        VALUES (@Quiz, 1, N'Điểm xuất phát mẫu đặt ở đâu?', N'Thanh Khê, Đà Nẵng.');

        DECLARE @QuestionNew int = CONVERT(int, SCOPE_IDENTITY());

        INSERT dbo.LuaChonCauHoi(CauHoiId, ThuTu, NoiDung, Dung)
        VALUES (@QuestionNew, 1, N'Thanh Khê, Đà Nẵng', 1),
               (@QuestionNew, 2, N'Hà Nội', 0),
               (@QuestionNew, 3, N'Huế', 0);
    END;

    DECLARE @Question int, @Answer int;

    SELECT TOP (1) @Question = q.CauHoiId, @Answer = a.LuaChonId
    FROM dbo.CauHoiNhiemVu q
    JOIN dbo.LuaChonCauHoi a ON a.CauHoiId = q.CauHoiId AND a.Dung = 1
    WHERE q.NhiemVuId = @Quiz
    ORDER BY q.ThuTu;

    ----------------------------------------------------------------------
    -- 3. Đối tác, trải nghiệm công khai và trải nghiệm chờ duyệt
    ----------------------------------------------------------------------
    DECLARE @OrgName nvarchar(160) = N'Xưởng Gốm Dấu Chân [DEMO]';

    IF NOT EXISTS (SELECT 1 FROM dbo.DoiTac WHERE TinhThanhId = @DaNang AND Ten = @OrgName)
        INSERT dbo.DoiTac(TinhThanhId, Ten, MoTa, Email, DiaChi)
        VALUES (@DaNang, @OrgName, N'Đơn vị giả lập để thử lịch trải nghiệm.',
                N'xuong-gom@example.test', N'Làng gốm Thanh Hà · Dữ liệu demo');

    DECLARE @Org int = (
        SELECT DoiTacId FROM dbo.DoiTac WHERE TinhThanhId = @DaNang AND Ten = @OrgName
    );

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ThanhVienDoiTac
        WHERE DoiTacId = @Org AND NguoiDungId = @PartnerUser
    )
        INSERT dbo.ThanhVienDoiTac(DoiTacId, NguoiDungId, Quyen)
        VALUES (@Org, @PartnerUser, 'QUAN_LY');

    IF NOT EXISTS (SELECT 1 FROM dbo.TraiNghiem WHERE Slug = 'demo-hoi-an-nan-gom')
        INSERT dbo.TraiNghiem
            (DoiTacId, DiSanId, Slug, Ten, MoTa, DiaDiemTapTrung, ThoiLuongPhut,
             GiaMoiNguoi, AnhBiaPath, TrangThai, NguoiDuyetId, DuyetLucUtc)
        VALUES
            (@Org, @ThanhHa, 'demo-hoi-an-nan-gom', N'[DEMO] Nặn gốm Thanh Hà',
             N'Thử tạo hình một chiếc chén nhỏ.', N'Làng gốm Thanh Hà', 75, 60000,
             N'/images/di-san/thanh-ha.webp', 'CONG_KHAI', @Admin, @Now);

    IF NOT EXISTS (SELECT 1 FROM dbo.TraiNghiem WHERE Slug = 'demo-hoi-an-cho-duyet')
        INSERT dbo.TraiNghiem
            (DoiTacId, DiSanId, Slug, Ten, MoTa, ThoiLuongPhut, TrangThai)
        VALUES
            (@Org, @HoiAn, 'demo-hoi-an-cho-duyet', N'[DEMO] Dạo phố và ký họa',
             N'Trải nghiệm mẫu đang chờ duyệt.', 60, 'CHO_DUYET');

    DECLARE @Experience int = (
        SELECT TraiNghiemId FROM dbo.TraiNghiem WHERE Slug = 'demo-hoi-an-nan-gom'
    );

    IF NOT EXISTS (
        SELECT 1 FROM dbo.MediaTraiNghiem WHERE TraiNghiemId = @Experience AND ThuTu = 1
    )
        INSERT dbo.MediaTraiNghiem(TraiNghiemId, AnhPath, ChuThich, ThuTu)
        VALUES (@Experience, N'/images/di-san/thanh-ha.webp', N'Ảnh minh họa làng gốm', 1);

    DECLARE @PastSlot int, @FutureSlot int;
    DECLARE @PastStart datetime2(3), @PastEnd datetime2(3);

    SELECT TOP (1) @PastSlot = LichTraiNghiemId,
        @PastStart = BatDauUtc, @PastEnd = KetThucUtc
    FROM dbo.LichTraiNghiem
    WHERE TraiNghiemId = @Experience AND TrangThai = 'DONG' AND KetThucUtc < @Now
    ORDER BY BatDauUtc DESC;

    IF @PastSlot IS NULL
    BEGIN
        SET @PastStart = DATEADD(DAY, -4, @Now);
        SET @PastEnd = DATEADD(MINUTE, 75, @PastStart);

        INSERT dbo.LichTraiNghiem(TraiNghiemId, BatDauUtc, KetThucUtc, SucChua, TrangThai)
        VALUES (@Experience, @PastStart, @PastEnd, 100, 'DONG');

        SET @PastSlot = CONVERT(int, SCOPE_IDENTITY());
    END;

    SELECT TOP (1) @FutureSlot = LichTraiNghiemId
    FROM dbo.LichTraiNghiem
    WHERE TraiNghiemId = @Experience AND TrangThai = 'MO'
      AND BatDauUtc > DATEADD(MINUTE, 30, @Now) AND SucChua >= 2
    ORDER BY BatDauUtc;

    IF @FutureSlot IS NULL
    BEGIN
        DECLARE @FutureStart datetime2(3) = DATEADD(DAY, 7, @Now);

        INSERT dbo.LichTraiNghiem(TraiNghiemId, BatDauUtc, KetThucUtc, SucChua, TrangThai)
        VALUES (@Experience, @FutureStart, DATEADD(MINUTE, 75, @FutureStart), 100, 'MO');

        SET @FutureSlot = CONVERT(int, SCOPE_IDENTITY());
    END;

    ----------------------------------------------------------------------
    -- 4. Dữ liệu của chuyến đi
    ----------------------------------------------------------------------
    DECLARE @Stops TABLE (
        DiSanId int PRIMARY KEY,
        Ngay int NOT NULL,
        ThuTu int NOT NULL,
        Anh nvarchar(450) NOT NULL,
        TieuDe nvarchar(160) NOT NULL,
        GhiChu nvarchar(500) NOT NULL,
        NoiDung nvarchar(1000) NOT NULL
    );

    INSERT @Stops VALUES
    (@Home, 1, 1, @Compass,
     N'[DEMO] Xuất phát từ My Home',
     N'Ghi dấu xuất phát ở Thanh Khê.',
     N'Chuẩn bị nước và máy ảnh, xem lại bản đồ rồi lên đường tới Hội An.'),
    (@HoiAn, 1, 2, N'/images/di-san/hoi-an.webp',
     N'[DEMO] Buổi sáng ở phố cổ',
     N'Đi bộ quanh Chùa Cầu và ngắm các dãy nhà phố.',
     N'Mình dừng lại quan sát mái ngói, mặt tiền nhà phố và ghi lại một góc đường yên tĩnh.'),
    (@ThanhHa, 1, 3, N'/images/di-san/thanh-ha.webp',
     N'[DEMO] Một chiếc chén ở Thanh Hà',
     N'Thử nặn gốm và xem cách tạo hình trên bàn xoay.',
     N'Chiếc chén đầu tiên hơi lệch. Mình ghi lại từng bước để lần sau thử làm lại.'),
    (@MySon, 2, 1, N'/images/di-san/my-son.webp',
     N'[DEMO] Ngày thứ hai ở Mỹ Sơn',
     N'Quan sát thân tháp, cửa và các lớp gạch.',
     N'Mình quan sát các cụm tháp và ghi lại những chi tiết khác nhau ở chân tháp.');

    DECLARE @Plan nvarchar(max) = (
        SELECT Ngay AS ngay, ThuTu AS thuTu, DiSanId AS diSanId, GhiChu AS ghiChu
        FROM @Stops
        ORDER BY Ngay, ThuTu
        FOR JSON PATH, ROOT('diem')
    );
    DECLARE @Sources nvarchar(max) = (
        SELECT 'DI_SAN' AS loai, DiSanId AS id
        FROM @Stops
        ORDER BY Ngay, ThuTu
        FOR JSON PATH
    );

    DECLARE @CheckIns TABLE (DiSanId int PRIMARY KEY, CheckInId bigint NOT NULL);
    DECLARE @UserId int, @Chat bigint, @Reply bigint, @Trip bigint, @Booking bigint;
    DECLARE @Day1 date, @Day2 date;

    -- Dùng cùng mã đánh dấu với bộ dữ liệu trước để tránh tạo trùng.
    DECLARE DemoUsers CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.NguoiDungId FROM @Targets t
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.CheckInDiSan c
            WHERE c.NguoiDungId = t.NguoiDungId AND c.MaYeuCau = @HomeKey
        )
        ORDER BY t.NguoiDungId;

    OPEN DemoUsers;
    FETCH NEXT FROM DemoUsers INTO @UserId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Day1 = DATEADD(DAY, -3, @Today);

        WHILE EXISTS (
            SELECT 1
            FROM dbo.CheckInDiSan c JOIN @Stops s ON s.DiSanId = c.DiSanId
            WHERE c.NguoiDungId = @UserId
              AND c.NgayDiaPhuong = DATEADD(DAY, s.Ngay - 1, @Day1)
        )
            SET @Day1 = DATEADD(DAY, -1, @Day1);

        SET @Day2 = DATEADD(DAY, 1, @Day1);
        DELETE FROM @CheckIns;

        -- Hội thoại và đề xuất hành trình mẫu.
        INSERT dbo.HoiThoaiAI(NguoiDungId, TieuDe)
        VALUES (@UserId, N'[DEMO] Lên lịch hai ngày ở Hội An');
        SET @Chat = CONVERT(bigint, SCOPE_IDENTITY());

        EXEC dbo.usp_LuuTinNhanGemini @UserId, @Chat, 'USER',
            N'Lên lịch từ My Home để ghé phố cổ, Thanh Hà và Mỹ Sơn.',
            @TraKetQua = 0;

        EXEC dbo.usp_LuuTinNhanGemini @UserId, @Chat, 'ASSISTANT',
            N'[DEMO] Ngày 1 ghé phố cổ và Thanh Hà. Ngày 2 dành cho Mỹ Sơn.',
            @MaMoHinh = 'gemini-demo',
            @DeXuatHanhTrinhJson = @Plan,
            @NguonJson = @Sources,
            @TinNhanAIId = @Reply OUTPUT,
            @TraKetQua = 0;

        EXEC dbo.usp_TaoHanhTrinh @UserId, @TripName, @Day1, @Day2,
            @MoTa = N'Chuyến đi giả lập để thử bản đồ, check-in và sổ tay.',
            @TinNhanAIId = @Reply,
            @HanhTrinhId = @Trip OUTPUT,
            @TraKetQua = 0;

        UPDATE dbo.HanhTrinh
        SET TrangThai = 'HOAN_THANH', AnhBiaPath = N'/images/di-san/hoi-an.webp'
        WHERE HanhTrinhId = @Trip;

        -- Tạo check-in trong quá khứ. Tọa độ và bán kính lấy từ di sản hiện hành.
        INSERT dbo.CheckInDiSan
            (NguoiDungId, DiSanId, MaYeuCau, ThoiGianUtc, ViDo, KinhDo,
             SaiSoGpsM, TamViDo, TamKinhDo, BanKinhApDungM,
             SaiSoToiDaApDungM, KhoangCachM, GhiChu, AnhPath)
        OUTPUT inserted.DiSanId, inserted.CheckInId INTO @CheckIns
        SELECT @UserId, d.DiSanId,
               CASE WHEN d.DiSanId = @Home THEN @HomeKey ELSE NEWID() END,
               DATEADD(HOUR, 1 + s.ThuTu,
                   CONVERT(datetime2(3), DATEADD(DAY, s.Ngay - 1, @Day1))),
               d.ViDo, d.KinhDo, 5, d.ViDo, d.KinhDo,
               d.BanKinhCheckInM, d.SaiSoGpsToiDaM,
               dbo.fn_KhoangCachM(d.ViDo, d.KinhDo, d.ViDo, d.KinhDo),
               N'[DEMO] ' + s.GhiChu, s.Anh
        FROM @Stops s
        JOIN dbo.vw_DiSanCongKhai d ON d.DiSanId = s.DiSanId;

        INSERT dbo.HanhTrinhCheckIn(HanhTrinhId, CheckInId, NguoiDungId)
        SELECT @Trip, CheckInId, @UserId FROM @CheckIns;

        INSERT dbo.NhatKyHanhTrinh
            (HanhTrinhId, CheckInId, TieuDe, NoiDung, NgayGhi, AnhBiaPath)
        SELECT @Trip, c.CheckInId, s.TieuDe, s.NoiDung,
               DATEADD(DAY, s.Ngay - 1, @Day1), s.Anh
        FROM @Stops s JOIN @CheckIns c ON c.DiSanId = s.DiSanId;

        INSERT dbo.MediaNhatKy(NhatKyId, AnhPath, ChuThich, ThuTu)
        SELECT NhatKyId, AnhBiaPath, N'Ảnh minh họa cho ghi chép mẫu', 1
        FROM dbo.NhatKyHanhTrinh WHERE HanhTrinhId = @Trip;

        -- Địa điểm đã lưu và sở thích cho tính năng gợi ý.
        INSERT dbo.DiSanYeuThich(NguoiDungId, DiSanId)
        SELECT @UserId, s.DiSanId FROM @Stops s
        WHERE s.DiSanId <> @Home AND NOT EXISTS (
            SELECT 1 FROM dbo.DiSanYeuThich f
            WHERE f.NguoiDungId = @UserId AND f.DiSanId = s.DiSanId
        );

        INSERT dbo.SoThichNguoiDung(NguoiDungId, ChuDeId, MucDo)
        SELECT @UserId, c.ChuDeId, 4 FROM dbo.ChuDe c
        WHERE c.Ma IN ('KIEN_TRUC', 'GOM') AND NOT EXISTS (
            SELECT 1 FROM dbo.SoThichNguoiDung f
            WHERE f.NguoiDungId = @UserId AND f.ChuDeId = c.ChuDeId
        );

        IF EXISTS (
            SELECT 1 FROM dbo.NguoiDung
            WHERE NguoiDungId = @UserId AND ChoPhepCaNhanHoa = 1
        )
            INSERT dbo.SuKienTuongTac(MaSuKien, NguoiDungId, Loai, DiSanId)
            SELECT NEWID(), @UserId, 'XEM', DiSanId
            FROM @Stops WHERE DiSanId <> @Home;

        INSERT dbo.DanhGiaDiSan
            (NguoiDungId, DiSanId, CheckInId, SoSao, NoiDung, CongKhai)
        SELECT @UserId, c.DiSanId, c.CheckInId,
               CASE WHEN c.DiSanId = @MySon THEN 4 ELSE 5 END,
               N'[DEMO] ' + s.GhiChu,
               CASE WHEN c.DiSanId = @MySon THEN 0 ELSE 1 END
        FROM @CheckIns c JOIN @Stops s ON s.DiSanId = c.DiSanId
        WHERE c.DiSanId <> @Home AND NOT EXISTS (
            SELECT 1 FROM dbo.DanhGiaDiSan r
            WHERE r.NguoiDungId = @UserId AND r.DiSanId = c.DiSanId
        );

        -- Hai đóng góp để hiện trong hồ sơ và hàng đợi kiểm duyệt.
        INSERT dbo.DongGopDiSan
            (NguoiDungId, MaYeuCau, Loai, DiSanId, TieuDe, NoiDung,
             AnhPath, TrangThai, NguoiDuyetId, DuyetLucUtc,
             DiSanCongKhaiId, TuLieuId, TaoLucUtc, CapNhatLucUtc)
        VALUES
            (@UserId, NEWID(), 'BO_SUNG', @ThanhHa,
             N'[DEMO] Bổ sung ghi chép về bàn xoay ở Thanh Hà',
             N'Đề xuất thêm ghi chép quan sát thao tác tạo hình và ảnh minh họa.',
             N'/images/di-san/thanh-ha.webp', 'CHO_DUYET',
             NULL, NULL, NULL, NULL, @Now, @Now),
            (@UserId, NEWID(), 'BO_SUNG', @HoiAn,
             N'[DEMO] Bổ sung ghi chép về nhà phố Hội An',
             N'Gửi ghi chép mẫu về mái ngói và mặt tiền nhà phố.',
             N'/images/di-san/hoi-an.webp', 'DA_DUYET',
             @Moderator, @Now, @HoiAn, @Material, @Now, @Now);

        IF @Question IS NOT NULL AND @Answer IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM dbo.KetQuaTraLoi
               WHERE NguoiDungId = @UserId AND CauHoiId = @Question AND DungKhiNop = 1
           )
            EXEC dbo.usp_NopCauTraLoi @UserId, @Question, @Answer, @TraKetQua = 0;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.DiemDiSan WHERE NguoiDungId = @UserId AND DiSanId = @Home
        )
            INSERT dbo.DiemDiSan(NguoiDungId, DiSanId, Diem)
            VALUES (@UserId, @Home, 30);

        -- Một lượt đã tham gia để có đánh giá trải nghiệm.
        SET @Booking = NULL;

        SELECT TOP (1) @Booking = YeuCauId
        FROM dbo.YeuCauThamGia
        WHERE NguoiDungId = @UserId AND LichTraiNghiemId = @PastSlot
          AND TrangThai = 'DA_THAM_GIA'
        ORDER BY YeuCauId DESC;

        IF @Booking IS NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.YeuCauThamGia
            WHERE NguoiDungId = @UserId AND LichTraiNghiemId = @PastSlot
              AND TrangThai IN ('CHO_XAC_NHAN', 'DA_XAC_NHAN')
        )
        BEGIN
            INSERT dbo.YeuCauThamGia
                (LichTraiNghiemId, TraiNghiemId, NguoiDungId, SoNguoi, DonGiaDuKien,
                 GhiChu, TrangThai, TaoLucUtc, XuLyLucUtc, NguoiXuLyId)
            VALUES
                (@PastSlot, @Experience, @UserId, 1, 60000, N'[DEMO] Đã tham gia nặn gốm.',
                 'DA_THAM_GIA', DATEADD(DAY, -2, @PastStart), @PastEnd, @PartnerUser);

            SET @Booking = CONVERT(bigint, SCOPE_IDENTITY());
        END;

        IF @Booking IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.DanhGiaTraiNghiem
            WHERE NguoiDungId = @UserId AND TraiNghiemId = @Experience
        )
            INSERT dbo.DanhGiaTraiNghiem(NguoiDungId, TraiNghiemId, YeuCauId, SoSao, NoiDung)
            VALUES (@UserId, @Experience, @Booking, 5,
                    N'[DEMO] Phần hướng dẫn nặn gốm dễ theo dõi.');

        -- Một yêu cầu chờ xác nhận để đối tác có dữ liệu xử lý.
        IF @UserId <> @PartnerUser AND NOT EXISTS (
            SELECT 1 FROM dbo.YeuCauThamGia
            WHERE NguoiDungId = @UserId AND LichTraiNghiemId = @FutureSlot
              AND TrangThai IN ('CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DA_THAM_GIA')
        )
            INSERT dbo.YeuCauThamGia
                (LichTraiNghiemId, TraiNghiemId, NguoiDungId, SoNguoi, DonGiaDuKien, GhiChu)
            VALUES (@FutureSlot, @Experience, @UserId, 2, 60000,
                    N'[DEMO] Xin giữ chỗ cho hai người.');

        -- Đơn đối tác cho hàng đợi của quản trị viên.
        IF NOT EXISTS (
            SELECT 1 FROM dbo.NguoiDungVaiTro
            WHERE NguoiDungId = @UserId AND MaVaiTro = 'DOI_TAC'
        ) AND NOT EXISTS (
            SELECT 1 FROM dbo.DonDangKyDoiTac
            WHERE NguoiDungId = @UserId AND TrangThai = 'CHO_DUYET'
        )
            INSERT dbo.DonDangKyDoiTac
                (NguoiDungId, TinhThanhId, Ten, Email, DienThoai, DiaChi, MoTa)
            VALUES
                (@UserId, @DaNang, CONCAT(N'[DEMO] Nhóm trải nghiệm ', @UserId),
                 N'doi-tac@example.test', '0000000000',
                 N'Đà Nẵng · Địa chỉ giả lập',
                 N'Đơn đăng ký mẫu để thử màn hình quản trị.');

        FETCH NEXT FROM DemoUsers INTO @UserId;
    END;

    CLOSE DemoUsers;
    DEALLOCATE DemoUsers;

    COMMIT TRANSACTION;

    ----------------------------------------------------------------------
    -- 5. Kết quả để so sánh với các ô trên trang Hồ sơ
    ----------------------------------------------------------------------
    SELECT
        u.NguoiDungId, u.HoTen,
        COALESCE(r.Diem, 0) AS Diem,
        r.Hang,
        (SELECT COUNT(DISTINCT c.DiSanId)
         FROM dbo.CheckInDiSan c
         WHERE c.NguoiDungId = u.NguoiDungId) AS DiSanDaDen,
        (SELECT COUNT(*)
         FROM dbo.NhatKyHanhTrinh n
         JOIN dbo.HanhTrinh h ON h.HanhTrinhId = n.HanhTrinhId
         WHERE h.NguoiDungId = u.NguoiDungId) AS KhoanhKhac,
        (SELECT COUNT(*)
         FROM dbo.DongGopDiSan d
         WHERE d.NguoiDungId = u.NguoiDungId) AS DongGop
    FROM @Targets t
    JOIN dbo.NguoiDung u ON u.NguoiDungId = t.NguoiDungId
    LEFT JOIN dbo.vw_XepHangDiSan r ON r.NguoiDungId = u.NguoiDungId
    ORDER BY CASE WHEN u.NguoiDungId = @ProfileId THEN 0 ELSE 1 END, u.NguoiDungId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;