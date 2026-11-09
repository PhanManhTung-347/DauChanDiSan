using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class NguoiDungVaiTro
{
    public int NguoiDungId { get; set; }

    public string MaVaiTro { get; set; } = null!;

    public DateTime CapLucUtc { get; set; }

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
