using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class GioThamQuan
{
    public int GioThamQuanId { get; set; }

    public int DiSanId { get; set; }

    public byte ThuTrongTuan { get; set; }

    public TimeOnly GioMo { get; set; }

    public TimeOnly GioDong { get; set; }

    public virtual DiSan DiSan { get; set; } = null!;
}
