using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DanhGiaDiSan
{
    public int NguoiDungId { get; set; }

    public int DiSanId { get; set; }

    public long CheckInId { get; set; }

    public byte SoSao { get; set; }

    public string? NoiDung { get; set; }

    public string? AnhPath { get; set; }

    public string? VideoPath { get; set; }

    public bool CongKhai { get; set; }

    public DateTime TaoLucUtc { get; set; }

    public virtual CheckInDiSan CheckInDiSan { get; set; } = null!;
}
