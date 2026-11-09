using System;
using System.Collections.Generic;

namespace DauChanDiSan.Api.Data.Entities;

public partial class NguoiDung
{
    public int NguoiDungId { get; set; }

    public string FirebaseUid { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? Email { get; set; }

    public bool EmailDaXacMinh { get; set; }

    public string? AnhDaiDienPath { get; set; }

    public bool HoatDong { get; set; }

    public bool ChoPhepCaNhanHoa { get; set; }

    public DateTime TaoLucUtc { get; set; }

    public DateTime LanDongBoUtc { get; set; }

    public virtual ICollection<CheckInDiSan> CheckInDiSans { get; set; } = new List<CheckInDiSan>();

    public virtual ICollection<DiSanYeuThich> DiSanYeuThiches { get; set; } = new List<DiSanYeuThich>();

    public virtual ICollection<HanhTrinh> HanhTrinhs { get; set; } = new List<HanhTrinh>();

    public virtual ICollection<HoiThoaiAi> HoiThoaiAis { get; set; } = new List<HoiThoaiAi>();

    public virtual ICollection<KetQuaTraLoi> KetQuaTraLois { get; set; } = new List<KetQuaTraLoi>();

    public virtual ICollection<LuotGoiY> LuotGoiYs { get; set; } = new List<LuotGoiY>();

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();

    public virtual ICollection<SoThichNguoiDung> SoThichNguoiDungs { get; set; } = new List<SoThichNguoiDung>();

    public virtual ICollection<SuKienTuongTac> SuKienTuongTacs { get; set; } = new List<SuKienTuongTac>();

    public virtual ICollection<ThanhVienDoiTac> ThanhVienDoiTacs { get; set; } = new List<ThanhVienDoiTac>();

    public virtual ICollection<TienDoNhiemVu> TienDoNhiemVus { get; set; } = new List<TienDoNhiemVu>();

    public virtual ICollection<TraiNghiem> TraiNghiems { get; set; } = new List<TraiNghiem>();

    public virtual ICollection<TuLieuDiSan> TuLieuDiSans { get; set; } = new List<TuLieuDiSan>();

    public virtual ICollection<YeuCauThamGium> YeuCauThamGiumNguoiDungs { get; set; } = new List<YeuCauThamGium>();

    public virtual ICollection<YeuCauThamGium> YeuCauThamGiumNguoiXuLies { get; set; } = new List<YeuCauThamGium>();
}
