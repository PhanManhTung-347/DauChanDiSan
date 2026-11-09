using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwLichTraiNghiem
{
    public int LichTraiNghiemId { get; set; }

    public int TraiNghiemId { get; set; }

    public DateTime BatDauUtc { get; set; }

    public DateTime KetThucUtc { get; set; }

    public string TrangThai { get; set; } = null!;

    public short SucChua { get; set; }

    public int? SoChoCon { get; set; }
}
