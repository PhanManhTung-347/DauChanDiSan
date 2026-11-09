using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class PhienBanCsdl
{
    public string PhienBan { get; set; } = null!;

    public DateTime CaiDatLucUtc { get; set; }

    public string GhiChu { get; set; } = null!;
}
