import { useState } from 'react'
import { Building2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api, qs } from '../../shared/api'
import { useAction, useResource } from '../../shared/hooks'
import { Empty, Feedback, LoadState, PageTitle, Status } from '../../shared/ui'
import type { Catalog } from '../../shared/types'
import { useSession } from '../accounts/session'
type Application = { donId: number; ten: string; trangThai: string; lyDo?: string }
type AdminApplication = Application & { nguoiGui: string; tinhThanh: string; email: string; dienThoai: string; diaChi: string; moTa: string }

export function PartnerApplicationPage() {
  const { profile, refresh } = useSession()
  const catalog = useResource<Catalog>('/di-san/danh-muc')
  const history = useResource<Application[]>('/don-doi-tac/toi')
  const action = useAction()
  return <><PageTitle eyebrow="HỢP TÁC ĐỊA PHƯƠNG" title="Đăng ký làm đối tác" text="Dành cho đơn vị tổ chức trải nghiệm gắn với di sản." />
    <div className="detail-columns"><section className="surface"><h2><Building2 size={23} /> Hồ sơ đơn vị</h2>
      <LoadState {...catalog} retry={catalog.reload} />
      {profile?.vaiTro.includes('DOI_TAC') ? <Link className="button primary" to="/doi-tac">Vào khu vực đối tác</Link> :
        history.data?.some(x => x.trangThai === 'CHO_DUYET') ? <p>Đơn của bạn đang chờ quản trị viên xét duyệt.</p> :
        !profile?.emailDaXacMinh ? <p>Cần xác minh email trước khi gửi đơn.</p> : !!history.data && !!catalog.data && <form className="form-stack" onSubmit={e => {
          e.preventDefault(); const form = e.currentTarget, f = Object.fromEntries(new FormData(form))
          void action.run(async () => { await api('/don-doi-tac', 'POST', { ...f, tinhThanhId: Number(f.tinhThanhId) }); form.reset(); history.reload() }, 'Đã gửi hồ sơ.')
        }}>
          <label>Tên đơn vị<input name="ten" maxLength={160} required /></label>
          <label>Tỉnh, thành<select name="tinhThanhId" required defaultValue=""><option value="">Chọn địa bàn</option>
            {catalog.data.tinhThanh.map(x => <option key={x.tinhThanhId} value={x.tinhThanhId}>{x.ten}</option>)}</select></label>
          <label>Email liên hệ<input name="email" type="email" maxLength={254} defaultValue={profile.email} required /></label>
          <label>Điện thoại<input name="dienThoai" type="tel" minLength={6} maxLength={24} required /></label>
          <label>Địa chỉ<input name="diaChi" maxLength={250} required /></label>
          <label>Giới thiệu và hoạt động dự kiến<textarea name="moTa" rows={4} minLength={20} maxLength={1000} required /></label>
          <button className="button primary" disabled={action.busy}>Gửi hồ sơ</button>
        </form>}
      <Feedback error={action.error} message={action.message} />
    </section><section className="surface"><div className="section-heading"><h2>Lịch sử xét duyệt</h2>
      <button className="button outline" disabled={action.busy} onClick={() => void action.run(async () => { history.reload(); await refresh() }, '')}>Cập nhật</button></div>
      <LoadState {...history} retry={history.reload} />{history.data?.length === 0 && <Empty title="Bạn chưa gửi hồ sơ." />}
      {history.data?.map(x => <div className="record" key={x.donId}><div className="inline"><strong>{x.ten}</strong><Status value={x.trangThai} /></div>{x.lyDo && <p>{x.lyDo}</p>}</div>)}
    </section></div>
  </>
}

export function PartnerApprovalPage() {
  const [page, setPage] = useState(1), action = useAction()
  const list = useResource<AdminApplication[]>('/quan-tri/don-doi-tac' + qs({ trang: page }))
  return <><PageTitle eyebrow="QUẢN TRỊ HỆ THỐNG" title="Hồ sơ đối tác" text="Duyệt hồ sơ sẽ tạo đơn vị và cấp quyền quản lý cho người gửi." />
    <LoadState {...list} retry={list.reload} /><Feedback error={action.error} message={action.message} />
    {list.data?.length === 0 && <Empty title="Chưa có hồ sơ trên trang này." />}
    <div className="article-stack">{list.data?.map(x => <section className="surface" key={x.donId}>
      <div className="section-heading"><div><p className="eyebrow">{x.tinhThanh} · HỒ SƠ #{x.donId}</p><h3>{x.ten}</h3></div><Status value={x.trangThai} /></div>
      <p>{x.moTa}</p><p>{x.diaChi} · {x.nguoiGui} · {x.email} · {x.dienThoai}</p>{x.lyDo && <p>{x.lyDo}</p>}
      {x.trangThai === 'CHO_DUYET' && <form className="form-stack" onSubmit={e => {
        e.preventDefault(); const submitter = (e.nativeEvent as SubmitEvent).submitter as HTMLButtonElement | null
        const approved = submitter?.value === 'approve', reason = String(new FormData(e.currentTarget).get('reason')).trim()
        void action.run(async () => {
          if (!approved && !reason) throw new Error('Nhập lý do từ chối.')
          await api('/quan-tri/don-doi-tac/' + x.donId, 'PUT', { duyet: approved, lyDo: approved ? null : reason }); list.reload()
        }, approved ? 'Đã duyệt hồ sơ và cấp quyền đối tác.' : 'Đã từ chối hồ sơ.')
      }}><label>Lý do từ chối<input name="reason" maxLength={500} /></label><div className="inline">
        <button value="approve" className="button primary" disabled={action.busy}>Duyệt và cấp quyền</button>
        <button value="reject" className="button outline" disabled={action.busy}>Từ chối</button></div></form>}
    </section>)}</div>
    {(page > 1 || list.data?.length === 30) && <div className="pager"><button className="button outline" disabled={page === 1} onClick={() => setPage(page-1)}>Trang trước</button>
      <span>Trang {page}</span><button className="button outline" disabled={list.data?.length !== 30} onClick={() => setPage(page+1)}>Trang sau</button></div>}
  </>
}
