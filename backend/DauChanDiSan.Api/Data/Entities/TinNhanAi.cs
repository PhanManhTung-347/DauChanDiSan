using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class TinNhanAi
{
    public long TinNhanAiid { get; set; }

    public long HoiThoaiId { get; set; }

    public int NguoiDungId { get; set; }

    public string Vai { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? NhaCungCap { get; set; }

    public string? MaMoHinh { get; set; }

    public int? TokenVao { get; set; }

    public int? TokenRa { get; set; }

    public string? DeXuatHanhTrinhJson { get; set; }

    public DateTime TaoLucUtc { get; set; }

    public virtual ICollection<HanhTrinh> HanhTrinhs { get; set; } = new List<HanhTrinh>();

    public virtual HoiThoaiAi HoiThoaiAi { get; set; } = null!;

    public virtual ICollection<NguonTraLoiAi> NguonTraLoiAis { get; set; } = new List<NguonTraLoiAi>();
}
