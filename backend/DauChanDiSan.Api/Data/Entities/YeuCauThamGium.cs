using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class YeuCauThamGium
{
    public long YeuCauId { get; set; }

    public Guid MaYeuCau { get; set; }

    public int LichTraiNghiemId { get; set; }

    public int TraiNghiemId { get; set; }

    public int NguoiDungId { get; set; }

    public short SoNguoi { get; set; }

    public decimal? DonGiaDuKien { get; set; }

    public string? GhiChu { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public DateTime? XuLyLucUtc { get; set; }

    public int? NguoiXuLyId { get; set; }

    public virtual ICollection<DanhGiaTraiNghiem> DanhGiaTraiNghiems { get; set; } = new List<DanhGiaTraiNghiem>();

    public virtual LichTraiNghiem LichTraiNghiem { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual NguoiDung? NguoiXuLy { get; set; }
}
