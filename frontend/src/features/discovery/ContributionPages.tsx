import { useState } from 'react'
import type { FormEvent } from 'react'
import { Camera, CirclePlus, LocateFixed, ScrollText } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { api, qs, upload } from '../../shared/api'
import type { Catalog, Contribution, Heritage, Page } from '../../shared/types'
import { Empty, Feedback, LoadState, PageTitle, ProtectedPhoto, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { vnDate } from '../../shared/format'
import { position } from '../checkins/position'

export function ContributionsPage() {
  const [params] = useSearchParams()
  const target = params.get('diSanId')
  const [type, setType] = useState(target ? 'BO_SUNG' : 'DI_SAN_MOI')
  const [heritage, setHeritage] = useState(target ?? '')
  const [coordinates, setCoordinates] = useState({ lat: '', lng: '' })
  const [image, setImage] = useState<File | null>(null)
  const [uploaded, setUploaded] = useState<{ image: File; path: string }>()
  const [requestId, setRequestId] = useState(() => crypto.randomUUID())
  const catalog = useResource<Catalog>('/di-san/danh-muc')
  const places = useResource<Page<Heritage>>(type === 'BO_SUNG' ? '/di-san?kichThuoc=100' : null)
  const mine = useResource<Page<Contribution>>('/dong-gop-di-san/toi')
  const action = useAction()
  const location = useAction()
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const f = new FormData(form)
    await action.run(async () => {
      let path: string | undefined
      if (image) {
        path = uploaded?.image === image ? uploaded.path : await upload(image)
        setUploaded({ image, path })
      }
      await api('/dong-gop-di-san', 'POST', {
        maYeuCau: requestId, loai: type, tieuDe: f.get('title'), noiDung: f.get('description'),
        urlNguon: f.get('url') || null, anhPath: path,
        ...(type === 'DI_SAN_MOI' ? { tinhThanhId: Number(f.get('province')), loaiDiSanId: Number(f.get('kind')), diaChi: f.get('address'),
          viDo: Number(coordinates.lat), kinhDo: Number(coordinates.lng) } : { diSanId: Number(f.get('heritage')) }),
      })
      form.reset()
      setCoordinates({ lat: '', lng: '' }); setImage(null); setUploaded(undefined); setRequestId(crypto.randomUUID()); mine.reload()
    }, 'Đã gửi vào hàng chờ kiểm duyệt.')
  }
  return <><PageTitle eyebrow="TƯ LIỆU CỘNG ĐỒNG" title="Góp một dấu chân" text="Đề xuất di sản mới hoặc bổ sung tư liệu cho địa điểm đang có." />
    <div className="detail-columns reverse"><section className="surface"><h2>Gửi đóng góp</h2>
      <form className="form-stack" onSubmit={e => void submit(e)}>
        <label>Loại đóng góp<select value={type} onChange={e => setType(e.target.value)}><option value="DI_SAN_MOI">Đề xuất di sản mới</option><option value="BO_SUNG">Bổ sung di sản đã có</option></select></label>
        <label>{type === 'DI_SAN_MOI' ? 'Tên di sản' : 'Tiêu đề tư liệu'}<input name="title" maxLength={200} required /></label>
        {type === 'DI_SAN_MOI' ? <>
          <div className="form-grid"><label>Tỉnh, thành<select name="province" required><option value="">Chọn tỉnh, thành</option>{catalog.data?.tinhThanh.map(x => <option value={x.tinhThanhId} key={x.tinhThanhId}>{x.ten}</option>)}</select></label>
            <label>Loại di sản<select name="kind" required><option value="">Chọn loại di sản</option>{catalog.data?.loaiDiSan.map(x => <option value={x.loaiDiSanId} key={x.loaiDiSanId}>{x.ten}</option>)}</select></label></div>
          <label>Địa chỉ<input name="address" required maxLength={250} /></label>
          <div className="form-grid"><label>Vĩ độ<input type="number" required min={-90} max={90} step="any" value={coordinates.lat} onChange={e => setCoordinates(x => ({ ...x, lat: e.target.value }))} /></label>
            <label>Kinh độ<input type="number" required min={-180} max={180} step="any" value={coordinates.lng} onChange={e => setCoordinates(x => ({ ...x, lng: e.target.value }))} /></label></div>
          <button className="button outline" type="button" disabled={location.busy} onClick={() => void location.run(async () => {
            const p = await position(); setCoordinates({ lat: String(p.latitude), lng: String(p.longitude) })
          }, 'Đã điền tọa độ hiện tại.') }><LocateFixed size={16} /> Lấy vị trí hiện tại</button>
          <Feedback error={location.error} message={location.message} />
        </> : <label>Địa điểm<select name="heritage" value={heritage} onChange={e => setHeritage(e.target.value)} required><option value="">Chọn di sản</option>{places.data?.muc.map(x => <option key={x.diSanId} value={x.diSanId}>{x.ten}</option>)}</select></label>}
        <label>Nội dung<textarea name="description" required maxLength={1600} rows={5} /></label>
        <label>Nguồn tham khảo (tùy chọn)<input name="url" type="url" maxLength={1000} placeholder="https://…" /></label>
        <label><span className="inline"><Camera size={17} /> Ảnh tư liệu (tùy chọn)</span><input type="file" accept="image/jpeg,image/png,image/webp" onChange={e => { setImage(e.target.files?.[0] ?? null); setUploaded(undefined) }} /></label>
        <button className="button primary" disabled={action.busy}><CirclePlus size={17} /> {action.busy ? 'Đang gửi…' : 'Gửi kiểm duyệt'}</button>
        <Feedback error={action.error} message={action.message} />
      </form>
    </section><aside className="surface"><h2><ScrollText size={21} /> Bài của tôi</h2><LoadState {...mine} retry={mine.reload} />
      {mine.data?.muc.map(row => <article className="record" key={row.dongGopId}><Status value={row.trangThai} /><h3>{row.tieuDe}</h3><p>{vnDate(row.taoLucUtc)} · {row.loai === 'DI_SAN_MOI' ? 'Di sản mới' : 'Tư liệu bổ sung'}</p>
        {row.lyDo && <p>Lý do: {row.lyDo}</p>}{row.trangThai === 'CHO_DUYET' && <button className="button quiet" disabled={action.busy} onClick={() => void action.run(async () => { await api('/dong-gop-di-san/' + row.dongGopId + '/rut', 'PUT'); mine.reload() }, 'Đã rút bài.')}>Rút bài</button>}</article>)}
      {mine.data?.tong === 0 && <Empty title="Chưa có bài đóng góp." />}
    </aside></div>
  </>
}

