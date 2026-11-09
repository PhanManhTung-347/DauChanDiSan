using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwNguCanhGemini
{
    public string? LoaiNguon { get; set; }

    public int NguonId { get; set; }

    public int DiSanId { get; set; }

    public int TinhThanhId { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public string? UrlNguon { get; set; }
}
