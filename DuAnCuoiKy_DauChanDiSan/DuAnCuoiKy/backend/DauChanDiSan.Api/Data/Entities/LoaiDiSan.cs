using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class LoaiDiSan
{
    public int LoaiDiSanId { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<DiSan> DiSans { get; set; } = new List<DiSan>();

    public virtual ICollection<NhiemVu> NhiemVus { get; set; } = new List<NhiemVu>();
}
