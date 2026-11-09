using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class ChuDe
{
    public int ChuDeId { get; set; }

    public string Ma { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public virtual ICollection<SoThichNguoiDung> SoThichNguoiDungs { get; set; } = new List<SoThichNguoiDung>();

    public virtual ICollection<DiSan> DiSans { get; set; } = new List<DiSan>();
}
