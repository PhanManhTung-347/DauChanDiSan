import { useState } from 'react'
import { ArrowRight, CalendarDays, MapPin, Ticket } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api, qs } from '../../shared/api'
import type { Experience, Gallery, Page } from '../../shared/types'
import { Cover, Empty, Feedback, LoadState, MediaGallery, PageTitle, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { money, vnDate } from '../../shared/format'
import { useSession } from '../accounts/session'
import { PartnerExperienceEditor } from './PartnerExperienceEditor'

type Slot = { lichTraiNghiemId: number; batDauUtc: string; ketThucUtc: string; sucChua: number; soChoCon?: number; trangThai: string }
type Booking = { yeuCauId: number; tenTraiNghiem: string; hoTen?: string; batDauUtc: string; soNguoi: number; trangThai: string; ghiChu?: string }
const localTime = (x: string) => new Date(x).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', dateStyle: 'short', timeStyle: 'short' })
export function ExperiencesPage() {
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const list = useResource<Page<Experience>>('/trai-nghiem' + qs({ q: query, trang: page }))
  return <><PageTitle eyebrow="GẶP GỠ CỘNG ĐỒNG" title="Trải nghiệm bản địa" text="Đặt lịch tham gia hoạt động cùng những người đang gìn giữ di sản." />
    <form className="surface inline" onSubmit={e => {e.preventDefault(); setQuery(String(new FormData(e.currentTarget).get('q'))); setPage(1)}}>
      <label>Tìm trải nghiệm<input name="q" type="search" placeholder="Làm gốm, tham quan…" /></label><button className="button primary">Tìm kiếm</button>
    </form>
    <LoadState {...list} retry={list.reload} /><div className="cards three block">{list.data?.muc.map(x => <Link className="place-card" key={x.traiNghiemId} to={'/trai-nghiem/' + x.slug}>
      <div className="place-cover"><Cover slug={x.slug} path={x.anhBiaPath} alt={x.ten} /></div>
      <div className="place-body"><small className="place-location">{x.tenDoiTac ?? 'Trải nghiệm bản địa'}</small><h3>{x.ten}</h3><p>{x.moTa}</p>
        <span className="place-link">{money(x.giaMoiNguoi)} · {x.thoiLuongPhut} phút <ArrowRight size={16} /></span></div></Link>)}</div>
    {list.data?.tong === 0 && <Empty title="Chưa có trải nghiệm phù hợp." />}
    {!!list.data && list.data.tong > (list.data.kichThuoc || 20) && <div className="pager">
      <button className="button outline" disabled={page === 1} onClick={() => setPage(p => p - 1)}>Trang trước</button><span>Trang {page}</span>
      <button className="button outline" disabled={page * (list.data.kichThuoc || 20) >= list.data.tong} onClick={() => setPage(p => p + 1)}>Trang sau</button>
    </div>}
  </>
}

export function ExperiencePage() {
  const { slug } = useParams()
  const { profile } = useSession()
  const detail = useResource<{ traiNghiem: Experience; lich: Slot[]; media: Gallery[] }>('/trai-nghiem/' + slug)
  const action = useAction()
  const [uuid, setUuid] = useState(() => crypto.randomUUID())
  const x = detail.data?.traiNghiem
  return <><Link className="back-link" to="/trai-nghiem">← Tất cả trải nghiệm</Link>
    <PageTitle eyebrow="TRẢI NGHIỆM BẢN ĐỊA" title={x?.ten ?? 'Chi tiết trải nghiệm'} text={x?.tenDoiTac} /><LoadState {...detail} retry={detail.reload} />
    {x && <div className="detail-columns"><section className="surface"><h2>Về hoạt động</h2><p className="prose">{x.moTa}</p>
      <MediaGallery title={x.ten} items={[{ anhPath: x.anhBiaPath }, { videoPath: x.videoPath }, ...(detail.data?.media ?? [])]} />
      <p><MapPin size={16} /> {x.diaDiemTapTrung || 'Xem hướng dẫn từ đối tác sau khi xác nhận.'}</p>
      <p>{x.thoiLuongPhut} phút · {money(x.giaMoiNguoi)}/người</p>
      <h2>Lịch mở</h2>{detail.data?.lich.map(slot => <p key={slot.lichTraiNghiemId}><CalendarDays size={15} /> {localTime(slot.batDauUtc)} · {slot.soChoCon ?? slot.sucChua} chỗ</p>)}
      {detail.data?.lich.length === 0 && <Empty title="Chưa có lịch nhận khách." />}
    </section><aside className="surface"><h2><Ticket size={22} /> Đăng ký tham gia</h2>
      {profile ? <form className="form-stack" onSubmit={e => {e.preventDefault(); const f = new FormData(e.currentTarget)
        void action.run(async () => { await api('/yeu-cau-tham-gia', 'POST', { lichTraiNghiemId: Number(f.get('slot')), soNguoi: Number(f.get('people')), maYeuCau: uuid, ghiChu: f.get('note') }); setUuid(crypto.randomUUID()); detail.reload() }, 'Đã gửi yêu cầu đặt chỗ.') }}>
        <label>Chọn lịch<select name="slot" required><option value="">Chọn thời gian</option>{detail.data?.lich.filter(s => (s.soChoCon ?? s.sucChua) > 0).map(s => <option key={s.lichTraiNghiemId} value={s.lichTraiNghiemId}>{localTime(s.batDauUtc)}</option>)}</select></label>
        <label>Số người<input name="people" type="number" min={1} max={500} defaultValue={1} required /></label>
        <label>Ghi chú<textarea name="note" maxLength={500} rows={3} /></label>
        <button className="button primary" disabled={action.busy || !detail.data?.lich.length}>Gửi yêu cầu</button>
        <Link className="text-link" to="/yeu-cau-tham-gia">Xem các yêu cầu của tôi</Link><Feedback error={action.error} message={action.message} />
      </form> : <Link to="/dang-nhap" state={{ from: '/trai-nghiem/' + slug }} className="button primary">Đăng nhập để đặt chỗ</Link>}
    </aside></div>}
  </>
}

export function BookingsPage() {
  const list = useResource<Booking[]>('/yeu-cau-tham-gia/toi')
  const action = useAction()
  return <><PageTitle eyebrow="TRẢI NGHIỆM ĐÃ CHỌN" title="Yêu cầu tham gia" /><LoadState {...list} retry={list.reload} />
    <Feedback error={action.error} message={action.message} />
    {list.data?.map(b => <article className="surface record" key={b.yeuCauId}><Status value={b.trangThai} /><h2>{b.tenTraiNghiem}</h2>
      <p>{localTime(b.batDauUtc)} · {b.soNguoi} người</p>
      {['CHO_XAC_NHAN','DA_XAC_NHAN'].includes(b.trangThai) && <button className="button outline" disabled={action.busy} onClick={() => {
        if (confirm('Hủy yêu cầu tham gia này?')) void action.run(async () => {await api(`/yeu-cau-tham-gia/${b.yeuCauId}/trang-thai`, 'PUT', { trangThaiMoi: 'DA_HUY' });list.reload()}, 'Đã hủy yêu cầu.')
      }}>Hủy yêu cầu</button>}</article>)}
    {list.data?.length === 0 && <Empty title="Chưa gửi yêu cầu tham gia." />}
  </>
}

type Partner = { doiTacId: number; ten: string }
export function PartnerPage() {
  const partners = useResource<Partner[]>('/doi-tac/toi')
  const [selected, setSelected] = useState<number>()
  const id = selected ?? partners.data?.[0]?.doiTacId
  const requests = useResource<Booking[]>(id ? `/doi-tac/${id}/yeu-cau` : null)
  const experiences = useResource<Experience[]>(id ? `/doi-tac/${id}/trai-nghiem` : null)
  const action = useAction()
  return <><PageTitle eyebrow="KHÔNG GIAN ĐỐI TÁC" title="Lịch và khách tham gia" />
    <LoadState {...partners} retry={partners.reload} />
    {partners.data?.length === 0 && <Empty title="Tài khoản chưa được gắn vào đối tác." />}
    {!!partners.data?.length && <div className="surface"><label>Đối tác<select value={id ?? ''} onChange={e => setSelected(Number(e.target.value))}>{partners.data.map(p =>
      <option value={p.doiTacId} key={p.doiTacId}>{p.ten}</option>)}</select></label></div>}
    {id && <PartnerExperienceEditor key={id} id={id} experiences={experiences.data} reload={experiences.reload} />}
    {id && <div className="detail-columns reverse block"><section className="surface"><h2>Yêu cầu của khách</h2><LoadState {...requests} retry={requests.reload} />
      {requests.data?.map(b => <article className="record" key={b.yeuCauId}><Status value={b.trangThai} /><h3>{b.tenTraiNghiem}</h3><p>{b.hoTen} · {vnDate(b.batDauUtc)} · {b.soNguoi} khách</p>
        <div className="inline">{(b.trangThai === 'CHO_XAC_NHAN' ? [['DA_XAC_NHAN','Xác nhận'],['TU_CHOI','Từ chối']]
          : b.trangThai === 'DA_XAC_NHAN' ? [['DA_THAM_GIA','Đã tham gia'],['DA_HUY','Hủy']] : []).map(([value, label]) =>
          <button className="button outline" key={value} disabled={action.busy} onClick={() => void action.run(async () => { await api(`/yeu-cau-tham-gia/${b.yeuCauId}/trang-thai`, 'PUT', { trangThaiMoi: value }); requests.reload() })}>{label}</button>)}</div></article>)}
      {requests.data?.length === 0 && <Empty title="Chưa có yêu cầu." />}</section>
    <section className="surface"><h2>Thêm lịch trải nghiệm</h2><LoadState {...experiences} retry={experiences.reload} />
      {experiences.data?.length===0 && <p>Hãy tạo trải nghiệm để có thể thêm lịch.</p>}
      {!!experiences.data?.length && <form className="form-stack" onSubmit={e => {e.preventDefault(); const f = new FormData(e.currentTarget)
        void action.run(() => api(`/doi-tac/${id}/trai-nghiem/${f.get('experience')}/lich`, 'POST', {
          batDauUtc: new Date(String(f.get('start')) + ':00+07:00').toISOString(), ketThucUtc: new Date(String(f.get('end')) + ':00+07:00').toISOString(),
          sucChua: Number(f.get('capacity')), trangThai: 'MO' }), 'Đã thêm lịch mở.') }}>
        <label>Trải nghiệm<select name="experience" required><option value="">Chọn trải nghiệm</option>{experiences.data?.map(x => <option value={x.traiNghiemId} key={x.traiNghiemId}>{x.ten}</option>)}</select></label>
        <label>Bắt đầu (giờ Việt Nam)<input type="datetime-local" name="start" required /></label><label>Kết thúc (giờ Việt Nam)<input type="datetime-local" name="end" required /></label>
        <label>Sức chứa<input type="number" name="capacity" min={1} max={500} defaultValue={10} required /></label>
        <button className="button primary" disabled={action.busy}>Thêm lịch</button>
      </form>}</section></div>}
    <Feedback error={action.error} message={action.message} />
  </>
}
