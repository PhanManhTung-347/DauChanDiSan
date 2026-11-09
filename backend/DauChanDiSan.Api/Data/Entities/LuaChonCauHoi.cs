using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class LuaChonCauHoi
{
    public int LuaChonId { get; set; }

    public int CauHoiId { get; set; }

    public byte ThuTu { get; set; }

    public string NoiDung { get; set; } = null!;

    public bool Dung { get; set; }

    public virtual CauHoiNhiemVu CauHoi { get; set; } = null!;

    public virtual ICollection<KetQuaTraLoi> KetQuaTraLois { get; set; } = new List<KetQuaTraLoi>();
}
