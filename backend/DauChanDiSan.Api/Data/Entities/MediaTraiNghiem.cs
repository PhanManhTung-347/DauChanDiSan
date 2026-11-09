using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class MediaTraiNghiem
{
    public int MediaId { get; set; }

    public int TraiNghiemId { get; set; }

    public string? AnhPath { get; set; }

    public string? VideoPath { get; set; }

    public string? ChuThich { get; set; }

    public short ThuTu { get; set; }

    public virtual TraiNghiem TraiNghiem { get; set; } = null!;
}
