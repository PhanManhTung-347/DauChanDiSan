using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class TinhThanh
{
    public int TinhThanhId { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public bool HoatDong { get; set; }

    public virtual ICollection<DiSan> DiSans { get; set; } = new List<DiSan>();

    public virtual ICollection<DoiTac> DoiTacs { get; set; } = new List<DoiTac>();
}
