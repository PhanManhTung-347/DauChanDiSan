using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class ChiTietGoiY
{
    public long LuotGoiYid { get; set; }

    public short ThuHang { get; set; }

    public int? DiSanId { get; set; }

    public int? TraiNghiemId { get; set; }

    public decimal Diem { get; set; }

    public string? LyDo { get; set; }

    public virtual DiSan? DiSan { get; set; }

    public virtual LuotGoiY LuotGoiY { get; set; } = null!;

    public virtual ICollection<SuKienTuongTac> SuKienTuongTacs { get; set; } = new List<SuKienTuongTac>();

    public virtual TraiNghiem? TraiNghiem { get; set; }
}
