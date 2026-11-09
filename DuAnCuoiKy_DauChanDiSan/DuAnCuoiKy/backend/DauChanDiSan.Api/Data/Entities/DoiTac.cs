using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class DoiTac
{
    public int DoiTacId { get; set; }

    public int TinhThanhId { get; set; }

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? AnhDaiDienPath { get; set; }

    public bool HoatDong { get; set; }

    public virtual ICollection<ThanhVienDoiTac> ThanhVienDoiTacs { get; set; } = new List<ThanhVienDoiTac>();

    public virtual TinhThanh TinhThanh { get; set; } = null!;

    public virtual ICollection<TraiNghiem> TraiNghiems { get; set; } = new List<TraiNghiem>();
}