export function ModerationPage() {
  const [status, setStatus] = useState('CHO_DUYET')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<Contribution>()
  const queue = useResource<Page<Contribution>>('/quan-tri/dong-gop-di-san' + qs({ trangThai: status, trang: page }))
  const action = useAction()
  async function decide(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget, (event.nativeEvent as SubmitEvent).submitter)
    if (!selected) return
    const accepted = form.get('decision') === 'approve'
    await action.run(async () => {
      await api(`/quan-tri/dong-gop-di-san/${selected.dongGopId}/duyet`, 'PUT', {
        quyetDinh: accepted ? 'DA_DUYET' : 'TU_CHOI',
        slug: accepted && selected.loai === 'DI_SAN_MOI' ? form.get('slug') || null : null,
        lyDo: !accepted ? form.get('reason') || null : null,
      })
      setSelected(undefined); queue.reload()
    }, accepted ? 'Đã duyệt và công bố tư liệu.' : 'Đã từ chối đóng góp.')
  }
  return <div className="workspace">
    <PageTitle eyebrow="KHÔNG GIAN KIỂM DUYỆT VĂN HÓA" title="Duyệt tư liệu cộng đồng" text="Xem nội dung, đối chiếu nguồn và quyết định có công bố lên trang di sản hay không." />
    <div className="chips">{[['CHO_DUYET', 'Chờ duyệt'], ['DA_DUYET', 'Đã duyệt'], ['TU_CHOI', 'Từ chối'], ['DA_RUT', 'Đã rút']].map(([key, label]) =>
      <button className={status === key ? 'chip active' : 'chip'} key={key} onClick={() => { setStatus(key); setPage(1); setSelected(undefined) }}>{label}</button>)}</div>
    <Feedback error={action.error} message={action.message} />
    <div className="detail-columns reverse"><section className="surface"><h2>Danh sách đóng góp</h2><LoadState {...queue} retry={queue.reload} />
      {queue.data?.muc.map(row => <button type="button" className={'queue-row' + (selected?.dongGopId === row.dongGopId ? ' selected' : '')} key={row.dongGopId} onClick={() => setSelected(row)}>
        <span><strong>{row.tieuDe}</strong><small>{row.loai === 'DI_SAN_MOI' ? 'Di sản mới' : 'Tư liệu bổ sung'} · {vnDate(row.taoLucUtc)}</small></span><Status value={row.trangThai} />
      </button>)}{queue.data?.tong === 0 && <Empty title="Danh sách này đang trống." />}
      {!!queue.data && queue.data.tong > 20 && <div className="pager"><button className="button outline" disabled={page <= 1} onClick={() => {setPage(n => n - 1);setSelected(undefined)}}>Trước</button>
        <span>Trang {page}</span><button className="button outline" disabled={page * 20 >= queue.data.tong} onClick={() => {setPage(n => n + 1);setSelected(undefined)}}>Sau</button></div>}
    </section>
    <section className="surface">{selected ? <>
      <Status value={selected.trangThai} /><h2>{selected.tieuDe}</h2><p className="prose">{selected.noiDung}</p>
      {selected.diaChi && <p>{selected.diaChi} · {selected.viDo}, {selected.kinhDo}</p>}
      {selected.urlNguon && /^https?:\/\//i.test(selected.urlNguon) && <a href={selected.urlNguon} target="_blank" rel="noreferrer" className="text-link">Xem nguồn tham khảo</a>}
      {selected.anhPath && ['CHO_DUYET','DA_DUYET'].includes(selected.trangThai) && <ProtectedPhoto path={selected.anhPath} alt={'Ảnh ' + selected.tieuDe} />}
      {selected.lyDo && <p>Lý do từ chối: {selected.lyDo}</p>}
      {selected.trangThai === 'CHO_DUYET' && <form className="form-stack" key={selected.dongGopId} onSubmit={e => void decide(e)}>
        {selected.loai === 'DI_SAN_MOI' && <label>Slug khi duyệt<input name="slug" maxLength={160} placeholder="vi-du-di-san-moi" /></label>}
        <label>Lý do nếu từ chối<textarea name="reason" maxLength={500} rows={3} /></label>
        <div className="inline"><button className="button primary" name="decision" value="approve" disabled={action.busy}>Duyệt và công bố</button>
          <button className="button outline" name="decision" value="reject" disabled={action.busy}>Từ chối</button></div>
      </form>}
    </> : <Empty title="Chọn một đóng góp để xem chi tiết." />}</section></div>
  </div>
}
