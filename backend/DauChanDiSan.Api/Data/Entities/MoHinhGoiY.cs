using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class MoHinhGoiY
{
    public int MoHinhId { get; set; }

    public string Ma { get; set; } = null!;

    public string PhienBan { get; set; } = null!;

    public string ThuatToan { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public DateTime? HuanLuyenLucUtc { get; set; }

    public string? ChiSoJson { get; set; }

    public string? TepMoHinhPath { get; set; }

    public bool HoatDong { get; set; }

    public virtual ICollection<LuotGoiY> LuotGoiYs { get; set; } = new List<LuotGoiY>();
}
