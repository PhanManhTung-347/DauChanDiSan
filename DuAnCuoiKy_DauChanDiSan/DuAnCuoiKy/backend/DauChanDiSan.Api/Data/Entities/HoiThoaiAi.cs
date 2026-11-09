using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class HoiThoaiAi
{
    public long HoiThoaiId { get; set; }

    public int NguoiDungId { get; set; }

    public string TieuDe { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public bool DaLuuTru { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<TinNhanAi> TinNhanAis { get; set; } = new List<TinNhanAi>();
}
