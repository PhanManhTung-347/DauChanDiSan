using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class HuyHieuNguoiDung
{
    public int NguoiDungId { get; set; }

    public int HuyHieuId { get; set; }

    public int NhiemVuId { get; set; }

    public DateTime NhanLucUtc { get; set; }

    public virtual HuyHieu HuyHieu { get; set; } = null!;

    public virtual TienDoNhiemVu TienDoNhiemVu { get; set; } = null!;
}
