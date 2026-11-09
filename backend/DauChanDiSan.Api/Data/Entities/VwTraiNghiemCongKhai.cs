using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class VwTraiNghiemCongKhai
{
    public int TraiNghiemId { get; set; }

    public int DiSanId { get; set; }

    public int DoiTacId { get; set; }

    public string Slug { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string MoTa { get; set; } = null!;

    public string? DiaDiemTapTrung { get; set; }

    public short ThoiLuongPhut { get; set; }

    public decimal? GiaMoiNguoi { get; set; }

    public string? AnhBiaPath { get; set; }

    public string? VideoPath { get; set; }

    public string TenDoiTac { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public int TinhThanhId { get; set; }
}
