import { useEffect, useState } from 'react'
import { ArrowRight, BookOpen, Camera, Compass, LocateFixed, MapPin, Search } from 'lucide-react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { api, qs } from '../../shared/api'
import type { Catalog, Heritage, HeritageDetail, Page } from '../../shared/types'
import { Cover, Empty, Feedback, LoadState, MediaGallery, PageTitle, PlaceCard } from '../../shared/ui'
import { useAction, useResource } from '../../shared/hooks'
import { useSession } from '../accounts/session'
import { position } from '../checkins/position'
import { OptionalQuiz, StoryText, VisitorReviews } from './HeritageReading'

export function ExplorePage() {
  const [filters, setFilters] = useState({ q: '', tinhThanhId: '', loaiDiSanId: '', trang: 1 })
  const [nearby, setNearby] = useState<Heritage[] | null>(null)
  const catalog = useResource<Catalog>('/di-san/danh-muc')
  const list = useResource<Page<Heritage>>('/di-san' + qs({ ...filters, kichThuoc: 9 }))
  const action = useAction()
  const places = nearby ?? list.data?.muc
  return <>
    <PageTitle eyebrow="THƯ MỤC DI SẢN" title="Khám phá những miền ký ức" text="Tìm địa điểm, đọc tư liệu và chọn điểm đến tiếp theo." />
    <form className="surface filters" onSubmit={e => {
      e.preventDefault(); const fields = new FormData(e.currentTarget)
      setNearby(null); setFilters({ q: String(fields.get('q')), tinhThanhId: String(fields.get('province')), loaiDiSanId: String(fields.get('type')), trang: 1 })
    }}>
      <label>Tìm di sản<input name="q" type="search" placeholder="Hội An, Mỹ Sơn…" /></label>
      <label>Tỉnh, thành<select name="province"><option value="">Tất cả</option>{catalog.data?.tinhThanh.map(p => <option key={p.tinhThanhId} value={p.tinhThanhId}>{p.ten}</option>)}</select></label>
      <label>Loại di sản<select name="type"><option value="">Tất cả</option>{catalog.data?.loaiDiSan.map(p => <option key={p.loaiDiSanId} value={p.loaiDiSanId}>{p.ten}</option>)}</select></label>
      <button className="button primary"><Search size={17} /> Tìm kiếm</button>
      <button type="button" className="button outline" disabled={action.busy} onClick={() => void action.run(async () => {
        const coordinates = await position()
        setNearby(await api<Heritage[]>('/di-san/gan' + qs({ viDo: coordinates.latitude, kinhDo: coordinates.longitude, banKinhM: 50000 })))
      }, 'Hiển thị di sản trong phạm vi 50 km.') }><LocateFixed size={17} /> Gần tôi</button>
    </form>
    <Feedback error={action.error} message={nearby ? action.message : undefined} />
    {nearby ? <button className="button quiet" onClick={() => setNearby(null)}>Về danh mục</button> : <LoadState {...list} retry={list.reload} />}
    <div className="cards three">{places?.map(place => <PlaceCard key={place.diSanId} {...place} />)}</div>
    {places?.length === 0 && <Empty title="Chưa tìm thấy di sản phù hợp." />}
    {!nearby && !!list.data && list.data.tong > 9 && <div className="pager">
      <button className="button outline" disabled={filters.trang === 1} onClick={() => setFilters(x => ({ ...x, trang: x.trang - 1 }))}>Trang trước</button>
      <span>Trang {filters.trang} / {Math.ceil(list.data.tong / 9)}</span>
      <button className="button outline" disabled={filters.trang * 9 >= list.data.tong} onClick={() => setFilters(x => ({ ...x, trang: x.trang + 1 }))}>Trang sau</button>
    </div>}
  </>
}

