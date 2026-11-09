using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class LuotGoiY
{
    public long LuotGoiYid { get; set; }

    public int NguoiDungId { get; set; }

    public int MoHinhId { get; set; }

    public string ViTriHienThi { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public virtual ICollection<ChiTietGoiY> ChiTietGoiYs { get; set; } = new List<ChiTietGoiY>();

    public virtual MoHinhGoiY MoHinh { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<SuKienTuongTac> SuKienTuongTacs { get; set; } = new List<SuKienTuongTac>();
}
