using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DanhGiaTraiNghiem
{
    public int NguoiDungId { get; set; }

    public int TraiNghiemId { get; set; }

    public long YeuCauId { get; set; }

    public string TrangThaiYeuCau { get; set; } = null!;

    public byte SoSao { get; set; }

    public string? NoiDung { get; set; }

    public string? AnhPath { get; set; }

    public string? VideoPath { get; set; }

    public bool CongKhai { get; set; }

    public DateTime TaoLucUtc { get; set; }

    public virtual YeuCauThamGium YeuCauThamGium { get; set; } = null!;
}
