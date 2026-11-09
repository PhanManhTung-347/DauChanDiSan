using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class TraiNghiem
{
    public int TraiNghiemId { get; set; }

    public int DoiTacId { get; set; }

    public int DiSanId { get; set; }

    public string Slug { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string MoTa { get; set; } = null!;

    public string? DiaDiemTapTrung { get; set; }

    public short ThoiLuongPhut { get; set; }

    public decimal? GiaMoiNguoi { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public string TrangThai { get; set; } = null!;

    public int? NguoiDuyetId { get; set; }

    public DateTime? DuyetLucUtc { get; set; }

    public byte[] PhienBan { get; set; } = null!;

    public virtual ICollection<ChiTietGoiY> ChiTietGoiYs { get; set; } = new List<ChiTietGoiY>();

    public virtual DiSan DiSan { get; set; } = null!;

    public virtual ICollection<DiemHanhTrinh> DiemHanhTrinhs { get; set; } = new List<DiemHanhTrinh>();

    public virtual DoiTac DoiTac { get; set; } = null!;

    public virtual ICollection<LichTraiNghiem> LichTraiNghiems { get; set; } = new List<LichTraiNghiem>();

    public virtual ICollection<MediaTraiNghiem> MediaTraiNghiems { get; set; } = new List<MediaTraiNghiem>();

    public virtual NguoiDung? NguoiDuyet { get; set; }

    public virtual ICollection<NguonTraLoiAi> NguonTraLoiAis { get; set; } = new List<NguonTraLoiAi>();

    public virtual ICollection<SuKienTuongTac> SuKienTuongTacs { get; set; } = new List<SuKienTuongTac>();
}
