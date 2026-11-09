using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwNhatKyCongKhai
{
    public Guid MaChiaSe { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public DateOnly NgayGhi { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public string TenTacGia { get; set; } = null!;
}
