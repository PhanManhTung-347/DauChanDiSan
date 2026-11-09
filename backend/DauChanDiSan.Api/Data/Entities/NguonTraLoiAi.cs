using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class NguonTraLoiAi
{
    public long TinNhanAiid { get; set; }

    public int NguoiDungId { get; set; }

    public string Vai { get; set; } = null!;

    public short ThuTu { get; set; }

    public int? DiSanId { get; set; }

    public int? TraiNghiemId { get; set; }

    public int? TuLieuId { get; set; }

    public string TieuDeKhiTrich { get; set; } = null!;

    public string? UrlKhiTrich { get; set; }

    public virtual DiSan? DiSan { get; set; }

    public virtual TinNhanAi TinNhanAi { get; set; } = null!;

    public virtual TraiNghiem? TraiNghiem { get; set; }

    public virtual TuLieuDiSan? TuLieu { get; set; }
}
