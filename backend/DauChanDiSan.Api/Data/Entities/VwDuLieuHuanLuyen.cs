using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwDuLieuHuanLuyen
{
    public int NguoiDungId { get; set; }

    public string Loai { get; set; } = null!;

    public int? DiSanId { get; set; }

    public int? TraiNghiemId { get; set; }

    public long? LuotGoiYid { get; set; }

    public byte? SoSao { get; set; }

    public DateTime ThoiGianUtc { get; set; }

    public byte? Nhan { get; set; }
}
