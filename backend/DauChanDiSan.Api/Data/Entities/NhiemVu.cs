using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class NhiemVu
{
    public int NhiemVuId { get; set; }

    public int? HuyHieuId { get; set; }

    public int? DiSanId { get; set; }

    public int? LoaiDiSanId { get; set; }

    public string Loai { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string MoTa { get; set; } = null!;

    public short? SoDiSanCanDat { get; set; }

    public DateTime BatDauUtc { get; set; }

    public DateTime? KetThucUtc { get; set; }

    public bool CongKhai { get; set; }

    public virtual ICollection<CauHoiNhiemVu> CauHoiNhiemVus { get; set; } = new List<CauHoiNhiemVu>();

    public virtual DiSan? DiSan { get; set; }

    public virtual HuyHieu? HuyHieu { get; set; }

    public virtual LoaiDiSan? LoaiDiSan { get; set; }

    public virtual ICollection<TienDoNhiemVu> TienDoNhiemVus { get; set; } = new List<TienDoNhiemVu>();
}
