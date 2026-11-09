import { BookImage, MapPin, PencilLine, Trophy, UserRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api, mediaUrl, upload } from '../../shared/api'
import { vnDate } from '../../shared/format'
import { useAction, useResource } from '../../shared/hooks'
import { Empty, Feedback, LoadState, PageTitle, ProtectedPhoto, Status } from '../../shared/ui'
import { useSession } from './session'
import type { Role } from '../../shared/types'

type Score = { hang: number; diem: number; soBai: number }
type Leader = Score & { nguoiDungId: number; hoTen: string; anhDaiDienPath?: string }
type Passport = {
  thanhTich: Score | null; tongDiSan: number; tongKhoanhKhac: number; tongDongGop: number;
  daDen: { diSanId: number; ten: string; slug?: string; lanCuoiUtc: string }[];
  khoanhKhac: { nhatKyId: number; hanhTrinhId: number; tieuDe: string; ngayGhi: string; anhBiaPath?: string }[];
  dongGop: { dongGopId: number; tieuDe: string; trangThai: string }[];
}
const names: Record<Role,string> = { THANH_VIEN: 'Thành viên', QUAN_TRI: 'Quản trị viên', KIEM_DUYET_VAN_HOA: 'Cán bộ văn hóa', DOI_TAC: 'Đối tác' }
const level = (points: number) => points >= 150 ? 'Người kể chuyện' : points >= 60 ? 'Người khám phá' : 'Người mở sổ'

