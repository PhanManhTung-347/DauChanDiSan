using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class SuKienTuongTac
{
    public long SuKienId { get; set; }

    public Guid MaSuKien { get; set; }

    public int NguoiDungId { get; set; }

    public string Loai { get; set; } = null!;

    public int? DiSanId { get; set; }

    public int? TraiNghiemId { get; set; }

    public long? LuotGoiYid { get; set; }

    public short? ThuHang { get; set; }

    public byte? SoSao { get; set; }

    public DateTime ThoiGianUtc { get; set; }

    public virtual ChiTietGoiY? ChiTietGoiY { get; set; }

    public virtual DiSan? DiSan { get; set; }

    public virtual LuotGoiY? LuotGoiY { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual TraiNghiem? TraiNghiem { get; set; }
}
