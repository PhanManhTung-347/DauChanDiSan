using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwDiSanCongKhai
{
    public int DiSanId { get; set; }

    public string Slug { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string TomTat { get; set; } = null!;

    public string? NoiDung { get; set; }

    public string DiaChi { get; set; } = null!;

    public int TinhThanhId { get; set; }

    public string TinhThanh { get; set; } = null!;

    public int LoaiDiSanId { get; set; }

    public string LoaiDiSan { get; set; } = null!;

    public decimal ViDo { get; set; }

    public decimal KinhDo { get; set; }

    public short BanKinhCheckInM { get; set; }

    public short SaiSoGpsToiDaM { get; set; }

    public decimal? GiaVeThamKhao { get; set; }

    public short? ThoiLuongPhut { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }
}
