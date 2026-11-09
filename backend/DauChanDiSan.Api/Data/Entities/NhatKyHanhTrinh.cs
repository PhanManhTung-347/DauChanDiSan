using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class NhatKyHanhTrinh
{
    public long NhatKyId { get; set; }

    public long HanhTrinhId { get; set; }

    public long? CheckInId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public DateOnly NgayGhi { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public Guid MaChiaSe { get; set; }

    public bool CongKhai { get; set; }

    public DateTime TaoLucUtc { get; set; }

    public byte[] PhienBan { get; set; } = null!;

    public virtual HanhTrinh HanhTrinh { get; set; } = null!;

    public virtual HanhTrinhCheckIn? HanhTrinhCheckIn { get; set; }

    public virtual ICollection<MediaNhatKy> MediaNhatKies { get; set; } = new List<MediaNhatKy>();
}
