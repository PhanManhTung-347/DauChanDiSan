using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class LichTraiNghiem
{
    public int LichTraiNghiemId { get; set; }

    public int TraiNghiemId { get; set; }

    public DateTime BatDauUtc { get; set; }

    public DateTime KetThucUtc { get; set; }

    public short SucChua { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual TraiNghiem TraiNghiem { get; set; } = null!;

    public virtual ICollection<YeuCauThamGium> YeuCauThamGia { get; set; } = new List<YeuCauThamGium>();
}
