import { useState } from 'react'
import { BookOpen, Star } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api, qs } from '../../shared/api'
import { useAction, useResource } from '../../shared/hooks'
import { Empty, Feedback, LoadState, MediaGallery } from '../../shared/ui'
import { vnDate } from '../../shared/format'
import { useSession } from '../accounts/session'

export function StoryText({ text }: { text: string }) {
  return <div className="heritage-prose">{text.trim().split(/\r?\n\s*\r?\n/).map((block, i) => {
    const lines = block.split(/\r?\n/)
    const title = lines[0]?.startsWith('## ') ? lines.shift()?.slice(3) : undefined
    return <div key={i}>{title && <h3>{title}</h3>}{lines.length > 0 && <p>{lines.join('\n').split(/(\*\*[^*]+\*\*)/).map((value, n) =>
      value.startsWith('**') && value.endsWith('**') ? <strong key={n}>{value.slice(2,-2)}</strong> : value)}</p>}</div>
  })}</div>
}
export function Stars({ value }: { value: number }) {
  return <span className="stars" aria-label={value + ' trên 5 sao'}>{[1,2,3,4,5].map(n =>
    <Star key={n} size={17} aria-hidden="true" fill={n <= value ? 'currentColor' : 'none'} />)}</span>
}

type Review = { tenNguoiViet: string; soSao: number; noiDung?: string; taoLucUtc: string; anhPath?: string; videoPath?: string }
type Eligibility = { daCheckIn: boolean; danhGia?: { soSao: number; noiDung?: string; congKhai: boolean } }
export function VisitorReviews({ id, slug }: { id: number; slug: string }) {
  const { profile } = useSession()
  const [page, setPage] = useState(1)
  const endpoint = `/di-san/${id}/danh-gia`
  const list = useResource<Review[]>(endpoint + qs({ trang: page }))
  const eligibility = useResource<Eligibility>(profile ? endpoint + '/toi' : null)
  const action = useAction(), old = eligibility.data?.danhGia
  return <section className="surface article-section" id="danh-gia"><p className="eyebrow">GHI NHẬN THỰC TẾ</p><h2>Đánh giá của khách tham quan</h2>
    <p>Thành viên đã check-in có thể để lại một đánh giá từ 1 đến 5 sao.</p>
    <LoadState {...list} retry={list.reload} />
    {list.data?.length === 0 && <Empty title={page === 1 ? 'Chưa có đánh giá công khai.' : 'Đã hết đánh giá.'} />}
    {list.data?.map((r,i) => <article key={r.taoLucUtc+i} className="record"><Stars value={r.soSao} />
      <small>{r.tenNguoiViet} · {vnDate(r.taoLucUtc)}</small>{r.noiDung && <p className="prose">{r.noiDung}</p>}
      <MediaGallery title="Ảnh của khách tham quan" items={[{ anhPath:r.anhPath },{ videoPath:r.videoPath }]} />
    </article>)}
    {(page>1 || list.data?.length===20) && <div className="pager"><button className="button outline" disabled={page===1} onClick={() => setPage(page-1)}>Trang trước</button>
      <span>Trang {page}</span><button className="button outline" disabled={list.data?.length!==20} onClick={() => setPage(page+1)}>Trang sau</button></div>}
    {!profile && <Link className="text-link" to="/dang-nhap" state={{from:'/di-san/'+slug}}>Đăng nhập để đánh giá</Link>}
    {profile && <LoadState {...eligibility} retry={eligibility.reload} />}
    {eligibility.data && !eligibility.data.daCheckIn && <Link className="text-link" to={'/check-in?diSanId='+id}>Check-in để mở quyền đánh giá</Link>}
    {old?.congKhai === false && <p>Đánh giá của bạn đang được ẩn bởi bộ phận kiểm duyệt.</p>}
    {eligibility.data?.daCheckIn && old?.congKhai !== false && <form key={(old?.soSao ?? 0)+(old?.noiDung ?? '')} className="form-stack block" onSubmit={e => {
      e.preventDefault(); const f = new FormData(e.currentTarget)
      void action.run(async () => {
        await api(endpoint,'PUT',{soSao:Number(f.get('score')),noiDung:String(f.get('text')).trim() || null})
        eligibility.reload();list.reload()
      },'Đã lưu đánh giá.')
    }}><fieldset className="question"><legend>Điểm đánh giá</legend><div className="star-options">{[1,2,3,4,5].map(n =>
      <label key={n}><input name="score" type="radio" value={n} defaultChecked={old?.soSao===n} required /><Star size={19} /> {n}</label>)}</div></fieldset>
      <label>Nhận xét<textarea name="text" rows={3} maxLength={1000} defaultValue={old?.noiDung} placeholder="Điều gì đọng lại sau chuyến đi?" /></label>
      <button className="button primary" disabled={action.busy}>{old?'Cập nhật đánh giá':'Gửi đánh giá'}</button></form>}
    <Feedback error={action.error} message={action.message} />
  </section>
}

