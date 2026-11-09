import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../../shared/api'
import type { Experience, Heritage, Page } from '../../shared/types'
import { Empty, Feedback, LoadState, PageTitle, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'

type Item = (Heritage & { trangThai: string; banKinhCheckInM: number; saiSoGpsToiDaM: number })
type Mission = { nhiemVuId: number; ten: string; congKhai: boolean; moTa: string }
export function AdminContent() {
  const [tab, setTab] = useState('places')
  const places = useResource<Page<Item>>(tab === 'places' ? '/quan-tri/di-san' : null)
  const experiences = useResource<Experience[]>(tab === 'activities' ? '/quan-tri/trai-nghiem' : null)
  const missions = useResource<Mission[]>(tab === 'missions' ? '/quan-tri/nhiem-vu' : null)
  const action = useAction()
  return <div className="workspace"><PageTitle eyebrow="QUẢN TRỊ HỆ THỐNG" title="Quản trị nội dung" text="Quản lý trạng thái hiển thị của di sản, trải nghiệm và nhiệm vụ." />
    <div className="chips">{[['places','Di sản'],['activities','Trải nghiệm'],['missions','Nhiệm vụ']].map(([id, label]) =>
      <button className={tab === id ? 'chip active' : 'chip'} key={id} onClick={() => setTab(id)}>{label}</button>)}
      <Link className="chip" to="/quan-tri">Tài khoản</Link></div>
    <Feedback error={action.error} message={action.message} />
    <section className="surface">
      {tab === 'places' && <><LoadState {...places} retry={places.reload} />{places.data?.muc.map(p => <div className="queue-row" key={p.diSanId}><span><strong>{p.ten}</strong><small>{p.diaChi}</small></span>
        <div className="inline"><Status value={p.trangThai} /><button className="button outline" disabled={action.busy} onClick={() => void action.run(async () => {
          await api('/quan-tri/di-san/' + p.diSanId, 'PUT', { ...p, trangThai: p.trangThai === 'CONG_KHAI' ? 'AN' : 'CONG_KHAI' });places.reload()
        })}>{p.trangThai === 'CONG_KHAI' ? 'Ẩn' : 'Công khai'}</button></div></div>)}{places.data?.tong === 0 && <Empty title="Chưa có địa điểm." />}</>}
      {tab === 'activities' && <><LoadState {...experiences} retry={experiences.reload} />{experiences.data?.map(e => <div className="queue-row" key={e.traiNghiemId}><span><strong>{e.ten}</strong><small>{e.thoiLuongPhut} phút</small></span>
        <div className="inline"><Status value={e.trangThai ?? 'NHAP'} />{['CHO_DUYET','CONG_KHAI','AN'].includes(e.trangThai ?? '') && <button className="button outline" disabled={action.busy} onClick={() => void action.run(async () => {
          await api(`/quan-tri/trai-nghiem/${e.traiNghiemId}/cong-khai`, 'PUT', { congKhai: e.trangThai !== 'CONG_KHAI' });experiences.reload()
        })}>{e.trangThai === 'CONG_KHAI' ? 'Ẩn' : 'Duyệt công khai'}</button>}</div></div>)}{experiences.data?.length === 0 && <Empty title="Chưa có trải nghiệm." />}</>}
      {tab === 'missions' && <><LoadState {...missions} retry={missions.reload} />{missions.data?.map(m => <div className="queue-row" key={m.nhiemVuId}><span><strong>{m.ten}</strong><small>{m.moTa}</small></span>
        <div className="inline"><Status value={m.congKhai ? 'CONG_KHAI' : 'AN'} /><button className="button outline" disabled={action.busy} onClick={() => void action.run(async () => {
          await api(`/quan-tri/nhiem-vu/${m.nhiemVuId}/cong-khai`, 'PUT', { congKhai: !m.congKhai });missions.reload()
        })}>{m.congKhai ? 'Ẩn' : 'Công khai'}</button></div></div>)}{missions.data?.length === 0 && <Empty title="Chưa có nhiệm vụ." />}</>}
    </section>
  </div>
}
