using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DiSanYeuThich
{
    public int NguoiDungId { get; set; }

    public int DiSanId { get; set; }

    public DateTime LuuLucUtc { get; set; }

    public virtual DiSan DiSan { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