type Quiz = { diemThuong: number; daNhanDiem: boolean; cauHoi: { cauHoiId: number; noiDung: string; luaChon: { luaChonId: number; noiDung: string }[] }[] }
type Grade = { soDung: number; tong: number; dat: boolean; diemCong: number; chiTiet: { cauHoiId: number; dung: boolean; giaiThich?: string }[] }
export function OptionalQuiz({ id, slug }: { id: number; slug: string }) {
  const { profile } = useSession()
  const endpoint = `/di-san/${id}/bai-kiem-tra`
  const quiz = useResource<Quiz>(endpoint), action = useAction()
  const [grade,setGrade] = useState<Grade>()
  if (quiz.data?.cauHoi.length === 0) return null
  return <section className="surface article-section quiz-section" id="bai-kiem-tra"><p className="eyebrow"><BookOpen size={16} /> TỰ CHỌN</p>
    <h2>Thử nhớ câu chuyện</h2><LoadState {...quiz} retry={quiz.reload} />
    {quiz.data && <><p>Bạn có thể bỏ qua. Trả lời đúng ít nhất hai phần ba số câu để nhận <strong>{quiz.data.diemThuong} điểm di sản</strong> lần đầu.</p>
      {quiz.data.daNhanDiem && <span className="badge">Đã nhận điểm cho di sản này</span>}
      <details><summary>Làm bài kiểm tra</summary>
        {profile ? <form className="form-stack block" onSubmit={e => {
          e.preventDefault(); const f = new FormData(e.currentTarget)
          const traLoi = quiz.data!.cauHoi.map(q => ({ cauHoiId:q.cauHoiId, luaChonId:Number(f.get(String(q.cauHoiId))) }))
          void action.run(async () => { setGrade(await api<Grade>(endpoint,'POST',{traLoi}));quiz.reload() },'')
        }}>
          {quiz.data.cauHoi.map((q,i) => {
            const answered=grade?.chiTiet.find(x=>x.cauHoiId===q.cauHoiId)
            return <fieldset className="question" key={q.cauHoiId}><legend>{i+1}. {q.noiDung}</legend>{q.luaChon.map(o =>
              <label key={o.luaChonId}><input name={String(q.cauHoiId)} value={o.luaChonId} type="radio" required /> {o.noiDung}</label>)}
              {answered && <small>{answered.dung ? 'Đúng' : 'Chưa đúng'} · {answered.giaiThich}</small>}</fieldset>
          })}
          <button className="button primary" disabled={action.busy}>Nộp bài kiểm tra</button>
          <Feedback error={action.error} message={grade ? `${grade.soDung}/${grade.tong} câu đúng · ${grade.dat ?
            grade.diemCong ? 'Cộng ' + grade.diemCong + ' điểm.' : 'Đạt. Bạn đã nhận điểm trước đó.' : 'Chưa đạt. Bạn có thể thử lại.'}` : undefined} />
        </form> : <Link className="button outline block" to="/dang-nhap" state={{from:'/di-san/'+slug+'#bai-kiem-tra'}}>Đăng nhập để làm bài</Link>}
      </details></>}
  </section>
}
