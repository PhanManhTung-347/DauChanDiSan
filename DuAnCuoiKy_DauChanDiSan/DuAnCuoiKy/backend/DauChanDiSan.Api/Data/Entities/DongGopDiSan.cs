namespace DauChanDiSan.Api.Data.Entities;

public sealed class DongGopDiSan
{
    public long DongGopId { get; set; }
    public int NguoiDungId { get; set; }
    public Guid MaYeuCau { get; set; }
    public string Loai { get; set; } = null!;
    public int? DiSanId { get; set; }
    public int? TinhThanhId { get; set; }
    public int? LoaiDiSanId { get; set; }
    public string TieuDe { get; set; } = null!;
    public string NoiDung { get; set; } = null!;
    public string? DiaChi { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }
    public string? AnhPath { get; set; }
    public string? UrlNguon { get; set; }
    public string TrangThai { get; set; } = null!;
    public string? LyDo { get; set; }
    public int? NguoiDuyetId { get; set; }
    public DateTime? DuyetLucUtc { get; set; }
    public int? DiSanCongKhaiId { get; set; }
    public int? TuLieuId { get; set; }
    public DateTime TaoLucUtc { get; set; }
    public DateTime CapNhatLucUtc { get; set; }
    public byte[] PhienBan { get; set; } = null!;
}
