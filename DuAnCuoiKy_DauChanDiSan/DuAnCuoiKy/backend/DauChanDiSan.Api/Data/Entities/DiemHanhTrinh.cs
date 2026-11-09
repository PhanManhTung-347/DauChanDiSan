using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DiemHanhTrinh
{
    public long DiemHanhTrinhId { get; set; }

    public long HanhTrinhId { get; set; }

    public DateOnly NgayDuKien { get; set; }

    public short ThuTu { get; set; }

    public int? DiSanId { get; set; }

    public int? TraiNghiemId { get; set; }

    public TimeOnly? GioDuKien { get; set; }

    public string? GhiChu { get; set; }

    public virtual DiSan? DiSan { get; set; }

    public virtual HanhTrinh HanhTrinh { get; set; } = null!;

    public virtual TraiNghiem? TraiNghiem { get; set; }
}
