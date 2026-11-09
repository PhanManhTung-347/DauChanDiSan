using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class CheckInDiSan
{
    public long CheckInId { get; set; }

    public int NguoiDungId { get; set; }

    public int DiSanId { get; set; }

    public Guid MaYeuCau { get; set; }

    public DateTime ThoiGianUtc { get; set; }

    public DateOnly? NgayDiaPhuong { get; set; }

    public decimal ViDo { get; set; }

    public decimal KinhDo { get; set; }

    public decimal SaiSoGpsM { get; set; }

    public decimal TamViDo { get; set; }

    public decimal TamKinhDo { get; set; }

    public short BanKinhApDungM { get; set; }

    public short SaiSoToiDaApDungM { get; set; }

    public decimal KhoangCachM { get; set; }

    public string? GhiChu { get; set; }

    public string? AnhPath { get; set; }

    public string? VideoPath { get; set; }

    public virtual ICollection<DanhGiaDiSan> DanhGiaDiSans { get; set; } = new List<DanhGiaDiSan>();

    public virtual DiSan DiSan { get; set; } = null!;

    public virtual ICollection<HanhTrinhCheckIn> HanhTrinhCheckIns { get; set; } = new List<HanhTrinhCheckIn>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
