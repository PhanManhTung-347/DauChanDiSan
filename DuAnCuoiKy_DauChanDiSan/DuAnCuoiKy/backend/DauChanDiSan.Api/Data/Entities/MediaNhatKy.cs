using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class MediaNhatKy
{
    public long MediaId { get; set; }

    public long NhatKyId { get; set; }

    public string? AnhPath { get; set; }

    public string? VideoPath { get; set; }

    public string? ChuThich { get; set; }

    public short ThuTu { get; set; }

    public virtual NhatKyHanhTrinh NhatKy { get; set; } = null!;
}