export function Avatar({ path, name }: { path?: string; name: string }) {
  return <span className="avatar"><UserRound size={24} aria-hidden="true" />{path && !path.startsWith('demo/') &&
    <img key={path} src={mediaUrl(path)} alt={'Ảnh đại diện ' + name} onError={e => { e.currentTarget.hidden = true }} />}</span>
}
export function ProfilePage() {
  const { profile, refresh } = useSession()
  const result = useResource<Passport>('/tai-khoan/ho-chieu')
  const data = result.data, action = useAction()
  if (!profile) return null
  return <>
    <PageTitle eyebrow="HỘ CHIẾU DI SẢN" title="Hồ sơ của tôi" text="Xem lại những nơi đã đến và những điều bạn đã ghi lại." />
    <section className="passport-cover"><div className="profile-identity"><Avatar path={profile.anhDaiDienPath} name={profile.hoTen} />
      <div><h2>{profile.hoTen}</h2><p>{profile.email}</p><div className="chips">{profile.vaiTro.map(r => <span className="badge" key={r}>{names[r]}</span>)}</div></div></div>
      <Link className="button outline" to="/bang-xep-hang"><Trophy size={17} /> Bảng xếp hạng</Link></section>
    <LoadState {...result} retry={result.reload} />
    {data && <div className="metric-grid">
      <div className="surface"><Trophy size={21} /><strong>{data.thanhTich?.diem ?? 0}</strong><small>Điểm · {data.thanhTich ? 'Hạng ' + data.thanhTich.hang : 'Chưa có hạng'}</small></div>
      <div className="surface"><MapPin size={21} /><strong>{data.tongDiSan}</strong><small>Di sản đã đến</small></div>
      <div className="surface"><BookImage size={21} /><strong>{data.tongKhoanhKhac}</strong><small>Khoảnh khắc</small></div>
      <div className="surface"><PencilLine size={21} /><strong>{data.tongDongGop}</strong><small>Đóng góp</small></div>
    </div>}
    <div className="detail-columns"><section className="surface"><h2>Thiết lập hồ sơ</h2>
      <form key={profile.nguoiDungId} className="form-stack" onSubmit={e => {
        e.preventDefault(); const form = e.currentTarget, f = new FormData(form)
        void action.run(async () => {
          const file = f.get('avatar')
          let path = f.has('remove') ? null : profile.anhDaiDienPath ?? null
          if (file instanceof File && file.size) {
            if (!['image/jpeg','image/png','image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) throw new Error('Chọn ảnh JPG, PNG hoặc WebP tối đa 5 MB.')
            path = await upload(file)
          }
          await api('/tai-khoan/toi', 'PUT', { hoTen: f.get('name'), anhDaiDienPath: path, choPhepCaNhanHoa: f.has('personalize') })
          const input = form.elements.namedItem('avatar')
          if (input instanceof HTMLInputElement) input.value = ''
          await refresh()
        }, 'Đã lưu hồ sơ.')
      }}><label>Tên hiển thị<input name="name" defaultValue={profile.hoTen} maxLength={120} required /></label>
        <label>Ảnh đại diện<input type="file" name="avatar" accept="image/jpeg,image/png,image/webp" /><small>Ảnh có thể hiển thị trên bảng xếp hạng công khai.</small></label>
        {profile.anhDaiDienPath && <label className="check"><input type="checkbox" name="remove" /> Gỡ ảnh hiện tại</label>}
        <label className="check"><input type="checkbox" name="personalize" defaultChecked={profile.choPhepCaNhanHoa} /> Cá nhân hóa gợi ý</label>
        <button className="button primary" disabled={action.busy}>Lưu hồ sơ</button>
      </form><Feedback error={action.error} message={action.message} />
      {!profile.vaiTro.includes('DOI_TAC') && <Link className="text-link block" to="/dang-ky-doi-tac">Đăng ký đối tác địa phương</Link>}
    </section>
    {data && <section className="surface"><p className="eyebrow">{level(data.thanhTich?.diem ?? 0)}</p><h2>Dấu chân gần đây</h2>
      {!data.daDen.length && <Empty title="Chưa có điểm check-in." />}
      {data.daDen.map(x => <article className="record" key={x.diSanId}>
        {x.slug ? <Link className="text-link" to={'/di-san/' + x.slug}>{x.ten}</Link> : <strong>{x.ten}</strong>}<small>{vnDate(x.lanCuoiUtc)}</small>
      </article>)}
      <h2 className="block">Đóng góp gần đây</h2>{!data.dongGop.length && <p>Chưa gửi đóng góp nào.</p>}
      {data.dongGop.map(x => <div className="queue-row" key={x.dongGopId}><span>{x.tieuDe}</span><Status value={x.trangThai} /></div>)}
      <Link className="text-link block" to="/dong-gop">Mở trang đóng góp</Link>
    </section>}</div>
    {data && <section className="surface block"><div className="section-heading"><h2>Khoảnh khắc gần đây</h2><Link className="button outline" to="/so-tay">Mở sổ tay</Link></div>
      {!data.khoanhKhac.length && <Empty title="Chưa có khoảnh khắc trong sổ tay." />}
      <div className="cards three">{data.khoanhKhac.map(x => <article className="memory-card" key={x.nhatKyId}>
        {x.anhBiaPath && !x.anhBiaPath.startsWith('demo/') && <ProtectedPhoto path={x.anhBiaPath} alt={x.tieuDe} />}
        <Link className="text-link" to={'/so-tay?hanhTrinhId=' + x.hanhTrinhId}>{x.tieuDe}</Link><small>{x.ngayGhi.split('-').reverse().join('/')}</small>
      </article>)}</div>
    </section>}
  </>
}
export function LeaderboardPage() {
  const result = useResource<Leader[]>('/bang-xep-hang')
  const { profile } = useSession()
  return <><PageTitle eyebrow="CỘNG ĐỒNG KHÁM PHÁ" title="Bảng xếp hạng di sản" text="Thành viên bằng điểm đồng hạng." />
    <section className="surface"><LoadState {...result} retry={result.reload} />
      {result.data?.length === 0 && <Empty title="Chưa có thành viên nhận điểm."><Link to="/kham-pha">Khám phá câu chuyện đầu tiên</Link></Empty>}
      {result.data?.map(x => <div key={x.nguoiDungId} className={profile?.nguoiDungId === x.nguoiDungId ? 'leader-row current' : 'leader-row'}>
        <span className="rank-no">{String(x.hang).padStart(2,'0')}</span><Avatar path={x.anhDaiDienPath} name={x.hoTen} />
        <div><strong>{x.hoTen}</strong><small>{level(x.diem)} · {x.soBai} di sản</small></div><strong>{x.diem} điểm</strong>
      </div>)}
    </section>
  </>
}
