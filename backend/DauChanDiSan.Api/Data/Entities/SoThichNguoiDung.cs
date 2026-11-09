using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class SoThichNguoiDung
{
    public int NguoiDungId { get; set; }

    public int ChuDeId { get; set; }

    public byte MucDo { get; set; }

    public virtual ChuDe ChuDe { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
