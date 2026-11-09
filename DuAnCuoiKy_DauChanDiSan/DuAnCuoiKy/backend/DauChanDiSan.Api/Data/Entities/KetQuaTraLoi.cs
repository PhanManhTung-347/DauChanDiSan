using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class KetQuaTraLoi
{
    public long KetQuaId { get; set; }

    public int NguoiDungId { get; set; }

    public int CauHoiId { get; set; }

    public int LuaChonId { get; set; }

    public bool DungKhiNop { get; set; }

    public DateTime NopLucUtc { get; set; }

    public virtual LuaChonCauHoi LuaChonCauHoi { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
