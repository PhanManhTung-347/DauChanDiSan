export type Page<T> = { tong: number; trang: number; kichThuoc: number; muc: T[] }
export type Role = 'THANH_VIEN' | 'QUAN_TRI' | 'KIEM_DUYET_VAN_HOA' | 'DOI_TAC'
export type Profile = { nguoiDungId: number; hoTen: string; email?: string; emailDaXacMinh: boolean; choPhepCaNhanHoa: boolean; anhDaiDienPath?: string; vaiTro: Role[] }
export type Heritage = {
  diSanId: number; slug: string; ten: string; tomTat: string; noiDung?: string;
  tinhThanh: string; tinhThanhId: number; loaiDiSan: string; loaiDiSanId: number;
  viDo: number; kinhDo: number; diaChi: string; anhBiaPath?: string; videoPath?: string;
  khoangCachM?: number; banKinhCheckInM?: number; saiSoGpsToiDaM?: number
}
export type Gallery = { mediaId?: number; anhPath?: string; videoPath?: string; chuThich?: string }
export type HeritageDetail = { diSan: Heritage; gioThamQuan: { gioThamQuanId: number; thuTrongTuan: number; gioMo: string; gioDong: string }[];
  media: Gallery[]; tuLieu: { tuLieuId: number; tieuDe: string; trichYeu: string; nguon: string; urlNguon?: string }[] }
export type Catalog = {
  tinhThanh: { tinhThanhId: number; ten: string }[];
  loaiDiSan: { loaiDiSanId: number; ten: string }[];
  chuDe: { chuDeId: number; ten: string }[]
}
export type Experience = { traiNghiemId: number; slug: string; ten: string; moTa: string; tenDoiTac?: string;
  thoiLuongPhut: number; giaMoiNguoi?: number; diaDiemTapTrung?: string; trangThai?: string; diSanId: number; anhBiaPath?: string; videoPath?: string }
export type Trip = { hanhTrinhId: number; tieuDe: string; ngayBatDau: string; ngayKetThuc: string; trangThai: string; moTa?: string; phienBan?: string; anhBiaPath?: string; videoPath?: string }
export type Contribution = { dongGopId: number; nguoiDungId: number; loai: string; tieuDe: string; noiDung: string; trangThai: string; lyDo?: string;
  anhPath?: string; diaChi?: string; viDo?: number; kinhDo?: number; diSanId?: number; diSanCongKhaiId?: number; tinhThanhId?: number;
  loaiDiSanId?: number; urlNguon?: string; taoLucUtc: string }
