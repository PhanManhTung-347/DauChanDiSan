using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class CauHoiNhiemVu
{
    public int CauHoiId { get; set; }

    public int NhiemVuId { get; set; }

    public short ThuTu { get; set; }

    public string NoiDung { get; set; } = null!;

    public string? GiaiThich { get; set; }

    public virtual ICollection<LuaChonCauHoi> LuaChonCauHois { get; set; } = new List<LuaChonCauHoi>();

    public virtual NhiemVu NhiemVu { get; set; } = null!;
}
