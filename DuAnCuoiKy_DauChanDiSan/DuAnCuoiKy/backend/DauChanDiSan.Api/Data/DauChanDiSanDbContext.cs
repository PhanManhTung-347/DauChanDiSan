using System;
using System.Collections.Generic;
using DauChanDiSan.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Data;

public partial class DauChanDiSanDbContext : DbContext
{
    public DauChanDiSanDbContext(DbContextOptions<DauChanDiSanDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CauHoiNhiemVu> CauHoiNhiemVus { get; set; }

    public virtual DbSet<CheckInDiSan> CheckInDiSans { get; set; }

    public virtual DbSet<ChiTietGoiY> ChiTietGoiYs { get; set; }

    public virtual DbSet<ChuDe> ChuDes { get; set; }

    public virtual DbSet<DanhGiaDiSan> DanhGiaDiSans { get; set; }

    public virtual DbSet<DanhGiaTraiNghiem> DanhGiaTraiNghiems { get; set; }

    public virtual DbSet<DiSan> DiSans { get; set; }

    public virtual DbSet<DiSanYeuThich> DiSanYeuThiches { get; set; }

    public virtual DbSet<DiemHanhTrinh> DiemHanhTrinhs { get; set; }

    public virtual DbSet<DoiTac> DoiTacs { get; set; }

    public virtual DbSet<GioThamQuan> GioThamQuans { get; set; }

    public virtual DbSet<HanhTrinh> HanhTrinhs { get; set; }

    public virtual DbSet<HanhTrinhCheckIn> HanhTrinhCheckIns { get; set; }

    public virtual DbSet<HoiThoaiAi> HoiThoaiAis { get; set; }

    public virtual DbSet<HuyHieu> HuyHieus { get; set; }

    public virtual DbSet<HuyHieuNguoiDung> HuyHieuNguoiDungs { get; set; }

    public virtual DbSet<KetQuaTraLoi> KetQuaTraLois { get; set; }

    public virtual DbSet<LichTraiNghiem> LichTraiNghiems { get; set; }

    public virtual DbSet<LoaiDiSan> LoaiDiSans { get; set; }

    public virtual DbSet<LuaChonCauHoi> LuaChonCauHois { get; set; }

    public virtual DbSet<LuotGoiY> LuotGoiYs { get; set; }

    public virtual DbSet<MediaDiSan> MediaDiSans { get; set; }

    public virtual DbSet<MediaNhatKy> MediaNhatKies { get; set; }

    public virtual DbSet<MediaTraiNghiem> MediaTraiNghiems { get; set; }

    public virtual DbSet<MoHinhGoiY> MoHinhGoiYs { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; }

    public virtual DbSet<NguonTraLoiAi> NguonTraLoiAis { get; set; }

    public virtual DbSet<NhatKyHanhTrinh> NhatKyHanhTrinhs { get; set; }

    public virtual DbSet<NhiemVu> NhiemVus { get; set; }

    public virtual DbSet<PhienBanCsdl> PhienBanCsdls { get; set; }

    public virtual DbSet<SoThichNguoiDung> SoThichNguoiDungs { get; set; }

    public virtual DbSet<SuKienTuongTac> SuKienTuongTacs { get; set; }

    public virtual DbSet<ThanhVienDoiTac> ThanhVienDoiTacs { get; set; }

    public virtual DbSet<TienDoNhiemVu> TienDoNhiemVus { get; set; }

    public virtual DbSet<TinNhanAi> TinNhanAis { get; set; }

    public virtual DbSet<TinhThanh> TinhThanhs { get; set; }

    public virtual DbSet<TraiNghiem> TraiNghiems { get; set; }

    public virtual DbSet<TuLieuDiSan> TuLieuDiSans { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<VwDiSanCongKhai> VwDiSanCongKhais { get; set; }

    public virtual DbSet<VwDuLieuHuanLuyen> VwDuLieuHuanLuyens { get; set; }

    public virtual DbSet<VwLichTraiNghiem> VwLichTraiNghiems { get; set; }

    public virtual DbSet<VwNguCanhGemini> VwNguCanhGeminis { get; set; }

    public virtual DbSet<VwNhatKyCongKhai> VwNhatKyCongKhais { get; set; }

    public virtual DbSet<VwTraiNghiemCongKhai> VwTraiNghiemCongKhais { get; set; }

    public virtual DbSet<YeuCauThamGium> YeuCauThamGia { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CauHoiNhiemVu>(entity =>
        {
            entity.HasKey(e => e.CauHoiId).HasName("PK__CauHoiNh__EDF63F1CCEB7656B");

            entity.ToTable("CauHoiNhiemVu", tb => tb.HasTrigger("tr_CauHoi_ChiChoQuiz"));

            entity.HasIndex(e => new { e.NhiemVuId, e.ThuTu }, "UQ__CauHoiNh__D516A4FBC0C83134").IsUnique();

            entity.Property(e => e.GiaiThich).HasMaxLength(800);
            entity.Property(e => e.NoiDung).HasMaxLength(500);

            entity.HasOne(d => d.NhiemVu).WithMany(p => p.CauHoiNhiemVus)
                .HasForeignKey(d => d.NhiemVuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CauHoiNhi__Nhiem__17C286CF");
        });

        modelBuilder.Entity<CheckInDiSan>(entity =>
        {
            entity.HasKey(e => e.CheckInId).HasName("PK__CheckInD__E64976840422F0FE");

            entity.ToTable("CheckInDiSan", tb => tb.HasTrigger("tr_CheckIn_KiemTraGPS"));

            entity.HasIndex(e => new { e.NguoiDungId, e.ThoiGianUtc }, "IX_CheckIn_TienDo");

            entity.HasIndex(e => new { e.CheckInId, e.NguoiDungId }, "UQ__CheckInD__2A02CCCE8AFE38A8").IsUnique();

            entity.HasIndex(e => new { e.NguoiDungId, e.MaYeuCau }, "UQ__CheckInD__3841F948010F2FEE").IsUnique();

            entity.HasIndex(e => new { e.NguoiDungId, e.DiSanId, e.NgayDiaPhuong }, "UQ__CheckInD__97318D0CBE1F0DA1").IsUnique();

            entity.HasIndex(e => new { e.CheckInId, e.NguoiDungId, e.DiSanId }, "UQ__CheckInD__CD941C850D1E7FCE").IsUnique();

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.KhoangCachM).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.MaYeuCau).HasDefaultValueSql("(newid())");
            entity.Property(e => e.NgayDiaPhuong).HasComputedColumnSql("(CONVERT([date],dateadd(minute,(420),[ThoiGianUtc])))", true);
            entity.Property(e => e.SaiSoGpsM).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.TamKinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.TamViDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.ThoiGianUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.DiSan).WithMany(p => p.CheckInDiSans)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInDi__DiSan__6AEFE058");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.CheckInDiSans)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInDi__Nguoi__69FBBC1F");
        });

