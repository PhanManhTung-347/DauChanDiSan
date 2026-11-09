using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class TuLieuDiSan
{
    public int TuLieuId { get; set; }

    public int DiSanId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string TrichYeu { get; set; } = null!;

    public string Nguon { get; set; } = null!;

    public string? UrlNguon { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime? DuyetLucUtc { get; set; }

    public bool CongKhai { get; set; }

    public virtual DiSan DiSan { get; set; } = null!;

    public virtual NguoiDung? NguoiDuyet { get; set; }

    public virtual ICollection<NguonTraLoiAi> NguonTraLoiAis { get; set; } = new List<NguonTraLoiAi>();
}
