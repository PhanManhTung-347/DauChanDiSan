using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class ThanhVienDoiTac
{
    public int DoiTacId { get; set; }

    public int NguoiDungId { get; set; }

    public string Quyen { get; set; } = null!;

    public bool HoatDong { get; set; }

    public virtual DoiTac DoiTac { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