        modelBuilder.Entity<ChiTietGoiY>(entity =>
        {
            entity.HasKey(e => new { e.LuotGoiYid, e.ThuHang }).HasName("PK__ChiTietG__31233C1332D7CC48");

            entity.ToTable("ChiTietGoiY");

            entity.HasIndex(e => new { e.LuotGoiYid, e.DiSanId }, "UX_ChiTietGoiY_DiSan")
                .IsUnique()
                .HasFilter("([DiSanId] IS NOT NULL)");

            entity.HasIndex(e => new { e.LuotGoiYid, e.TraiNghiemId }, "UX_ChiTietGoiY_TraiNghiem")
                .IsUnique()
                .HasFilter("([TraiNghiemId] IS NOT NULL)");

            entity.Property(e => e.LuotGoiYid).HasColumnName("LuotGoiYId");
            entity.Property(e => e.Diem).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.LyDo).HasMaxLength(350);

            entity.HasOne(d => d.DiSan).WithMany(p => p.ChiTietGoiYs)
                .HasForeignKey(d => d.DiSanId)
                .HasConstraintName("FK__ChiTietGo__DiSan__4D2A7347");

            entity.HasOne(d => d.LuotGoiY).WithMany(p => p.ChiTietGoiYs)
                .HasForeignKey(d => d.LuotGoiYid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietGo__LuotG__4B422AD5");

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.ChiTietGoiYs)
                .HasForeignKey(d => d.TraiNghiemId)
                .HasConstraintName("FK__ChiTietGo__TraiN__4E1E9780");
        });

        modelBuilder.Entity<ChuDe>(entity =>
        {
            entity.HasKey(e => e.ChuDeId).HasName("PK__ChuDe__381DCA6B0C2592BC");

            entity.ToTable("ChuDe");

            entity.HasIndex(e => e.Ma, "UQ__ChuDe__3214CC9E8B4D20AB").IsUnique();

            entity.HasIndex(e => e.Ten, "UQ__ChuDe__C451FA8345C0FAC5").IsUnique();

            entity.Property(e => e.Ma)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<DanhGiaDiSan>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.DiSanId }).HasName("PK__DanhGiaD__BDD6A003D6D5FF14");

            entity.ToTable("DanhGiaDiSan");

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.CongKhai).HasDefaultValue(true);
            entity.Property(e => e.NoiDung).HasMaxLength(1000);
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.CheckInDiSan).WithMany(p => p.DanhGiaDiSans)
                .HasPrincipalKey(p => new { p.CheckInId, p.NguoiDungId, p.DiSanId })
                .HasForeignKey(d => new { d.CheckInId, d.NguoiDungId, d.DiSanId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DanhGiaDiSan__345EC57D");
        });

        modelBuilder.Entity<DanhGiaTraiNghiem>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.TraiNghiemId }).HasName("PK__DanhGiaT__2C3D535CB71F4BF3");

            entity.ToTable("DanhGiaTraiNghiem");

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.CongKhai).HasDefaultValue(true);
            entity.Property(e => e.NoiDung).HasMaxLength(1000);
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TrangThaiYeuCau)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("DA_THAM_GIA");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.YeuCauThamGium).WithMany(p => p.DanhGiaTraiNghiems)
                .HasPrincipalKey(p => new { p.YeuCauId, p.NguoiDungId, p.TraiNghiemId, p.TrangThai })
                .HasForeignKey(d => new { d.YeuCauId, d.NguoiDungId, d.TraiNghiemId, d.TrangThaiYeuCau })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DanhGiaTraiNghie__3BFFE745");
        });

        modelBuilder.Entity<DiSan>(entity =>
        {
            entity.HasKey(e => e.DiSanId).HasName("PK__DiSan__96D04BE686202C89");

            entity.ToTable("DiSan");

            entity.HasIndex(e => new { e.TrangThai, e.TinhThanhId, e.LoaiDiSanId }, "IX_DiSan_KhamPha");

            entity.HasIndex(e => e.Slug, "UQ__DiSan__BC7B5FB65019559C").IsUnique();

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.BanKinhCheckInM).HasDefaultValue((short)150);
            entity.Property(e => e.DiaChi).HasMaxLength(250);
            entity.Property(e => e.GiaVeThamKhao).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.PhienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SaiSoGpsToiDaM).HasDefaultValue((short)50);
            entity.Property(e => e.Slug)
                .HasMaxLength(160)
                .IsUnicode(false);
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Ten).HasMaxLength(200);
            entity.Property(e => e.TomTat).HasMaxLength(1000);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("NHAP");
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.LoaiDiSan).WithMany(p => p.DiSans)
                .HasForeignKey(d => d.LoaiDiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiSan__LoaiDiSan__5CD6CB2B");

            entity.HasOne(d => d.TinhThanh).WithMany(p => p.DiSans)
                .HasForeignKey(d => d.TinhThanhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiSan__TinhThanh__5BE2A6F2");

            entity.HasMany(d => d.ChuDes).WithMany(p => p.DiSans)
                .UsingEntity<Dictionary<string, object>>(
                    "DiSanChuDe",
                    r => r.HasOne<ChuDe>().WithMany()
                        .HasForeignKey("ChuDeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__DiSanChuD__ChuDe__6C190EBB"),
                    l => l.HasOne<DiSan>().WithMany()
                        .HasForeignKey("DiSanId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__DiSanChuD__DiSan__6B24EA82"),
                    j =>
                    {
                        j.HasKey("DiSanId", "ChuDeId").HasName("PK__DiSanChu__3551974062F2768E");
                        j.ToTable("DiSanChuDe");
                        j.HasIndex(new[] { "ChuDeId", "DiSanId" }, "IX_DiSanChuDe_ChuDe");
                    });
        });

        modelBuilder.Entity<DiSanYeuThich>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.DiSanId }).HasName("PK__DiSanYeu__BDD6A00357E3BA70");

            entity.ToTable("DiSanYeuThich");

            entity.Property(e => e.LuuLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.DiSan).WithMany(p => p.DiSanYeuThiches)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiSanYeuT__DiSan__3587F3E0");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.DiSanYeuThiches)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiSanYeuT__Nguoi__3493CFA7");
        });

        modelBuilder.Entity<DiemHanhTrinh>(entity =>
        {
            entity.HasKey(e => e.DiemHanhTrinhId).HasName("PK__DiemHanh__F5603FF6E12494CE");

            entity.ToTable("DiemHanhTrinh", tb => tb.HasTrigger("tr_DiemHanhTrinh_Ngay"));

            entity.HasIndex(e => new { e.HanhTrinhId, e.NgayDuKien, e.ThuTu }, "UQ__DiemHanh__AE4B0351DF73B127").IsUnique();

            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.GioDuKien).HasPrecision(0);

            entity.HasOne(d => d.DiSan).WithMany(p => p.DiemHanhTrinhs)
                .HasForeignKey(d => d.DiSanId)
                .HasConstraintName("FK__DiemHanhT__DiSan__6166761E");

            entity.HasOne(d => d.HanhTrinh).WithMany(p => p.DiemHanhTrinhs)
                .HasForeignKey(d => d.HanhTrinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DiemHanhT__HanhT__5F7E2DAC");

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.DiemHanhTrinhs)
                .HasForeignKey(d => d.TraiNghiemId)
                .HasConstraintName("FK__DiemHanhT__TraiN__625A9A57");
        });

        modelBuilder.Entity<DoiTac>(entity =>
        {
            entity.HasKey(e => e.DoiTacId).HasName("PK__DoiTac__535127907B7A3172");

            entity.ToTable("DoiTac");

            entity.HasIndex(e => new { e.TinhThanhId, e.Ten }, "UQ__DoiTac__D6CDB34374814F0B").IsUnique();

            entity.Property(e => e.AnhDaiDienPath).HasMaxLength(450);
            entity.Property(e => e.DiaChi).HasMaxLength(250);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.MoTa).HasMaxLength(1000);
            entity.Property(e => e.Ten).HasMaxLength(160);

            entity.HasOne(d => d.TinhThanh).WithMany(p => p.DoiTacs)
                .HasForeignKey(d => d.TinhThanhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DoiTac__TinhThan__02084FDA");
        });

        modelBuilder.Entity<GioThamQuan>(entity =>
        {
            entity.HasKey(e => e.GioThamQuanId).HasName("PK__GioThamQ__B83D944728CA3287");

            entity.ToTable("GioThamQuan", tb => tb.HasTrigger("tr_GioThamQuan_TrungGio"));

            entity.HasIndex(e => new { e.DiSanId, e.ThuTrongTuan, e.GioMo }, "UQ__GioThamQ__B6A7D5F0EE9710B7").IsUnique();

            entity.Property(e => e.GioDong).HasPrecision(0);
            entity.Property(e => e.GioMo).HasPrecision(0);

            entity.HasOne(d => d.DiSan).WithMany(p => p.GioThamQuans)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GioThamQu__DiSan__7C4F7684");
        });

        modelBuilder.Entity<HanhTrinh>(entity =>
        {
            entity.HasKey(e => e.HanhTrinhId).HasName("PK__HanhTrin__D3ABEF7C1340FED8");

            entity.ToTable("HanhTrinh", tb => tb.HasTrigger("tr_HanhTrinh_Ngay"));

            entity.HasIndex(e => new { e.NguoiDungId, e.NgayBatDau }, "IX_HanhTrinh_NguoiDung");

            entity.HasIndex(e => new { e.HanhTrinhId, e.NguoiDungId }, "UQ__HanhTrin__1FE055365451AE86").IsUnique();

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.MoTa).HasMaxLength(1000);
            entity.Property(e => e.PhienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TieuDe).HasMaxLength(160);
            entity.Property(e => e.TinNhanAiid).HasColumnName("TinNhanAIId");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("LEN_KE_HOACH");
            entity.Property(e => e.VaiTinNhanAi)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("ASSISTANT")
                .HasColumnName("VaiTinNhanAI");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.HanhTrinhs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HanhTrinh__Nguoi__55009F39");

            entity.HasOne(d => d.TinNhanAi).WithMany(p => p.HanhTrinhs)
                .HasPrincipalKey(p => new { p.TinNhanAiid, p.NguoiDungId, p.Vai })
                .HasForeignKey(d => new { d.TinNhanAiid, d.NguoiDungId, d.VaiTinNhanAi })
                .HasConstraintName("FK__HanhTrinh__5AB9788F");
        });

        modelBuilder.Entity<HanhTrinhCheckIn>(entity =>
        {
            entity.HasKey(e => new { e.HanhTrinhId, e.CheckInId }).HasName("PK__HanhTrin__8DCF781444554694");

            entity.ToTable("HanhTrinhCheckIn");

            entity.HasIndex(e => e.CheckInId, "UQ__HanhTrin__E6497685354B039D").IsUnique();

            entity.HasOne(d => d.CheckInDiSan).WithMany(p => p.HanhTrinhCheckIns)
                .HasPrincipalKey(p => new { p.CheckInId, p.NguoiDungId })
                .HasForeignKey(d => new { d.CheckInId, d.NguoiDungId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HanhTrinhCheckIn__793DFFAF");

            entity.HasOne(d => d.HanhTrinh).WithMany(p => p.HanhTrinhCheckIns)
                .HasPrincipalKey(p => new { p.HanhTrinhId, p.NguoiDungId })
                .HasForeignKey(d => new { d.HanhTrinhId, d.NguoiDungId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HanhTrinhCheckIn__7849DB76");
        });

        modelBuilder.Entity<HoiThoaiAi>(entity =>
        {
            entity.HasKey(e => e.HoiThoaiId).HasName("PK__HoiThoai__35172B126ACD8DD8");

            entity.ToTable("HoiThoaiAI");

            entity.HasIndex(e => new { e.HoiThoaiId, e.NguoiDungId }, "UQ__HoiThoai__F95C91584B06077F").IsUnique();

            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TieuDe).HasMaxLength(160);

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.HoiThoaiAis)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HoiThoaiA__Nguoi__3A4CA8FD");
        });

        modelBuilder.Entity<HuyHieu>(entity =>
        {
            entity.HasKey(e => e.HuyHieuId).HasName("PK__HuyHieu__5D0D6C96D8DEB7B5");

            entity.ToTable("HuyHieu");

            entity.HasIndex(e => e.Ma, "UQ__HuyHieu__3214CC9E725919D5").IsUnique();

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.Ma)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.Ten).HasMaxLength(120);
        });

        modelBuilder.Entity<HuyHieuNguoiDung>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.HuyHieuId }).HasName("PK__HuyHieuN__B16B727446DB783A");

            entity.ToTable("HuyHieuNguoiDung");

            entity.Property(e => e.NhanLucUtc).HasPrecision(3);

            entity.HasOne(d => d.HuyHieu).WithMany(p => p.HuyHieuNguoiDungs)
                .HasForeignKey(d => d.HuyHieuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HuyHieuNg__HuyHi__2DB1C7EE");

            entity.HasOne(d => d.TienDoNhiemVu).WithMany(p => p.HuyHieuNguoiDungs)
                .HasForeignKey(d => new { d.NguoiDungId, d.NhiemVuId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HuyHieuNguoiDung__2EA5EC27");
        });

        modelBuilder.Entity<KetQuaTraLoi>(entity =>
        {
            entity.HasKey(e => e.KetQuaId).HasName("PK__KetQuaTr__0CFF0203A44ABC06");

            entity.ToTable("KetQuaTraLoi");

            entity.HasIndex(e => new { e.NguoiDungId, e.CauHoiId, e.DungKhiNop, e.NopLucUtc }, "IX_KetQua_TienDo");

            entity.Property(e => e.NopLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.KetQuaTraLois)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__KetQuaTra__Nguoi__22401542");

            entity.HasOne(d => d.LuaChonCauHoi).WithMany(p => p.KetQuaTraLois)
                .HasPrincipalKey(p => new { p.CauHoiId, p.LuaChonId })
                .HasForeignKey(d => new { d.CauHoiId, d.LuaChonId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__KetQuaTraLoi__24285DB4");
        });

        modelBuilder.Entity<LichTraiNghiem>(entity =>
        {
            entity.HasKey(e => e.LichTraiNghiemId).HasName("PK__LichTrai__4F2387A89FBD923A");

            entity.ToTable("LichTraiNghiem", tb => tb.HasTrigger("tr_LichTraiNghiem_BaoVeYeuCau"));

            entity.HasIndex(e => new { e.LichTraiNghiemId, e.TraiNghiemId }, "UQ__LichTrai__A7A57048BD72BEA9").IsUnique();

            entity.HasIndex(e => new { e.TraiNghiemId, e.BatDauUtc }, "UQ__LichTrai__F6463355EA014B34").IsUnique();

            entity.Property(e => e.BatDauUtc).HasPrecision(3);
            entity.Property(e => e.KetThucUtc).HasPrecision(3);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("MO");

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.LichTraiNghiems)
                .HasForeignKey(d => d.TraiNghiemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LichTraiN__TraiN__1F98B2C1");
        });

        modelBuilder.Entity<LoaiDiSan>(entity =>
        {
            entity.HasKey(e => e.LoaiDiSanId).HasName("PK__LoaiDiSa__CC9B815163F9EB11");

            entity.ToTable("LoaiDiSan");

            entity.HasIndex(e => e.Ma, "UQ__LoaiDiSa__3214CC9EEDAE4B4B").IsUnique();

            entity.HasIndex(e => e.Ten, "UQ__LoaiDiSa__C451FA83A6ECFCD8").IsUnique();

            entity.Property(e => e.Ma)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<LuaChonCauHoi>(entity =>
        {
            entity.HasKey(e => e.LuaChonId).HasName("PK__LuaChonC__A69B2519199BE677");

            entity.ToTable("LuaChonCauHoi");

            entity.HasIndex(e => new { e.CauHoiId, e.LuaChonId }, "UQ__LuaChonC__679F8D4C99876B52").IsUnique();

            entity.HasIndex(e => new { e.CauHoiId, e.ThuTu }, "UQ__LuaChonC__FF14BC2011746AAB").IsUnique();

            entity.HasIndex(e => e.CauHoiId, "UX_LuaChon_DapAnDung")
                .IsUnique()
                .HasFilter("([Dung]=(1))");

            entity.Property(e => e.NoiDung).HasMaxLength(400);

            entity.HasOne(d => d.CauHoi).WithMany(p => p.LuaChonCauHois)
                .HasForeignKey(d => d.CauHoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LuaChonCa__CauHo__1D7B6025");
        });

        modelBuilder.Entity<LuotGoiY>(entity =>
        {
            entity.HasKey(e => e.LuotGoiYid).HasName("PK__LuotGoiY__4178D492217DF1AB");

            entity.ToTable("LuotGoiY");

            entity.HasIndex(e => new { e.LuotGoiYid, e.NguoiDungId }, "UQ__LuotGoiY__8D336ED888FEC6E4").IsUnique();

            entity.Property(e => e.LuotGoiYid).HasColumnName("LuotGoiYId");
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ViTriHienThi)
                .HasMaxLength(24)
                .IsUnicode(false);

            entity.HasOne(d => d.MoHinh).WithMany(p => p.LuotGoiYs)
                .HasForeignKey(d => d.MoHinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LuotGoiY__MoHinh__467D75B8");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.LuotGoiYs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LuotGoiY__NguoiD__4589517F");
        });

        modelBuilder.Entity<MediaDiSan>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaDiS__B2C2B5CF42FC39A4");

            entity.ToTable("MediaDiSan");

            entity.HasIndex(e => new { e.DiSanId, e.ThuTu }, "UQ__MediaDiS__8432C8DA043D0FA2").IsUnique();

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.ChuThich).HasMaxLength(250);
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.DiSan).WithMany(p => p.MediaDiSans)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MediaDiSa__DiSan__76969D2E");
        });

        modelBuilder.Entity<MediaNhatKy>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaNha__B2C2B5CF1F1E9AA9");

            entity.ToTable("MediaNhatKy");

            entity.HasIndex(e => new { e.NhatKyId, e.ThuTu }, "UQ__MediaNha__CDDA68936E810A14").IsUnique();

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.ChuThich).HasMaxLength(250);
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.NhatKy).WithMany(p => p.MediaNhatKies)
                .HasForeignKey(d => d.NhatKyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MediaNhat__NhatK__05A3D694");
        });

        modelBuilder.Entity<MediaTraiNghiem>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaTra__B2C2B5CF0FF4B312");

            entity.ToTable("MediaTraiNghiem");

            entity.HasIndex(e => new { e.TraiNghiemId, e.ThuTu }, "UQ__MediaTra__9A8DFD23E305D5E2").IsUnique();

            entity.Property(e => e.AnhPath).HasMaxLength(450);
            entity.Property(e => e.ChuThich).HasMaxLength(250);
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.MediaTraiNghiems)
                .HasForeignKey(d => d.TraiNghiemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MediaTrai__TraiN__18EBB532");
        });

        modelBuilder.Entity<MoHinhGoiY>(entity =>
        {
            entity.HasKey(e => e.MoHinhId).HasName("PK__MoHinhGo__983D4B7FC2215328");

            entity.ToTable("MoHinhGoiY");

            entity.HasIndex(e => new { e.Ma, e.PhienBan }, "UQ__MoHinhGo__A056F2E66B7A27E0").IsUnique();

            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.HuanLuyenLucUtc).HasPrecision(3);
            entity.Property(e => e.Ma)
                .HasMaxLength(48)
                .IsUnicode(false);
            entity.Property(e => e.PhienBan)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TepMoHinhPath).HasMaxLength(450);
            entity.Property(e => e.ThuatToan).HasMaxLength(120);
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.NguoiDungId).HasName("PK__NguoiDun__C4BBA4BD59C43F5C");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.FirebaseUid, "UQ__NguoiDun__F82B22B205A1EFDF").IsUnique();

            entity.Property(e => e.AnhDaiDienPath).HasMaxLength(450);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FirebaseUid)
                .HasMaxLength(128)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.HoTen).HasMaxLength(120);
            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.LanDongBoUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<NguoiDungVaiTro>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.MaVaiTro }).HasName("PK__NguoiDun__289F60A1A8860FCB");

            entity.ToTable("NguoiDungVaiTro");

            entity.Property(e => e.MaVaiTro)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.CapLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiDungVaiTros)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NguoiDung__MaVai__44FF419A");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.NguoiDungVaiTros)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NguoiDung__Nguoi__440B1D61");
        });

        modelBuilder.Entity<NguonTraLoiAi>(entity =>
        {
            entity.HasKey(e => new { e.TinNhanAiid, e.ThuTu }).HasName("PK__NguonTra__60784921F6291C46");

            entity.ToTable("NguonTraLoiAI");

            entity.Property(e => e.TinNhanAiid).HasColumnName("TinNhanAIId");
            entity.Property(e => e.TieuDeKhiTrich).HasMaxLength(220);
            entity.Property(e => e.UrlKhiTrich).HasMaxLength(1000);
            entity.Property(e => e.Vai)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("ASSISTANT");

            entity.HasOne(d => d.DiSan).WithMany(p => p.NguonTraLoiAis)
                .HasForeignKey(d => d.DiSanId)
                .HasConstraintName("FK__NguonTraL__DiSan__4D5F7D71");

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.NguonTraLoiAis)
                .HasForeignKey(d => d.TraiNghiemId)
                .HasConstraintName("FK__NguonTraL__TraiN__4E53A1AA");

            entity.HasOne(d => d.TuLieu).WithMany(p => p.NguonTraLoiAis)
                .HasForeignKey(d => d.TuLieuId)
                .HasConstraintName("FK__NguonTraL__TuLie__4F47C5E3");

            entity.HasOne(d => d.TinNhanAi).WithMany(p => p.NguonTraLoiAis)
                .HasPrincipalKey(p => new { p.TinNhanAiid, p.NguoiDungId, p.Vai })
                .HasForeignKey(d => new { d.TinNhanAiid, d.NguoiDungId, d.Vai })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NguonTraLoiAI__503BEA1C");
        });

        modelBuilder.Entity<NhatKyHanhTrinh>(entity =>
        {
            entity.HasKey(e => e.NhatKyId).HasName("PK__NhatKyHa__DF38EBAFA3C80123");

            entity.ToTable("NhatKyHanhTrinh");

            entity.HasIndex(e => e.MaChiaSe, "UQ__NhatKyHa__54AE718E2A22D175").IsUnique();

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.MaChiaSe).HasDefaultValueSql("(newid())");
            entity.Property(e => e.PhienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TieuDe).HasMaxLength(160);
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.HanhTrinh).WithMany(p => p.NhatKyHanhTrinhs)
                .HasForeignKey(d => d.HanhTrinhId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyHan__HanhT__7D0E9093");

            entity.HasOne(d => d.HanhTrinhCheckIn).WithMany(p => p.NhatKyHanhTrinhs)
                .HasForeignKey(d => new { d.HanhTrinhId, d.CheckInId })
                .HasConstraintName("FK__NhatKyHanhTrinh__00DF2177");
        });

        modelBuilder.Entity<NhiemVu>(entity =>
        {
            entity.HasKey(e => e.NhiemVuId).HasName("PK__NhiemVu__C7F427C71CE5B627");

            entity.ToTable("NhiemVu", tb => tb.HasTrigger("tr_NhiemVu_BaoVeCauHoi"));

            entity.Property(e => e.BatDauUtc).HasPrecision(3);
            entity.Property(e => e.KetThucUtc).HasPrecision(3);
            entity.Property(e => e.Loai)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(800);
            entity.Property(e => e.Ten).HasMaxLength(160);

            entity.HasOne(d => d.DiSan).WithMany(p => p.NhiemVus)
                .HasForeignKey(d => d.DiSanId)
                .HasConstraintName("FK__NhiemVu__DiSanId__0E391C95");

            entity.HasOne(d => d.HuyHieu).WithMany(p => p.NhiemVus)
                .HasForeignKey(d => d.HuyHieuId)
                .HasConstraintName("FK__NhiemVu__HuyHieu__0D44F85C");

            entity.HasOne(d => d.LoaiDiSan).WithMany(p => p.NhiemVus)
                .HasForeignKey(d => d.LoaiDiSanId)
                .HasConstraintName("FK__NhiemVu__LoaiDiS__0F2D40CE");
        });

        modelBuilder.Entity<PhienBanCsdl>(entity =>
        {
            entity.HasKey(e => e.PhienBan).HasName("PK__PhienBan__2423E7886DCCA467");

            entity.ToTable("PhienBanCSDL");

            entity.Property(e => e.PhienBan)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CaiDatLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GhiChu).HasMaxLength(250);
        });

        modelBuilder.Entity<SoThichNguoiDung>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.ChuDeId }).HasName("PK__SoThichN__673A781B018C9F11");

            entity.ToTable("SoThichNguoiDung");

            entity.Property(e => e.MucDo).HasDefaultValue((byte)3);

            entity.HasOne(d => d.ChuDe).WithMany(p => p.SoThichNguoiDungs)
                .HasForeignKey(d => d.ChuDeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SoThichNg__ChuDe__5629CD9C");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.SoThichNguoiDungs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SoThichNg__Nguoi__5535A963");
        });

        modelBuilder.Entity<SuKienTuongTac>(entity =>
        {
            entity.HasKey(e => e.SuKienId).HasName("PK__SuKienTu__3B9647185DFAAAF9");

            entity.ToTable("SuKienTuongTac", tb => tb.HasTrigger("tr_SuKien_XacNhanGoiY"));

            entity.HasIndex(e => new { e.NguoiDungId, e.ThoiGianUtc }, "IX_SuKien_NguoiDung");

            entity.HasIndex(e => new { e.NguoiDungId, e.MaSuKien }, "UQ__SuKienTu__38ADE1251A79CF64").IsUnique();

            entity.Property(e => e.Loai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LuotGoiYid).HasColumnName("LuotGoiYId");
            entity.Property(e => e.ThoiGianUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.DiSan).WithMany(p => p.SuKienTuongTacs)
                .HasForeignKey(d => d.DiSanId)
                .HasConstraintName("FK__SuKienTuo__DiSan__55BFB948");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.SuKienTuongTacs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SuKienTuo__Nguoi__53D770D6");

            entity.HasOne(d => d.TraiNghiem).WithMany(p => p.SuKienTuongTacs)
                .HasForeignKey(d => d.TraiNghiemId)
                .HasConstraintName("FK__SuKienTuo__TraiN__56B3DD81");

            entity.HasOne(d => d.LuotGoiY).WithMany(p => p.SuKienTuongTacs)
                .HasPrincipalKey(p => new { p.LuotGoiYid, p.NguoiDungId })
                .HasForeignKey(d => new { d.LuotGoiYid, d.NguoiDungId })
                .HasConstraintName("FK__SuKienTuongTac__59904A2C");

            entity.HasOne(d => d.ChiTietGoiY).WithMany(p => p.SuKienTuongTacs)
                .HasForeignKey(d => new { d.LuotGoiYid, d.ThuHang })
                .HasConstraintName("FK__SuKienTuongTac__5A846E65");
        });

        modelBuilder.Entity<ThanhVienDoiTac>(entity =>
        {
            entity.HasKey(e => new { e.DoiTacId, e.NguoiDungId }).HasName("PK__ThanhVie__9F1A9DDBE0AD3BE9");

            entity.ToTable("ThanhVienDoiTac");

            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.Quyen)
                .HasMaxLength(12)
                .IsUnicode(false);

            entity.HasOne(d => d.DoiTac).WithMany(p => p.ThanhVienDoiTacs)
                .HasForeignKey(d => d.DoiTacId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ThanhVien__DoiTa__06CD04F7");

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.ThanhVienDoiTacs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ThanhVien__Nguoi__07C12930");
        });

        modelBuilder.Entity<TienDoNhiemVu>(entity =>
        {
            entity.HasKey(e => new { e.NguoiDungId, e.NhiemVuId }).HasName("PK__TienDoNh__A8C4E6C1B5CD6A65");

            entity.ToTable("TienDoNhiemVu");

            entity.Property(e => e.CapNhatLucUtc).HasPrecision(3);
            entity.Property(e => e.HoanThanhLucUtc).HasPrecision(3);

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.TienDoNhiemVus)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TienDoNhi__Nguoi__2704CA5F");

            entity.HasOne(d => d.NhiemVu).WithMany(p => p.TienDoNhiemVus)
                .HasForeignKey(d => d.NhiemVuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TienDoNhi__Nhiem__27F8EE98");
        });

        modelBuilder.Entity<TinNhanAi>(entity =>
        {
            entity.HasKey(e => e.TinNhanAiid).HasName("PK__TinNhanA__729ACA1C909CDEA3");

            entity.ToTable("TinNhanAI");

            entity.HasIndex(e => new { e.HoiThoaiId, e.TinNhanAiid }, "IX_TinNhanAI_HoiThoai");

            entity.HasIndex(e => new { e.TinNhanAiid, e.NguoiDungId, e.Vai }, "UQ__TinNhanA__FD1480A57149AB0E").IsUnique();

            entity.Property(e => e.TinNhanAiid).HasColumnName("TinNhanAIId");
            entity.Property(e => e.MaMoHinh)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NhaCungCap)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Vai)
                .HasMaxLength(12)
                .IsUnicode(false);

            entity.HasOne(d => d.HoiThoaiAi).WithMany(p => p.TinNhanAis)
                .HasPrincipalKey(p => new { p.HoiThoaiId, p.NguoiDungId })
                .HasForeignKey(d => new { d.HoiThoaiId, d.NguoiDungId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TinNhanAI__45BE5BA9");
        });

        modelBuilder.Entity<TinhThanh>(entity =>
        {
            entity.HasKey(e => e.TinhThanhId).HasName("PK__TinhThan__EA88ACEA53C62966");

            entity.ToTable("TinhThanh");

            entity.HasIndex(e => e.Ma, "UQ__TinhThan__3214CC9EB813DD0B").IsUnique();

            entity.HasIndex(e => e.Ten, "UQ__TinhThan__C451FA836D102AF8").IsUnique();

            entity.Property(e => e.HoatDong).HasDefaultValue(true);
            entity.Property(e => e.Ma)
                .HasMaxLength(32)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<TraiNghiem>(entity =>
        {
            entity.HasKey(e => e.TraiNghiemId).HasName("PK__TraiNghi__886F7E1FF9323584");

            entity.ToTable("TraiNghiem");

            entity.HasIndex(e => new { e.DiSanId, e.TrangThai }, "IX_TraiNghiem_DiSan");

            entity.HasIndex(e => e.Slug, "UQ__TraiNghi__BC7B5FB6A4611492").IsUnique();

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.DiaDiemTapTrung).HasMaxLength(250);
            entity.Property(e => e.DuyetLucUtc).HasPrecision(3);
            entity.Property(e => e.GiaMoiNguoi).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.PhienBan)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Slug)
                .HasMaxLength(160)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(180);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("NHAP");
            entity.Property(e => e.VideoPath).HasMaxLength(450);

            entity.HasOne(d => d.DiSan).WithMany(p => p.TraiNghiems)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TraiNghie__DiSan__0E6E26BF");

            entity.HasOne(d => d.DoiTac).WithMany(p => p.TraiNghiems)
                .HasForeignKey(d => d.DoiTacId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TraiNghie__DoiTa__0D7A0286");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.TraiNghiems)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("FK__TraiNghie__Nguoi__1332DBDC");
        });

        modelBuilder.Entity<TuLieuDiSan>(entity =>
        {
            entity.HasKey(e => e.TuLieuId).HasName("PK__TuLieuDi__9C49D5983ADFF142");

            entity.ToTable("TuLieuDiSan");

            entity.HasIndex(e => new { e.DiSanId, e.CongKhai }, "IX_TuLieu_DiSan");

            entity.Property(e => e.DuyetLucUtc).HasPrecision(3);
            entity.Property(e => e.Nguon).HasMaxLength(200);
            entity.Property(e => e.TieuDe).HasMaxLength(200);
            entity.Property(e => e.TrichYeu).HasMaxLength(1600);
            entity.Property(e => e.UrlNguon).HasMaxLength(1000);

            entity.HasOne(d => d.DiSan).WithMany(p => p.TuLieuDiSans)
                .HasForeignKey(d => d.DiSanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TuLieuDiS__DiSan__6EF57B66");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.TuLieuDiSans)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("FK__TuLieuDiS__Nguoi__6FE99F9F");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CFABEA9645");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.Ten, "UQ__VaiTro__C451FA83D84E7AE0").IsUnique();

            entity.Property(e => e.MaVaiTro)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(80);
        });

        modelBuilder.Entity<VwDiSanCongKhai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DiSanCongKhai");

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.DiaChi).HasMaxLength(250);
            entity.Property(e => e.GiaVeThamKhao).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.LoaiDiSan).HasMaxLength(100);
            entity.Property(e => e.Slug)
                .HasMaxLength(160)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(200);
            entity.Property(e => e.TinhThanh).HasMaxLength(100);
            entity.Property(e => e.TomTat).HasMaxLength(1000);
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.VideoPath).HasMaxLength(450);
        });

        modelBuilder.Entity<VwDuLieuHuanLuyen>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DuLieuHuanLuyen");

            entity.Property(e => e.Loai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LuotGoiYid).HasColumnName("LuotGoiYId");
            entity.Property(e => e.ThoiGianUtc).HasPrecision(3);
        });

        modelBuilder.Entity<VwLichTraiNghiem>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LichTraiNghiem");

            entity.Property(e => e.BatDauUtc).HasPrecision(3);
            entity.Property(e => e.KetThucUtc).HasPrecision(3);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(12)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwNguCanhGemini>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_NguCanhGemini");

            entity.Property(e => e.LoaiNguon)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.TieuDe).HasMaxLength(220);
            entity.Property(e => e.UrlNguon).HasMaxLength(1000);
        });

        modelBuilder.Entity<VwNhatKyCongKhai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_NhatKyCongKhai");

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.TenTacGia).HasMaxLength(120);
            entity.Property(e => e.TieuDe).HasMaxLength(160);
            entity.Property(e => e.VideoPath).HasMaxLength(450);
        });

        modelBuilder.Entity<VwTraiNghiemCongKhai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TraiNghiemCongKhai");

            entity.Property(e => e.AnhBiaPath).HasMaxLength(450);
            entity.Property(e => e.DiaDiemTapTrung).HasMaxLength(250);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(24)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.GiaMoiNguoi).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.Slug)
                .HasMaxLength(160)
                .IsUnicode(false);
            entity.Property(e => e.Ten).HasMaxLength(180);
            entity.Property(e => e.TenDoiTac).HasMaxLength(160);
            entity.Property(e => e.VideoPath).HasMaxLength(450);
        });

        modelBuilder.Entity<YeuCauThamGium>(entity =>
        {
            entity.HasKey(e => e.YeuCauId).HasName("PK__YeuCauTh__A21883CECCF0E807");

            entity.ToTable(tb => tb.HasTrigger("tr_YeuCau_SucChuaVaTrangThai"));

            entity.HasIndex(e => new { e.LichTraiNghiemId, e.TrangThai }, "IX_YeuCau_SucChua");

            entity.HasIndex(e => new { e.NguoiDungId, e.MaYeuCau }, "UQ__YeuCauTh__3841F94860A7C599").IsUnique();

            entity.HasIndex(e => new { e.YeuCauId, e.NguoiDungId, e.TraiNghiemId, e.TrangThai }, "UQ__YeuCauTh__6EC8C0B0AABD8F03").IsUnique();

            entity.HasIndex(e => new { e.LichTraiNghiemId, e.NguoiDungId }, "UX_YeuCau_HieuLuc")
                .IsUnique()
                .HasFilter("([TrangThai] IN ('CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DA_THAM_GIA'))");

            entity.Property(e => e.DonGiaDuKien).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.MaYeuCau).HasDefaultValueSql("(newid())");
            entity.Property(e => e.TaoLucUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(16)
                .IsUnicode(false)
                .HasDefaultValue("CHO_XAC_NHAN");
            entity.Property(e => e.XuLyLucUtc).HasPrecision(3);

            entity.HasOne(d => d.NguoiDung).WithMany(p => p.YeuCauThamGiumNguoiDungs)
                .HasForeignKey(d => d.NguoiDungId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__YeuCauTha__Nguoi__29221CFB");

            entity.HasOne(d => d.NguoiXuLy).WithMany(p => p.YeuCauThamGiumNguoiXuLies)
                .HasForeignKey(d => d.NguoiXuLyId)
                .HasConstraintName("FK__YeuCauTha__Nguoi__2EDAF651");

            entity.HasOne(d => d.LichTraiNghiem).WithMany(p => p.YeuCauThamGia)
                .HasPrincipalKey(p => new { p.LichTraiNghiemId, p.TraiNghiemId })
                .HasForeignKey(d => new { d.LichTraiNghiemId, d.TraiNghiemId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__YeuCauThamGia__2FCF1A8A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
