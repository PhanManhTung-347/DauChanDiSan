using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class HuyHieu
{
    public int HuyHieuId { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? AnhPath { get; set; }

    public virtual ICollection<HuyHieuNguoiDung> HuyHieuNguoiDungs { get; set; } = new List<HuyHieuNguoiDung>();

    public virtual ICollection<NhiemVu> NhiemVus { get; set; } = new List<NhiemVu>();
}
