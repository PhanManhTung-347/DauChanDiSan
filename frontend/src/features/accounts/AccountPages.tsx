import { useState } from 'react'
import { ArrowRight, ShieldCheck, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useSession } from './session'
import { api, qs } from '../../shared/api'
import { Empty, Feedback, LoadState, PageTitle, Status } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import type { Page, Role } from '../../shared/types'

type AccountRow = { nguoiDungId: number; hoTen: string; email?: string; hoatDong: boolean; vaiTro: Role[] }
export function AdminPage() {
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const list = useResource<Page<AccountRow>>('/quan-tri/tai-khoan' + qs({ q: query, trang: page }))
  const { profile } = useSession()
  const action = useAction()
  async function toggle(account: AccountRow, change: Partial<{ hoatDong: boolean; doiTac: boolean; kiemDuyetVanHoa: boolean }>) {
    await action.run(async () => {
      await api('/quan-tri/tai-khoan/' + account.nguoiDungId + '/truy-cap', 'PUT', {
        hoatDong: account.hoatDong, doiTac: account.vaiTro.includes('DOI_TAC'),
        kiemDuyetVanHoa: account.vaiTro.includes('KIEM_DUYET_VAN_HOA'), ...change,
      })
      list.reload()
    })
  }
  return <div className="workspace">
    <PageTitle eyebrow="QUẢN TRỊ HỆ THỐNG" title="Tài khoản và phân quyền" text="Cấp quyền kiểm duyệt văn hóa, quyền đối tác và quản lý trạng thái tài khoản." />
    <div className="workspace-shortcuts"><Link to="/quan-tri/noi-dung"><UsersRound size={19} /> Quản trị nội dung <ArrowRight size={16} /></Link>
      <Link to="/kiem-duyet"><ShieldCheck size={19} /> Hàng chờ kiểm duyệt <ArrowRight size={16} /></Link></div>
    <div className="surface"><label className="search-label">Tìm tài khoản<input type="search" value={query} onChange={e => { setQuery(e.target.value); setPage(1) }} placeholder="Tên hoặc email" /></label>
      <LoadState {...list} retry={list.reload} /><Feedback error={action.error} message={action.message} />
      {list.data && <div className="table-wrap"><table><thead><tr><th>Tài khoản</th><th>Trạng thái</th><th>Kiểm duyệt</th><th>Đối tác</th><th>Quản lý</th></tr></thead>
        <tbody>{list.data.muc.map(row => { const locked = row.vaiTro.includes('QUAN_TRI') || row.nguoiDungId === profile?.nguoiDungId
          return <tr key={row.nguoiDungId}><td><strong>{row.hoTen}</strong><small>{row.email}</small></td>
            <td><Status value={row.hoatDong ? 'Đang hoạt động' : 'Đã khóa'} /></td>
            <td><label className="check"><input aria-label={`Quyền kiểm duyệt của ${row.hoTen}`} type="checkbox" checked={row.vaiTro.includes('KIEM_DUYET_VAN_HOA')} disabled={locked || action.busy}
              onChange={e => void toggle(row, { kiemDuyetVanHoa: e.target.checked })} /> Cán bộ văn hóa</label></td>
            <td><label className="check"><input aria-label={`Quyền đối tác của ${row.hoTen}`} type="checkbox" checked={row.vaiTro.includes('DOI_TAC')} disabled={locked || action.busy}
              onChange={e => void toggle(row, { doiTac: e.target.checked })} /> Đối tác</label></td>
            <td>{locked ? 'Quản trị viên' : <button className="button quiet" disabled={action.busy} onClick={() => void toggle(row, { hoatDong: !row.hoatDong })}>{row.hoatDong ? 'Khóa' : 'Mở khóa'}</button>}</td></tr>
        })}</tbody></table></div>}
      {list.data?.tong === 0 && <Empty title="Không tìm thấy tài khoản." />}
      {list.data && list.data.tong > 30 && <div className="inline"><button className="button outline" disabled={page === 1} onClick={() => setPage(p => p - 1)}>Trang trước</button>
        <span>Trang {page}</span><button className="button outline" disabled={page * 30 >= list.data.tong} onClick={() => setPage(p => p + 1)}>Trang sau</button></div>}
    </div>
  </div>
}

type Mission = { nhiemVuId: number; ten: string; moTa: string; loai: string; slugBaiDoc?: string }
type Question = { cauHoiId: number; noiDung: string; luaChon: { luaChonId: number; noiDung: string }[] }
type Badge = { huyHieuId: number; ten: string; moTa?: string }
export function MissionsPage() {
  const { profile } = useSession()
  const list = useResource<Mission[]>('/nhiem-vu')
  const badges = useResource<Badge[]>(profile ? '/nhiem-vu/huy-hieu/toi' : null)
  const [selected, setSelected] = useState<Mission>()
  const questions = useResource<Question[]>(selected?.loai === 'QUIZ' ? `/nhiem-vu/${selected.nhiemVuId}/cau-hoi` : null)
  const action = useAction()
  return <><PageTitle eyebrow="KHÁM PHÁ QUA CÂU HỎI" title="Nhiệm vụ di sản" text="Thử sức với câu hỏi về di sản và mở khóa huy hiệu." />
    <LoadState {...list} retry={list.reload} /><div className="cards three">{list.data?.map(m => <article className="surface" key={m.nhiemVuId}><span className="badge">{m.loai === 'QUIZ' ? 'Câu hỏi' : 'Check-in'}</span>
      <h2>{m.ten}</h2><p>{m.moTa}</p>{m.slugBaiDoc ? <Link className="button outline" to={'/di-san/' + m.slugBaiDoc + '#bai-kiem-tra'}>Đọc và làm bài tự chọn</Link> : <button className="button outline" onClick={() => setSelected(m)}>Xem nhiệm vụ</button>}</article>)}</div>
    {list.data?.length === 0 && <Empty title="Chưa có nhiệm vụ đang mở." />}
    {selected && <section className="surface block"><h2>{selected.ten}</h2><LoadState {...questions} retry={questions.reload} />
      {questions.data?.map(q => <div className="quiz" key={q.cauHoiId}><h3>{q.noiDung}</h3><div className="inline">{q.luaChon.map(o => <button className="button outline" key={o.luaChonId} disabled={!profile || action.busy}
        onClick={() => void action.run(async () => {
          const result = await api<{ traLoiDung: boolean }>(`/nhiem-vu/cau-hoi/${q.cauHoiId}/tra-loi`, 'POST', { luaChonId: o.luaChonId })
          badges.reload()
          if (!result.traLoiDung) throw new Error('Câu trả lời chưa đúng. Bạn có thể thử lại.')
        }, 'Bạn đã trả lời đúng.')}>{o.noiDung}</button>)}</div></div>)}
      {selected.loai !== 'QUIZ' && <button className="button outline" disabled={!profile || action.busy} onClick={() => void action.run(async () => { await api(`/nhiem-vu/${selected.nhiemVuId}/tien-do`, 'POST'); badges.reload() }, 'Đã cập nhật tiến độ.')}>Cập nhật tiến độ</button>}
      {!profile && <Link className="text-link" to="/dang-nhap">Đăng nhập để tham gia</Link>}<Feedback error={action.error} message={action.message} /></section>}
    {!!badges.data?.length && <section className="surface block"><h2>Huy hiệu của tôi</h2><div className="chips">{badges.data.map(b => <span key={b.huyHieuId} className="badge">{b.ten}</span>)}</div></section>}
  </>
}
