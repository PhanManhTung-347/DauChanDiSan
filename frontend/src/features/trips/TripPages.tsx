import { lazy, Suspense, useEffect, useMemo, useState } from 'react'
import { BedDouble, BookOpen, CalendarDays, CirclePlus, Landmark, Sparkles, Utensils } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { api, qs } from '../../shared/api'
import type { Heritage, Page, Trip } from '../../shared/types'
import { Empty, Feedback, LoadState, PageTitle, ProtectedPhoto, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { money, today, vnDate } from '../../shared/format'
import type { TripMapCheckIn, TripMapPoint } from './TripMap'

import './chat-trip.css'

const TripMap = lazy(() => import('./TripMap'))

type StopKind = 'DI_SAN' | 'TRAI_NGHIEM' | 'LUU_TRU' | 'AN_UONG'
type StopOption = {
  id: number; loai: StopKind; ten: string; diaChi?: string | null
  viDo?: number | null; kinhDo?: number | null; giaVnd?: number | null
  donViTinh?: string | null; laDuLieuDemo?: boolean
}
type Stop = { key: string; ngay: number; option: StopOption; gio: string; ghiChu: string; soLuong: number }
type SavedPoint = {
  diemHanhTrinhId: number; diSanId?: number | null; traiNghiemId?: number | null; dichVuId?: number | null
  loai?: StopKind; ngayDuKien: string; thuTu: number; ten?: string | null; diaChi?: string | null
  gioDuKien?: string | null; ghiChu?: string | null; viDo?: number | null; kinhDo?: number | null
  donGiaVnd?: number | null; donViTinhGia?: string | null; soLuongTinhGia?: number | null; laDuLieuDemo?: boolean
}
type TripDetail = { hanhTrinh: Trip; diem: SavedPoint[]; checkInIds?: number[] }
type TripCheckIn = TripMapCheckIn & { diSanId: number; ghiChu?: string | null }
type Journal = {
  nhatKyId: number; checkInId?: number | null; tieuDe: string; ngayGhi: string
  noiDung?: string | null; anhBiaPath?: string | null
}

function dateDistance(first: string, second: string) {
  return Math.round((Date.parse(second) - Date.parse(first)) / 86400000)
}

function addDays(date: string, days: number) {
  const time = Date.parse(date)
  if (!Number.isFinite(time) || !Number.isInteger(days) || days < 0 || days >= 90) return ''
  return new Date(time + days * 86400000).toISOString().slice(0, 10)
}

// Tải từng nhóm nhỏ, giữ các kết quả đã tải được khi một mục gặp lỗi.
function useDetails<T>(paths: string[]) {
  const key = paths.join('|')
  const [version, setVersion] = useState(0)
  const [state, setState] = useState<{ key: string; rows: T[]; partial: boolean }>()

  useEffect(() => {
    if (!key) return
    const abort = new AbortController()
    const urls = key.split('|')
    void (async () => {
      const rows: T[] = []
      let partial = false
      for (let start = 0; start < urls.length && !abort.signal.aborted; start += 6) {
        const batch = await Promise.allSettled(urls.slice(start, start + 6)
          .map(path => api<T>(path, 'GET', undefined, abort.signal)))
        for (const result of batch) {
          if (result.status === 'fulfilled') rows.push(result.value)
          else partial = true
        }
      }
      if (!abort.signal.aborted) setState({ key, rows, partial })
    })()
    return () => abort.abort()
  }, [key, version])

  return {
    rows: key && key === state?.key ? state.rows : undefined,
    loading: !!key && key !== state?.key,
    error: key && key === state?.key && state.partial ? 'Một số mục chưa tải được.' : undefined,
    reload: () => setVersion(value => value + 1),
  }
}

function TripMapSection({ points, tripId, checkInIds, places, reloadTrip }: {
  points: TripMapPoint[]; tripId?: number; checkInIds?: number[]
  places: ReadonlyMap<number, Heritage>; reloadTrip: () => void
}) {
  const [selectedCheckIn, setSelectedCheckIn] = useState<number>()
  const [showAttach, setShowAttach] = useState(false)
  const [historyPage, setHistoryPage] = useState(1)
  const details = useDetails<TripCheckIn>((checkInIds ?? []).map(id => '/check-in/' + id))
  const history = useResource<Page<TripCheckIn>>(showAttach && tripId ? '/check-in?trang=' + historyPage : null)
  const journalList = useResource<Journal[]>(selectedCheckIn && tripId ? '/so-tay/hanh-trinh/' + tripId : null)
  const journals = useDetails<{ journal: Journal }>((journalList.data ?? [])
    .filter(item => item.checkInId === undefined || item.checkInId === selectedCheckIn)
    .map(item => '/so-tay/' + item.nhatKyId))
  const attach = useAction()
  const checkIns = useMemo(() => (details.rows ?? []).map(row => {
    if (row.viDo != null && row.kinhDo != null) return row
    const place = places.get(row.diSanId)
    return place ? { ...row, viDo: place.viDo, kinhDo: place.kinhDo, toaDoThamChieu: true } : row
  }), [details.rows, places])
  const opened = checkIns.find(row => row.checkInId === selectedCheckIn)
  const linkedJournals = (journals.rows ?? []).map(row => row.journal)
    .filter(row => row.checkInId === selectedCheckIn)
  const available = history.data?.muc.filter(row => !checkInIds?.includes(row.checkInId)) ?? []

  return <div className="form-stack">
    <Suspense fallback={<LoadState loading />}>
      <TripMap points={points} checkIns={checkIns} onOpenCheckIn={setSelectedCheckIn} />
    </Suspense>
    <LoadState {...details} retry={details.reload} />
    {checkIns.length > 0 && <div className="inline" aria-label="Danh sách check-in đã gắn">
      {checkIns.map(row => <button type="button" className="button outline" key={row.checkInId}
        aria-pressed={selectedCheckIn === row.checkInId} onClick={() => setSelectedCheckIn(row.checkInId)}>
        {row.tenDiSan} · {vnDate(row.ngayDiaPhuong ?? row.thoiGianUtc)}
      </button>)}
    </div>}
    {opened && <article className="record" aria-label="Chi tiết check-in">
      <div className="inline">
        <strong>{opened.tenDiSan}</strong>
        <button type="button" className="button quiet" onClick={() => setSelectedCheckIn(undefined)}>Đóng</button>
      </div>
      <small>{vnDate(opened.ngayDiaPhuong ?? opened.thoiGianUtc)}
        {opened.toaDoThamChieu ? ' · Vị trí tham chiếu tại di sản' : ''}</small>
      {opened.ghiChu && <p className="prose">{opened.ghiChu}</p>}
      {opened.anhPath && !opened.anhPath.startsWith('demo/') &&
        <ProtectedPhoto path={opened.anhPath} alt={opened.tenDiSan} />}
      <LoadState {...journalList} retry={journalList.reload} />
      <LoadState {...journals} retry={journals.reload} />
      {linkedJournals.map(note => <section className="record" key={note.nhatKyId}>
        <small>Sổ tay · {vnDate(note.ngayGhi)}</small><h4>{note.tieuDe}</h4>
        {note.noiDung && <p className="prose">{note.noiDung}</p>}
        {note.anhBiaPath && !note.anhBiaPath.startsWith('demo/') &&
          <ProtectedPhoto path={note.anhBiaPath} alt={note.tieuDe} />}
      </section>)}
    </article>}
    {tripId && <details onToggle={event => setShowAttach(event.currentTarget.open)}>
      <summary>Gắn check-in đã lưu vào hành trình</summary>
      <LoadState {...history} retry={history.reload} />
      {showAttach && history.data && <div className="form-stack">
        {available.length === 0 && <small className="muted">Trang này chưa có check-in khác để gắn.</small>}
        {available.map(row => <div className="inline" key={row.checkInId}>
          <span>{row.tenDiSan} · {vnDate(row.ngayDiaPhuong ?? row.thoiGianUtc)}</span>
          <button type="button" className="button outline" disabled={attach.busy}
            onClick={() => void attach.run(async () => {
              await api('/check-in/' + row.checkInId + '/hanh-trinh/' + tripId, 'PUT')
              reloadTrip()
            }, 'Đã gắn check-in vào hành trình.')}>Gắn</button>
        </div>)}
        {history.data.tong > history.data.kichThuoc && <div className="inline">
          <button type="button" className="button quiet" disabled={historyPage <= 1}
            onClick={() => setHistoryPage(page => page - 1)}>Trang trước</button>
          <button type="button" className="button quiet" disabled={historyPage * history.data.kichThuoc >= history.data.tong}
            onClick={() => setHistoryPage(page => page + 1)}>Trang sau</button>
        </div>}
      </div>}
      <Feedback error={attach.error} message={attach.message} />
    </details>}
  </div>
}

const kinds = [
  { id: 'DI_SAN', name: 'Di sản', icon: Landmark },
  { id: 'TRAI_NGHIEM', name: 'Trải nghiệm', icon: Sparkles },
  { id: 'LUU_TRU', name: 'Khách sạn / Homestay', icon: BedDouble },
  { id: 'AN_UONG', name: 'Ăn uống', icon: Utensils },
] as const
const units: Record<string, string> = { NGUOI_LUOT: 'người/lượt', PHONG_DEM: 'phòng/đêm', NGUOI_BUA: 'người/bữa' }
const newStop = (option: StopOption, ngay: number): Stop =>
  ({ key: crypto.randomUUID(), ngay, option, gio: '', ghiChu: '', soLuong: 1 })

function savedStop(point: SavedPoint, start: string): Stop[] {
  const id = point.diSanId ?? point.traiNghiemId ?? point.dichVuId
  if (id == null) return []
  return [{
    key: String(point.diemHanhTrinhId), ngay: dateDistance(start, point.ngayDuKien) + 1,
    option: {
      id, loai: point.loai ?? (point.diSanId ? 'DI_SAN' : point.traiNghiemId ? 'TRAI_NGHIEM' : 'LUU_TRU'),
      ten: point.ten ?? 'Điểm đến đã lưu', diaChi: point.diaChi,
      viDo: point.viDo, kinhDo: point.kinhDo, giaVnd: point.donGiaVnd, donViTinh: point.donViTinhGia,
      laDuLieuDemo: point.laDuLieuDemo,
    },
    gio: point.gioDuKien?.slice(0, 5) ?? '', ghiChu: point.ghiChu ?? '',
    soLuong: point.soLuongTinhGia ?? 1,
  }]
}

function TripEditor({ detail, direct, places, onSaved, reloadTrip }: {
  detail?: TripDetail; direct: number | null; places: ReadonlyMap<number, Heritage>
  onSaved: (id: number) => void; reloadTrip: () => void
}) {
  const trip = detail?.hanhTrinh
  const [start, setStart] = useState(trip?.ngayBatDau ?? today())
  const [end, setEnd] = useState(trip?.ngayKetThuc ?? today())
  const [draft, setDraft] = useState<Stop[]>()
  const [kind, setKind] = useState<StopKind>('DI_SAN')
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [day, setDay] = useState(1)
  const [editing, setEditing] = useState(!trip)
  const action = useAction()
  const options = useResource<{ muc: StopOption[]; coTrangSau: boolean }>(
    editing ? '/hanh-trinh/danh-muc-diem' + qs({ loai: kind, q: query, trang: page }) : null)
  const directPlace = direct === null ? undefined : places.get(direct)

  const initial = useMemo(() => {
    if (detail) return detail.diem.flatMap(point => savedStop(point, detail.hanhTrinh.ngayBatDau))
    if (!directPlace) return []
    return [{ key: 'initial-' + directPlace.diSanId, ngay: 1, gio: '', ghiChu: '', soLuong: 1,
      option: { id: directPlace.diSanId, loai: 'DI_SAN', ten: directPlace.ten,
        diaChi: directPlace.diaChi, viDo: directPlace.viDo, kinhDo: directPlace.kinhDo } } satisfies Stop]
  }, [detail, directPlace])
  const stops = draft ?? initial
  const duration = Math.max(1, Math.min(90, (dateDistance(start, end) || 0) + 1))
  const activeDay = Math.min(day, duration)
  const ordered = useMemo(() => [...stops].sort((a, b) => a.ngay - b.ngay ||
    (a.gio || '99:99').localeCompare(b.gio || '99:99')), [stops])
  const mapPoints = useMemo<TripMapPoint[]>(() => ordered.map((stop, index) => ({
    ngayDuKien: addDays(start, stop.ngay - 1), thuTu: index + 1,
    ten: stop.option.ten, gioDuKien: stop.gio, ghiChu: stop.ghiChu,
    viDo: stop.option.viDo, kinhDo: stop.option.kinhDo,
  })), [ordered, start])
  const total = stops.reduce((sum, p) => sum + (p.option.giaVnd ?? 0) * p.soLuong, 0)

  function patchStop(key: string, patch: Partial<Stop>) {
    setDraft(stops.map(point => point.key === key ? { ...point, ...patch } : point))
  }

  return <div className="dcds-trip-content">
    <div className="dcds-trip-hero">
      <div><p className="eyebrow">KẾ HOẠCH CHUYẾN ĐI</p><h2>{trip?.tieuDe ?? 'Chuyến đi mới'}</h2>
        <p>{vnDate(start)} – {vnDate(end)} · {duration} ngày · {stops.length} điểm</p></div>
      {trip && <div className="inline"><Status value={trip.trangThai} />
        <Link className="button outline" to={'/so-tay?hanhTrinhId=' + trip.hanhTrinhId}><BookOpen size={16} /> Sổ tay</Link>
        <button type="button" className="button outline" onClick={() => setEditing(value => !value)}>
          {editing ? 'Ẩn danh mục' : 'Thêm điểm đến'}
        </button></div>}
    </div>

    <div className="dcds-trip-overview">
      <div><small>Thời lượng</small><strong>{duration} ngày</strong></div>
      <div><small>Điểm đã xếp</small><strong>{stops.length}</strong></div>
      <div><small>Tạm tính các điểm có giá</small><strong>{total.toLocaleString('vi-VN')} đ</strong>
        <small>{stops.filter(p => p.option.giaVnd == null).length} điểm chưa có giá</small></div>
    </div>

    <form className="surface form-stack dcds-trip-card" onSubmit={event => {
      event.preventDefault()
      const fields = new FormData(event.currentTarget)
      void action.run(async () => {
        const days = dateDistance(start, end) + 1
        if (!Number.isInteger(days) || days < 1 || days > 90) throw new Error('Hành trình phải từ 1 đến 90 ngày.')
        if (ordered.some(p => !Number.isInteger(p.ngay) || p.ngay < 1 || p.ngay > days))
          throw new Error('Có điểm nằm ngoài khoảng ngày của chuyến đi.')
        if (ordered.some(p => p.option.giaVnd != null && (!Number.isInteger(p.soLuong) || p.soLuong < 1 || p.soLuong > 10000)))
          throw new Error('Số lượng của từng điểm có giá phải từ 1 đến 10.000.')
        const dayOrders = new Map<number, number>()
        const diem = ordered.map(({ ngay, option, gio, ghiChu, soLuong }) => {
          const thuTu = (dayOrders.get(ngay) ?? 0) + 1
          dayOrders.set(ngay, thuTu)
          if (thuTu > 100 || ordered.length > 300) throw new Error('Chuyến đi có quá nhiều điểm.')
          const priced = option.giaVnd != null && !!option.donViTinh
          return {
            ngay, thuTu, diSanId: option.loai === 'DI_SAN' ? option.id : null,
            traiNghiemId: option.loai === 'TRAI_NGHIEM' ? option.id : null,
            dichVuId: ['LUU_TRU', 'AN_UONG'].includes(option.loai) ? option.id : null,
            gio: gio ? gio + ':00' : null, ghiChu: ghiChu.trim() || null,
            donGiaVnd: priced ? option.giaVnd : null, soLuongTinhGia: priced ? soLuong : null,
            donViTinhGia: priced ? option.donViTinh : null,
          }
        })
        const data = { tieuDe: String(fields.get('title') ?? '').trim(), moTa: fields.get('desc'),
          ngayBatDau: start, ngayKetThuc: end, diem }
        if (!data.tieuDe) throw new Error('Bạn hãy nhập tên hành trình.')
        const id = trip?.hanhTrinhId
        const row = id
          ? await api<{ hanhTrinhId: number }>('/hanh-trinh/' + id, 'PUT', {
            ...data, phienBan: trip.phienBan, trangThai: fields.get('status'),
            anhBiaPath: trip.anhBiaPath ?? null, videoPath: trip.videoPath ?? null,
          })
          : await api<{ hanhTrinhId: number }>('/hanh-trinh', 'POST', data)
        onSaved(row.hanhTrinhId)
      }, 'Đã lưu hành trình.')
    }}>
      <h3>Thông tin chuyến đi</h3>
      <div className="form-grid">
        <label>Tên hành trình<input name="title" maxLength={160} defaultValue={trip?.tieuDe} required /></label>
        {trip && <label>Trạng thái<select name="status" defaultValue={trip.trangThai}>
          <option value="LEN_KE_HOACH">Đang lên kế hoạch</option><option value="DANG_DI">Đang đi</option>
          <option value="HOAN_THANH">Hoàn thành</option><option value="DA_HUY">Đã hủy</option>
        </select></label>}
        <label>Bắt đầu<input type="date" value={start} required onChange={event => setStart(event.target.value)} /></label>
        <label>Kết thúc<input type="date" value={end} min={start} required onChange={event => setEnd(event.target.value)} /></label>
      </div>
      <label>Ghi chú chung<textarea name="desc" maxLength={1000} rows={2} defaultValue={trip?.moTa} /></label>

      <div className="dcds-trip-section-title"><h3>Lịch theo ngày</h3>
        <label>Ngày đang xem<select value={activeDay} onChange={event => setDay(Number(event.target.value))}>
          {Array.from({ length: duration }, (_, i) => <option key={i} value={i + 1}>Ngày {i + 1} · {vnDate(addDays(start, i))}</option>)}
        </select></label>
      </div>
      {stops.some(p => p.ngay > duration) && <Feedback error="Có điểm nằm sau ngày kết thúc. Hãy sửa lại ngày kết thúc hoặc số ngày của các điểm." />}
      {ordered.every(p => p.ngay !== activeDay) && <p className="muted">Ngày này chưa có điểm đến.</p>}
      <ol className="dcds-trip-timeline">
        {ordered.filter(p => p.ngay === activeDay || p.ngay > duration).map(stop => {
          const group = kinds.find(item => item.id === stop.option.loai) ?? kinds[0]
          const Icon = group.icon
          return <li key={stop.key}>
            <span className={'dcds-stop-icon ' + stop.option.loai}><Icon size={18} /></span>
            <article className="dcds-stop-card">
              <small>{group.name}{stop.option.laDuLieuDemo ? ' · DEMO' : ''}</small>
              <strong>{stop.option.ten}</strong>
              {stop.option.diaChi && <small>{stop.option.diaChi}</small>}
              <div className="form-grid">
                <label>Ngày thứ<select value={stop.ngay} onChange={event => {
                  const value = Number(event.target.value)
                  patchStop(stop.key, { ngay: value })
                  setDay(value)
                }}>
                  {stop.ngay > duration && <option value={stop.ngay}>Ngày {stop.ngay} · ngoài lịch</option>}
                  {Array.from({ length: duration }, (_, i) => <option key={i} value={i + 1}>Ngày {i + 1}</option>)}
                </select></label>
                <label>Giờ<input type="time" value={stop.gio} onChange={event => patchStop(stop.key, { gio: event.target.value })} /></label>
                {stop.option.giaVnd != null && <label>
                  Số lượng ({units[stop.option.donViTinh ?? ''] ?? 'lượt'})
                  <input type="number" min={1} max={10000} required value={stop.soLuong}
                    onChange={event => patchStop(stop.key, { soLuong: Number(event.target.value) })} />
                </label>}
              </div>
              <label>Ghi chú điểm đến<input maxLength={500} value={stop.ghiChu}
                onChange={event => patchStop(stop.key, { ghiChu: event.target.value })} /></label>
              <div className="dcds-trip-section-title">
                <small>{stop.option.giaVnd == null ? 'Chưa có giá' : money(stop.option.giaVnd * stop.soLuong)}</small>
                <button type="button" className="button quiet" onClick={() => setDraft(stops.filter(p => p.key !== stop.key))}>Bỏ điểm</button>
              </div>
            </article>
          </li>
        })}
      </ol>
      <Feedback error={action.error} message={action.message} />
      <button className="button primary" disabled={action.busy}>{action.busy ? 'Đang lưu…' : 'Lưu hành trình'}</button>
    </form>

    {editing && <section className="surface form-stack dcds-trip-card" aria-label="Danh mục điểm đến">
      <h3>Thêm điểm đến cho ngày {activeDay}</h3>
      <div className="dcds-trip-tabs" aria-label="Loại điểm">
        {kinds.map(group => <button type="button" key={group.id} aria-pressed={kind === group.id}
          onClick={() => { setKind(group.id); setPage(1) }}>{group.name}</button>)}
      </div>
      <form className="dcds-trip-search" key={kind} onSubmit={event => {
        event.preventDefault()
        setQuery(String(new FormData(event.currentTarget).get('q') ?? '').trim())
        setPage(1)
      }}>
        <input name="q" defaultValue={query} maxLength={100} aria-label="Tên hoặc địa chỉ" placeholder="Tìm theo tên hoặc địa chỉ…" />
        <button type="submit" className="button outline">Tìm</button>
      </form>
      <LoadState {...options} retry={options.reload} />
      {options.data?.muc.length === 0 && <p className="muted">Chưa tìm thấy điểm phù hợp.</p>}
      <div className="dcds-trip-options">
        {options.data?.muc.map(option => <article key={option.id}>
          <strong>{option.ten}{option.laDuLieuDemo ? ' · DEMO' : ''}</strong>
          {option.diaChi && <small>{option.diaChi}</small>}
          <small>{option.giaVnd == null ? 'Chưa có giá' :
            money(option.giaVnd) + ' / ' + (units[option.donViTinh ?? ''] ?? 'lượt')}</small>
          <button type="button" className="button outline" onClick={() => setDraft([...stops, newStop(option, activeDay)])}>
            <CirclePlus size={16} /> Thêm ngày {activeDay}
          </button>
        </article>)}
      </div>
      {(page > 1 || options.data?.coTrangSau) && <div className="inline">
        <button type="button" className="button quiet" disabled={page <= 1} onClick={() => setPage(page - 1)}>Trang trước</button>
        <span>Trang {page}</span>
        <button type="button" className="button quiet" disabled={!options.data?.coTrangSau} onClick={() => setPage(page + 1)}>Trang sau</button>
      </div>}
    </section>}

    <section className="surface dcds-trip-card">
      <TripMapSection points={mapPoints} tripId={trip?.hanhTrinhId}
        checkInIds={detail?.checkInIds} places={places} reloadTrip={reloadTrip} />
    </section>
  </div>
}

export function TripsPage() {
  const [search, setSearch] = useSearchParams()
  const selected = Number(search.get('hanhTrinhId')) || null
  const direct = Number(search.get('diSanId')) || null
  const trips = useResource<Page<Trip>>('/hanh-trinh')
  const detail = useResource<TripDetail>(selected ? '/hanh-trinh/' + selected : null)
  const catalog = useResource<Page<Heritage>>('/di-san?kichThuoc=100')
  const places = useMemo(() => new Map((catalog.data?.muc ?? []).map(row => [row.diSanId, row])), [catalog.data])
  const [revision, setRevision] = useState(0)

  return <><PageTitle eyebrow="NHỮNG ĐƯỜNG SẼ ĐI" title="Hành trình của tôi"
    text="Sắp xếp điểm tham quan, chỗ ăn và chỗ ở theo từng ngày.">
    <Link className="button outline" to="/tro-ly"><Sparkles size={17} /> Trợ lý hành trình</Link>
  </PageTitle>
  <div className="dcds-trip-layout">
    <aside className="surface dcds-trip-sidebar">
      <div className="dcds-trip-section-title"><h2>Chuyến đi đã lưu</h2>
        <button type="button" className="button outline" onClick={() => { setSearch({}); setRevision(value => value + 1) }}>
          <CirclePlus size={17} /> Tạo mới
        </button></div>
      <LoadState {...trips} retry={trips.reload} />
      {trips.data?.tong === 0 && <Empty title="Bạn chưa có hành trình." />}
      {trips.data?.muc.map(trip => <button type="button" className={'dcds-trip-choice' + (selected === trip.hanhTrinhId ? ' active' : '')}
        key={trip.hanhTrinhId} aria-pressed={selected === trip.hanhTrinhId}
        onClick={() => setSearch({ hanhTrinhId: String(trip.hanhTrinhId) })}>
        <strong>{trip.tieuDe}</strong>
        <small><CalendarDays size={14} /> {vnDate(trip.ngayBatDau)} – {vnDate(trip.ngayKetThuc)}</small>
        <Status value={trip.trangThai} />
      </button>)}
    </aside>
    <div>
      <LoadState {...detail} retry={detail.reload} />
      {(!selected || detail.data) && <TripEditor key={(selected ?? 'new') + '-' + (direct ?? '') + '-' + revision}
        detail={detail.data} direct={direct} places={places} reloadTrip={detail.reload} onSaved={id => {
          if (selected !== id) setSearch({ hanhTrinhId: String(id) })
          else {
            // Làm mới phiên bản và form sau khi API trả kết quả.
            detail.reload()
          }
          trips.reload()
        }} />}
    </div>
  </div></>
}

function suggestedEnd(value: string) {
  try {
    const plan = JSON.parse(value) as { diem?: { ngay: number }[] }
    const days = Math.max(1, ...(plan.diem ?? []).map(point => point.ngay))
    return addDays(today(), days - 1) || today()
  } catch { return today() }
}

type Chat = { hoiThoaiId: number; tieuDe: string; daLuuTru?: boolean }
type Message = { tinNhanAiId: number; vai: string; noiDung: string; deXuatHanhTrinhJson?: string; nguon: { tieuDeKhiTrich: string; urlKhiTrich?: string }[] }
export function AssistantPage() {
  const status = useResource<{ daCauHinh: boolean }>('/ai/trang-thai')
  const chats = useResource<Chat[]>('/ai/hoi-thoai')
  const [chat, setChat] = useState<number>()
  const log = useResource<{ hoiThoai: Chat; tinNhan: Message[] }>(chat ? '/ai/hoi-thoai/' + chat : null)
  const action = useAction()
  return <><PageTitle eyebrow="SỔ TAY ĐƯỜNG ĐI" title="Trợ lý hành trình" text="Tìm gợi ý từ tư liệu di sản và lưu thành kế hoạch." />
    {status.data && !status.data.daCauHinh && <p className="feedback">Trợ lý chưa được cấu hình. Bạn có thể mở hội thoại mẫu và lưu đề xuất có sẵn.</p>}
    <div className="detail-columns reverse"><section className="surface"><h2>Hội thoại</h2><LoadState {...chats} retry={chats.reload} />
      <form className="form-stack" onSubmit={e => {e.preventDefault(); const f = new FormData(e.currentTarget)
        void action.run(async () => { const row = await api<Chat>('/ai/hoi-thoai', 'POST', { tieuDe: f.get('title') }); setChat(row.hoiThoaiId); chats.reload() })}}>
        <label>Tên hội thoại mới<input name="title" maxLength={160} required /></label><button className="button outline" disabled={action.busy}>Tạo hội thoại</button>
      </form>
      {chats.data?.map(x => <button key={x.hoiThoaiId} className={'queue-row' + (chat === x.hoiThoaiId ? ' selected' : '')} onClick={() => setChat(x.hoiThoaiId)}>{x.tieuDe}</button>)}
    </section><section className="surface"><LoadState {...log} retry={log.reload} /><Feedback error={action.error} message={action.message} />
      {chat ? <><h2>{log.data?.hoiThoai.tieuDe ?? 'Nội dung hội thoại'}</h2>{log.data?.tinNhan.map(m => <article className="record" key={m.tinNhanAiId}>
        <small>{m.vai === 'ASSISTANT' ? 'TRỢ LÝ' : 'BẠN'}</small><p className="prose">{m.noiDung}</p>
        {m.nguon.map((n,i) => <p key={i}>{n.urlKhiTrich && /^https?:\/\//.test(n.urlKhiTrich) ? <a href={n.urlKhiTrich} target="_blank" rel="noreferrer">{n.tieuDeKhiTrich}</a> : n.tieuDeKhiTrich}</p>)}
        {m.deXuatHanhTrinhJson && <form className="inline" onSubmit={e => {e.preventDefault(); const f = new FormData(e.currentTarget)
          void action.run(() => api('/hanh-trinh', 'POST', { tinNhanAiId: m.tinNhanAiId, tieuDe: f.get('title'), ngayBatDau: f.get('start'), ngayKetThuc: f.get('end') }), 'Đã thêm vào hành trình.')}}>
          <label>Tên hành trình<input name="title" defaultValue={log.data?.hoiThoai.tieuDe} required /></label><label>Bắt đầu<input type="date" name="start" defaultValue={today()} required /></label>
          <label>Kết thúc<input type="date" name="end" defaultValue={suggestedEnd(m.deXuatHanhTrinhJson)} required /></label><button className="button outline" disabled={action.busy}>Lưu đề xuất</button></form>}
      </article>)}
      <form className="form-stack" onSubmit={e => {e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
        void action.run(async () => { await api(`/ai/hoi-thoai/${chat}/tin-nhan`, 'POST', { cauHoi: f.get('question'), soNgay: Number(f.get('days')) }); form.reset(); log.reload() }, '')}}>
        <label>Câu hỏi<textarea name="question" rows={3} required maxLength={2500} placeholder="Gợi ý cho tôi một ngày khám phá Hội An" /></label>
        <label>Số ngày<input type="number" name="days" min={1} max={14} defaultValue={1} required /></label>
        <button className="button primary" disabled={action.busy || !status.data?.daCauHinh}>Gửi câu hỏi</button>
      </form></> : <Empty title="Chọn hoặc tạo một hội thoại." />}</section></div></>
}