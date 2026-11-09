using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class HanhTrinh
{
    public long HanhTrinhId { get; set; }

    public int NguoiDungId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public long? TinNhanAiid { get; set; }

    public string VaiTinNhanAi { get; set; } = null!;

    public DateTime TaoLucUtc { get; set; }

    public byte[] PhienBan { get; set; } = null!;

    public virtual ICollection<DiemHanhTrinh> DiemHanhTrinhs { get; set; } = new List<DiemHanhTrinh>();

    public virtual ICollection<HanhTrinhCheckIn> HanhTrinhCheckIns { get; set; } = new List<HanhTrinhCheckIn>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<NhatKyHanhTrinh> NhatKyHanhTrinhs { get; set; } = new List<NhatKyHanhTrinh>();

    public virtual TinNhanAi? TinNhanAi { get; set; }
}
