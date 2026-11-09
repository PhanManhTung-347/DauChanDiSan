using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class HanhTrinhCheckIn
{
    public long HanhTrinhId { get; set; }

    public long CheckInId { get; set; }

    public int NguoiDungId { get; set; }

    public virtual CheckInDiSan CheckInDiSan { get; set; } = null!;

    public virtual HanhTrinh HanhTrinh { get; set; } = null!;

    public virtual ICollection<NhatKyHanhTrinh> NhatKyHanhTrinhs { get; set; } = new List<NhatKyHanhTrinh>();
}
