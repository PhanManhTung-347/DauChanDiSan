using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VaiTro
{
    public string MaVaiTro { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
}
