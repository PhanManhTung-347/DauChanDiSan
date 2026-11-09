import { lazy, Suspense, useEffect, useMemo, useState } from 'react'
import { BookOpen, CirclePlus, MapPinned, Sparkles } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../../shared/api'
import type { Experience, Heritage, Page, Trip } from '../../shared/types'
import { Empty, Feedback, LoadState, PageTitle, ProtectedPhoto, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { today, vnDate } from '../../shared/format'
import type { TripMapCheckIn, TripMapPoint } from './TripMap'

const TripMap = lazy(() => import('./TripMap'))

type Point = { ngay: number; thuTu: number; diSanId: number | null; traiNghiemId?: number | null; gio?: string; ghiChu?: string }
type SavedPoint = {
  diSanId: number | null; traiNghiemId?: number | null; ngayDuKien: string; thuTu: number
  ten?: string; gioDuKien?: string; ghiChu?: string; viDo?: number | null; kinhDo?: number | null
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

export function TripsPage() {
  const [search] = useSearchParams()
  const direct = Number(search.get('diSanId')) || null
  const trips = useResource<Page<Trip>>('/hanh-trinh')
  const catalog = useResource<Page<Heritage>>('/di-san?kichThuoc=100')
  const [selected, setSelected] = useState<number>()
  const detail = useResource<TripDetail>(selected ? '/hanh-trinh/' + selected : null)
  const [draft, setDraft] = useState<Point[] | null>(null)
  const [previewStart, setPreviewStart] = useState<string>()
  const action = useAction()
  const trip = detail.data?.hanhTrinh
  const points = useMemo(() => draft ?? (trip ? detail.data!.diem.map(p => ({ ngay: dateDistance(trip.ngayBatDau, p.ngayDuKien) + 1,
    thuTu: p.thuTu, diSanId: p.diSanId, traiNghiemId: p.traiNghiemId, gio: p.gioDuKien, ghiChu: p.ghiChu }))
    : direct ? [{ ngay: 1, thuTu: 1, diSanId: direct }] : []), [draft, trip, detail.data, direct])
  const experiences = useResource<Page<Experience>>(points.some(p => p.traiNghiemId) ? '/trai-nghiem' : null)
  const places = useMemo(() => new Map((catalog.data?.muc ?? []).map(row => [row.diSanId, row])), [catalog.data])
  const start = previewStart ?? trip?.ngayBatDau ?? today()
  const mapPoints = useMemo<TripMapPoint[]>(() => points.flatMap((p, index) => {
    if (!p.diSanId && !p.traiNghiemId) return []
    const original = detail.data?.diem.find(row => p.diSanId
      ? row.diSanId === p.diSanId : row.traiNghiemId === p.traiNghiemId)
    const experience = experiences.data?.muc.find(row => row.traiNghiemId === p.traiNghiemId)
    const place = places.get(p.diSanId ?? experience?.diSanId ?? 0)
    const coordinates = original?.viDo != null && original.kinhDo != null ? original : place
    const day = addDays(start, p.ngay - 1)
    return [{
      ngayDuKien: day, thuTu: index + 1,
      ten: original?.ten ?? experience?.ten ?? place?.ten ?? 'Điểm hành trình',
      viDo: coordinates?.viDo, kinhDo: coordinates?.kinhDo,
      gioDuKien: p.gio, ghiChu: p.ghiChu,
    }]
  }), [points, places, detail.data, experiences.data, start])

  function chooseTrip(id?: number) {
    setSelected(id)
    setDraft(null)
    setPreviewStart(undefined)
  }
  function setPoint(index: number, patch: Partial<Point>) { setDraft(points.map((p, i) => i === index ? { ...p, ...patch } : p)) }

  return <><PageTitle eyebrow="NHỮNG ĐƯỜNG SẼ ĐI" title="Hành trình của tôi" text="Sắp xếp những nơi muốn đến theo từng ngày.">
    <Link className="button outline" to="/tro-ly"><Sparkles size={17} /> Trợ lý hành trình</Link></PageTitle>
    <div className="detail-columns reverse"><section className="surface"><h2>Chuyến đi đã lưu</h2><LoadState {...trips} retry={trips.reload} />
      {trips.data?.muc.map(t => <button className={'queue-row' + (selected === t.hanhTrinhId ? ' selected' : '')} key={t.hanhTrinhId} onClick={() => chooseTrip(t.hanhTrinhId)}>
        <span><strong>{t.tieuDe}</strong><small>{vnDate(t.ngayBatDau)} – {vnDate(t.ngayKetThuc)}</small></span><Status value={t.trangThai} /></button>)}
      {trips.data?.tong === 0 && <Empty title="Chưa có hành trình." />}
      {selected && <div className="inline block"><button className="button outline" onClick={() => chooseTrip()}><CirclePlus size={16} /> Tạo mới</button>
        <Link className="button outline" to={'/so-tay?hanhTrinhId=' + selected}><BookOpen size={16} /> Sổ tay</Link></div>}
    </section><section className="surface"><h2><MapPinned size={22} /> {trip ? 'Chỉnh sửa hành trình' : 'Tạo hành trình'}</h2>
      <LoadState {...detail} retry={detail.reload} />
      {(!selected || trip) && <TripMapSection key={trip?.hanhTrinhId ?? 'new'} points={mapPoints}
        tripId={trip?.hanhTrinhId} checkInIds={detail.data?.checkInIds} places={places} reloadTrip={detail.reload} />}
      {(!selected || trip) && <form key={trip?.hanhTrinhId ?? 'new'} className="form-stack" onSubmit={e => {
        e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
        void action.run(async () => {
          const fields = { tieuDe: f.get('title'), ngayBatDau: f.get('start'), ngayKetThuc: f.get('end'), moTa: f.get('desc'),
            diem: points.map((p, i) => ({ ...p, thuTu: i + 1, gio: p.gio ? (p.gio.length === 5 ? p.gio + ':00' : p.gio) : null })) }
          if (trip) await api('/hanh-trinh/' + trip.hanhTrinhId, 'PUT', { ...fields, phienBan: trip.phienBan, trangThai: f.get('status'),
            anhBiaPath: trip.anhBiaPath ?? null, videoPath: trip.videoPath ?? null })
          else {
            const row = await api<{ hanhTrinhId: number }>('/hanh-trinh', 'POST', fields)
            chooseTrip(row.hanhTrinhId)
          }
          setDraft(null); trips.reload(); detail.reload()
        })
      }}>
        <label>Tên hành trình<input name="title" maxLength={160} defaultValue={trip?.tieuDe} required /></label>
        <div className="form-grid"><label>Bắt đầu<input type="date" name="start" defaultValue={trip?.ngayBatDau ?? today()}
          onChange={event => setPreviewStart(event.target.value)} required /></label>
          <label>Kết thúc<input type="date" name="end" defaultValue={trip?.ngayKetThuc ?? today()} required /></label></div>
        {trip && <label>Trạng thái<select name="status" defaultValue={trip.trangThai}><option value="LEN_KE_HOACH">Đang lên kế hoạch</option>
          <option value="DANG_DI">Đang đi</option><option value="HOAN_THANH">Hoàn thành</option><option value="DA_HUY">Đã hủy</option></select></label>}
        <label>Ghi chú<textarea name="desc" maxLength={1000} rows={3} defaultValue={trip?.moTa} /></label>
        {points.map((p, i) => <div className="point-grid" key={i}><label>Ngày thứ<input type="number" min={1} max={90} value={p.ngay} onChange={e => setPoint(i, { ngay: Number(e.target.value) })} required /></label>
          <label>Điểm đến<select value={p.diSanId ?? ''} disabled={!!p.traiNghiemId} required={!p.traiNghiemId} onChange={e => setPoint(i, { diSanId: Number(e.target.value) || null })}>
            <option value="">{p.traiNghiemId ? 'Trải nghiệm đã chọn' : 'Chọn di sản'}</option>{catalog.data?.muc.map(x => <option key={x.diSanId} value={x.diSanId}>{x.ten}</option>)}</select></label>
          <label>Giờ<input type="time" value={p.gio?.slice(0,5) ?? ''} onChange={e => setPoint(i, { gio: e.target.value })} /></label>
          <button type="button" className="button quiet" onClick={() => setDraft(points.filter((_, index) => index !== i))}>Bỏ</button></div>)}
        <div className="inline"><button className="button outline" type="button" onClick={() => setDraft([...points, { ngay: 1, thuTu: points.length + 1, diSanId: null }])}>+ Thêm điểm đến</button>
          <button className="button primary" disabled={action.busy}>Lưu hành trình</button></div>
      </form>}<Feedback error={action.error} message={action.message} /></section></div></>
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
          <label>Kết thúc<input type="date" name="end" defaultValue={today()} required /></label><button className="button outline" disabled={action.busy}>Lưu đề xuất</button></form>}
      </article>)}
      <form className="form-stack" onSubmit={e => {e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
        void action.run(async () => { await api(`/ai/hoi-thoai/${chat}/tin-nhan`, 'POST', { cauHoi: f.get('question'), soNgay: Number(f.get('days')) }); form.reset(); log.reload() }, '')}}>
        <label>Câu hỏi<textarea name="question" rows={3} required maxLength={2500} placeholder="Gợi ý cho tôi một ngày khám phá Hội An" /></label>
        <label>Số ngày<input type="number" name="days" min={1} max={14} defaultValue={1} required /></label>
        <button className="button primary" disabled={action.busy || !status.data?.daCauHinh}>Gửi câu hỏi</button>
      </form></> : <Empty title="Chọn hoặc tạo một hội thoại." />}</section></div></>
}