export function HeritagePage() {
  const { slug } = useParams()
  const { profile } = useSession()
  const detail = useResource<HeritageDetail>(slug ? '/di-san/' + encodeURIComponent(slug) : null)
  const place = detail.data?.diSan
  const { hash } = useLocation()
  useEffect(() => { if (place && hash) document.getElementById(hash.slice(1))?.scrollIntoView() }, [place, hash])
  const media = [...(detail.data?.media ?? []), { anhPath: undefined, videoPath: place?.videoPath }]
    .filter(x => !!(x.anhPath ?? x.videoPath) && !(x.anhPath ?? x.videoPath)?.startsWith('demo/'))
  const action = useAction()
  if (!place) return <><PageTitle eyebrow="TƯ LIỆU ĐỊA ĐIỂM" title="Chi tiết di sản" /><LoadState {...detail} retry={detail.reload} /></>
  const coordinates = `${place.viDo},${place.kinhDo}`
  return <>
    <Link className="back-link" to="/kham-pha">← Trở về khám phá</Link>
    <div className="detail-hero"><Cover slug={place.slug} path={place.anhBiaPath} alt={place.ten} />
      <div><p className="eyebrow">{place.loaiDiSan} · {place.tinhThanh}</p><h1>{place.ten}</h1><p>{place.tomTat}</p></div></div>
    <nav className="reading-nav" aria-label="Mục lục di sản"><a href="#cau-chuyen">Lịch sử</a>
      {!!media.length && <a href="#thu-vien">Ảnh và video</a>}<a href="#tu-lieu">Tư liệu</a><a href="#danh-gia">Đánh giá</a></nav>
    <div className="detail-columns editorial-columns"><div className="article-stack">
      <section className="surface article-section" id="cau-chuyen"><p className="eyebrow">HỒ SƠ DI SẢN</p>
        <h2>Câu chuyện địa điểm</h2><StoryText text={place.noiDung || place.tomTat} /></section>
      {!!media.length && <section className="surface article-section" id="thu-vien"><h2>Ảnh và video</h2><MediaGallery title={place.ten} items={media} /></section>}
      <section className="surface article-section" id="tu-lieu"><h2>Tư liệu và nguồn tham khảo</h2>
        {!detail.data?.tuLieu.length && <p>Tư liệu sẽ xuất hiện sau khi được duyệt.</p>}
        {detail.data?.tuLieu.map(item => <article key={item.tuLieuId} className="record"><small>{item.nguon}</small><h3>{item.tieuDe}</h3><p>{item.trichYeu}</p>
          {item.urlNguon && /^https?:\/\//i.test(item.urlNguon) && <a href={item.urlNguon} target="_blank" rel="noreferrer" className="text-link">Đọc nguồn <ArrowRight size={14} /></a>}</article>)}
      </section>
      <VisitorReviews key={'review-' + place.diSanId} id={place.diSanId} slug={place.slug} />
      <OptionalQuiz key={'quiz-' + place.diSanId} id={place.diSanId} slug={place.slug} />
    </div><aside className="surface side-info visit-info"><h2>Thông tin tham quan</h2><p><MapPin size={17} /> {place.diaChi}</p>
      <a className="text-link" href={'https://www.openstreetmap.org/?mlat=' + place.viDo + '&mlon=' + place.kinhDo + '#map=16/' + coordinates.replace(',', '/')} target="_blank" rel="noreferrer">Xem vị trí <ArrowRight size={15} /></a>
      {!!detail.data?.gioThamQuan.length && <ul>{detail.data.gioThamQuan.map(x => <li key={x.gioThamQuanId}>{x.thuTrongTuan === 7 ? 'Chủ nhật' : 'Thứ ' + (x.thuTrongTuan + 1)} · {x.gioMo.slice(0, 5)}–{x.gioDong.slice(0, 5)}</li>)}</ul>}
      <div className="form-stack">
        <Link className="button primary" to={`/check-in?diSanId=${place.diSanId}`}><Camera size={17} /> Check-in</Link>
        <Link className="button outline" to={`/hanh-trinh?diSanId=${place.diSanId}`}><Compass size={17} /> Thêm vào hành trình</Link>
        <Link className="button outline" to={`/dong-gop?diSanId=${place.diSanId}`}><BookOpen size={17} /> Bổ sung tư liệu</Link>
        {profile && <div className="inline"><button className="button quiet" onClick={() => void action.run(() => api(`/di-san/${place.diSanId}/yeu-thich`, 'PUT'), 'Đã lưu địa điểm.')}>Lưu yêu thích</button>
          <button className="button quiet" onClick={() => void action.run(() => api(`/di-san/${place.diSanId}/yeu-thich`, 'DELETE'), 'Đã bỏ lưu.')}>Bỏ lưu</button></div>}
      </div><Feedback error={action.error} message={action.message} />
    </aside></div>
  </>
}

export function FavoritesPage() {
  const list = useResource<Heritage[]>('/di-san/yeu-thich/toi')
  return <><PageTitle eyebrow="DẤU TRANG CỦA TÔI" title="Di sản đã lưu" /><LoadState {...list} retry={list.reload} />
    <div className="cards three">{list.data?.map(place => <PlaceCard key={place.diSanId} {...place} />)}</div>
    {list.data?.length === 0 && <Empty title="Bạn chưa lưu địa điểm nào."><Link to="/kham-pha">Khám phá danh mục</Link></Empty>}</>
}
