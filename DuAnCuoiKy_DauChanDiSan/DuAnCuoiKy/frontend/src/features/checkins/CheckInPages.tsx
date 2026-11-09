import { useState } from 'react'
import { Camera, LocateFixed, Share2 } from 'lucide-react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api, upload } from '../../shared/api'
import type { Gallery, Heritage, Page, Trip } from '../../shared/types'
import { Empty, Feedback, LoadState, MediaGallery, PageTitle, ProtectedPhoto } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { today, vnDate } from '../../shared/format'
import { position } from './position'

type CheckIn = { checkInId: number; diSanId: number; tenDiSan: string; ngayDiaPhuong: string; khoangCachM: number; ghiChu?: string; anhPath?: string }
export function CheckInPage() {
  const [search] = useSearchParams()
  const [heritage, setHeritage] = useState(search.get('diSanId') ?? '')
  const [coordinates, setCoordinates] = useState<GeolocationCoordinates>()
  const [uuid, setUuid] = useState(() => crypto.randomUUID())
  const [photo, setPhoto] = useState<File>()
  const [uploaded, setUploaded] = useState<{ photo: File; path: string }>()
  const places = useResource<Page<Heritage>>('/di-san?kichThuoc=100')
  const history = useResource<Page<CheckIn>>('/check-in')
  const locate = useAction()
  const action = useAction()
  return <><PageTitle eyebrow="DẤU CHÂN CỦA TÔI" title="Check-in di sản" text="Ghi nhận chuyến ghé thăm bằng GPS của thiết bị." />
    <div className="detail-columns reverse"><section className="surface"><h2><Camera size={22} /> Ghi dấu chuyến đi</h2>
      <form className="form-stack" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
        void action.run(async () => {
          if (!coordinates) throw new Error('Hãy lấy vị trí GPS trước khi check-in.')
          let path: string | undefined
          if (photo) {
            path = uploaded?.photo === photo ? uploaded.path : await upload(photo)
            setUploaded({ photo, path })
          }
          await api('/check-in', 'POST', { diSanId: Number(f.get('heritage')), maYeuCau: uuid, viDo: coordinates.latitude,
            kinhDo: coordinates.longitude, saiSoGpsM: coordinates.accuracy, ghiChu: f.get('note'), anhPath: path })
          form.reset(); setUuid(crypto.randomUUID()); setPhoto(undefined); setUploaded(undefined); history.reload()
        }, 'Đã lưu dấu chân mới.')
      }}>
        <label>Địa điểm<select name="heritage" required value={heritage} onChange={e => setHeritage(e.target.value)}><option value="">Chọn di sản</option>
          {places.data?.muc.map(p => <option key={p.diSanId} value={p.diSanId}>{p.ten}</option>)}</select></label>
        <button className="button outline" type="button" disabled={locate.busy} onClick={() => void locate.run(async () => setCoordinates(await position()), 'Đã nhận vị trí GPS.')}>
          <LocateFixed size={17} /> Lấy vị trí hiện tại</button>
        {coordinates && <p className="muted">{coordinates.latitude.toFixed(5)}, {coordinates.longitude.toFixed(5)} · sai số {Math.round(coordinates.accuracy)} m</p>}
        <Feedback error={locate.error} message={locate.message} />
        <label>Ghi chú<textarea name="note" maxLength={500} rows={3} /></label>
        <label>Ảnh chuyến đi<input type="file" accept="image/*" onChange={e => {setPhoto(e.target.files?.[0]);setUploaded(undefined)}} /></label>
        <button className="button primary" disabled={action.busy}>Xác nhận check-in</button>
        <Feedback error={action.error} message={action.message} />
      </form></section>
    <section className="surface"><h2>Lịch sử check-in</h2><LoadState {...history} retry={history.reload} />
      {history.data?.muc.map(c => <article className="record" key={c.checkInId}><small>{vnDate(c.ngayDiaPhuong)} · {Math.round(c.khoangCachM)} m</small><h3>{c.tenDiSan}</h3><p>{c.ghiChu}</p>
        {c.anhPath && !c.anhPath.startsWith('demo/') && <ProtectedPhoto path={c.anhPath} alt={c.tenDiSan} />}</article>)}
      {history.data?.tong === 0 && <Empty title="Chưa có check-in." />}</section></div>
  </>
}

