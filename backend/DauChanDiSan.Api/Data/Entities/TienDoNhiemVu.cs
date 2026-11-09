using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class TienDoNhiemVu
{
    public int NguoiDungId { get; set; }

    public int NhiemVuId { get; set; }

    public int SoDaDat { get; set; }

    public int MucTieu { get; set; }

    public DateTime CapNhatLucUtc { get; set; }

    public DateTime? HoanThanhLucUtc { get; set; }

    public virtual ICollection<HuyHieuNguoiDung> HuyHieuNguoiDungs { get; set; } = new List<HuyHieuNguoiDung>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual NhiemVu NhiemVu { get; set; } = null!;
}
