using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DiSan
{
    public int DiSanId { get; set; }

    public int TinhThanhId { get; set; }

    public int LoaiDiSanId { get; set; }

    public string Slug { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string TomTat { get; set; } = null!;

    public string? NoiDung { get; set; }

    public string DiaChi { get; set; } = null!;

    public decimal ViDo { get; set; }

    public decimal KinhDo { get; set; }

    public short BanKinhCheckInM { get; set; }

    public short SaiSoGpsToiDaM { get; set; }

    public decimal? GiaVeThamKhao { get; set; }

    public short? ThoiLuongPhut { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public byte[] PhienBan { get; set; } = null!;

    public virtual ICollection<CheckInDiSan> CheckInDiSans { get; set; } = new List<CheckInDiSan>();

    public virtual ICollection<ChiTietGoiY> ChiTietGoiYs { get; set; } = new List<ChiTietGoiY>();

    public virtual ICollection<DiSanYeuThich> DiSanYeuThiches { get; set; } = new List<DiSanYeuThich>();

    public virtual ICollection<DiemHanhTrinh> DiemHanhTrinhs { get; set; } = new List<DiemHanhTrinh>();

    public virtual ICollection<GioThamQuan> GioThamQuans { get; set; } = new List<GioThamQuan>();

    public virtual LoaiDiSan LoaiDiSan { get; set; } = null!;

    public virtual ICollection<MediaDiSan> MediaDiSans { get; set; } = new List<MediaDiSan>();

    public virtual ICollection<NguonTraLoiAi> NguonTraLoiAis { get; set; } = new List<NguonTraLoiAi>();

    public virtual ICollection<NhiemVu> NhiemVus { get; set; } = new List<NhiemVu>();

    public virtual ICollection<SuKienTuongTac> SuKienTuongTacs { get; set; } = new List<SuKienTuongTac>();

    public virtual TinhThanh TinhThanh { get; set; } = null!;

    public virtual ICollection<TraiNghiem> TraiNghiems { get; set; } = new List<TraiNghiem>();

    public virtual ICollection<TuLieuDiSan> TuLieuDiSans { get; set; } = new List<TuLieuDiSan>();

    public virtual ICollection<ChuDe> ChuDes { get; set; } = new List<ChuDe>();
}