type Journal = { nhatKyId: number; tieuDe: string; noiDung?: string; ngayGhi: string; maChiaSe?: string; congKhai: boolean; anhBiaPath?: string; videoPath?: string }
export function JournalsPage() {
  const trips = useResource<Page<Trip>>('/hanh-trinh')
  const checkins = useResource<Page<CheckIn>>('/check-in')
  const [search] = useSearchParams()
  const [chosenTrip, setTrip] = useState<string>()
  const requestedTrip = search.get('hanhTrinhId')
  const trip = chosenTrip ?? requestedTrip ?? (trips.data?.muc[0] ? String(trips.data.muc[0].hanhTrinhId) : '')
  const journals = useResource<Journal[]>(trip ? '/so-tay/hanh-trinh/' + trip : null)
  const [selected, setSelected] = useState<Journal>()
  const info = useResource<{ journal: Journal }>(selected ? '/so-tay/' + selected.nhatKyId : null)
  const action = useAction()
  return <><PageTitle eyebrow="KỶ NIỆM CHUYẾN ĐI" title="Sổ tay hành trình" text="Một nơi để giữ lại những điều bạn đã thấy." />
    <LoadState {...trips} retry={trips.reload} />
    {trips.data?.tong === 0 && <Empty title="Sổ tay của bạn đang chờ chuyến đi đầu tiên."><Link to="/hanh-trinh">Tạo hành trình</Link></Empty>}
    {((trips.data?.muc.length ?? 0) > 0 || !!requestedTrip) && <div className="surface inline"><label>Hành trình<select value={trip} onChange={e => {setTrip(e.target.value);setSelected(undefined)}}><option value="">Chọn hành trình</option>
      {requestedTrip && !trips.data?.muc.some(x => String(x.hanhTrinhId) === requestedTrip) && <option value={requestedTrip}>Hành trình từ hồ sơ</option>}
      {trips.data?.muc.map(x => <option value={x.hanhTrinhId} key={x.hanhTrinhId}>{x.tieuDe}</option>)}</select></label>
      <Link className="text-link" to="/hanh-trinh">Quản lý hành trình</Link></div>}
    {trip && <div className="detail-columns reverse block"><section className="surface"><h2>{selected ? 'Sửa ghi chép' : 'Tạo ghi chép'}</h2>
      {(!selected || info.data) && <form key={selected?.nhatKyId ?? 'new'} className="form-stack" onSubmit={e => {
        e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
        void action.run(async () => {
          const file = f.get('image')
          const path = file instanceof File && file.size ? await upload(file) : info.data?.journal.anhBiaPath ?? null
          const fields = { tieuDe: f.get('title'), noiDung: f.get('text'), ngayGhi: f.get('date'), anhBiaPath: path, videoPath: info.data?.journal.videoPath ?? null }
          await api(selected ? '/so-tay/' + selected.nhatKyId : '/so-tay/hanh-trinh/' + trip, selected ? 'PUT' : 'POST',
            selected ? fields : { ...fields, checkInId: Number(f.get('checkIn')) || null })
          form.reset(); setSelected(undefined); journals.reload()
        })
      }}>
        <label>Tiêu đề<input name="title" defaultValue={info.data?.journal.tieuDe} required maxLength={160} /></label>
        <label>Ngày ghi<input name="date" type="date" required defaultValue={info.data?.journal.ngayGhi ?? today()} /></label>
        {!selected && <label>Gắn check-in<select name="checkIn"><option value="">Tùy chọn</option>
          {checkins.data?.muc.map(c => <option key={c.checkInId} value={c.checkInId}>{c.tenDiSan} · {vnDate(c.ngayDiaPhuong)}</option>)}</select></label>}
        <label>Nội dung<textarea name="text" rows={6} defaultValue={info.data?.journal.noiDung} required /></label>
        <label>Ảnh bìa<input name="image" type="file" accept="image/*" /></label>
        <button className="button primary" disabled={action.busy}>Lưu ghi chép</button>
        {selected && <button type="button" className="button outline" onClick={() => setSelected(undefined)}>Tạo ghi chép mới</button>}
      </form>}
      <LoadState {...info} retry={info.reload} /><Feedback error={action.error} message={action.message} /></section>
      <section className="surface"><h2>Ghi chép của tôi</h2><LoadState {...journals} retry={journals.reload} />
        {journals.data?.map(j => <article className="record" key={j.nhatKyId}><small>{vnDate(j.ngayGhi)} · {j.congKhai ? 'Công khai' : 'Riêng tư'}</small><h3>{j.tieuDe}</h3>
          <div className="inline"><button className="button quiet" onClick={() => setSelected(j)}>Sửa</button>
          <button className="button outline" disabled={action.busy} onClick={() => void action.run(async () => {
            await api(`/so-tay/${j.nhatKyId}/chia-se`, 'PUT', { congKhai: !j.congKhai })
            journals.reload()
          }, j.congKhai ? 'Đã chuyển sang riêng tư.' : 'Đã công khai ghi chép.')}><Share2 size={15} /> {j.congKhai ? 'Chuyển riêng tư' : 'Công khai'}</button>
          {j.congKhai && j.maChiaSe && <Link className="button outline" to={'/so-tay/chia-se/' + j.maChiaSe}>Mở trang chia sẻ</Link>}
          <button className="button quiet" disabled={action.busy} onClick={() => {
            if (confirm('Xóa ghi chép này?')) void action.run(async () => { await api('/so-tay/' + j.nhatKyId, 'DELETE'); if (selected?.nhatKyId === j.nhatKyId) setSelected(undefined); journals.reload() })
          }}>Xóa</button></div>
        </article>)}{journals.data?.length === 0 && <Empty title="Chưa có ghi chép nào." />}</section></div>}
  </>
}

export function SharedJournalPage() {
  const { code } = useParams()
  const entry = useResource<{ journal: Journal; media: Gallery[] }>('/so-tay/chia-se/' + code)
  const journal = entry.data?.journal
  return <><PageTitle eyebrow="TRANG SỔ TAY ĐƯỢC CHIA SẺ" title={journal?.tieuDe ?? 'Ghi chép di sản'} />
    <LoadState {...entry} retry={entry.reload} />{journal && <article className="surface prose"><p>{vnDate(journal.ngayGhi)}</p>
      <p>{journal.noiDung}</p>
      <MediaGallery title={journal.tieuDe} items={[{ anhPath: journal.anhBiaPath }, { videoPath: journal.videoPath }, ...(entry.data?.media ?? [])]} />
    </article>}</>
}
