import { useEffect, useState } from 'react'
import { ArrowDown, ArrowRight, ArrowUpRight, BookOpen, ChevronLeft, ChevronRight, Compass, Landmark, MapPin, Pause, Play, ScrollText, Ticket, Trophy } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useSession } from '../accounts/session'
import { mediaUrl } from '../../shared/api'
import { PlaceCard } from '../../shared/ui'
import { useResource } from '../../shared/hooks'
import type { Heritage, Page } from '../../shared/types'

const slides = [
  { slug: 'pho-co-hoi-an', image: '/images/di-san/cau-chua.webp', label: 'Chùa Cầu · Hội An', region: 'QUẢNG NAM XƯA', title: 'Mỗi miền đất,', accent: 'một dấu chân.', text: 'Dạo qua phố cổ, lắng nghe câu chuyện của những mái ngói và lưu lại ký ức của riêng bạn.' },
  { slug: 'thanh-dia-my-son', image: '/images/di-san/my-son.webp', label: 'Thánh địa Mỹ Sơn', region: 'DẤU ẤN CHĂM PA', title: 'Dấu thời gian', accent: 'trên từng viên gạch.', text: 'Ghé thăm những ngọn tháp gạch và tìm hiểu dấu ấn Chăm Pa còn lại giữa thung lũng.' },
  { slug: 'dai-noi-hue', image: '/images/di-san/hue.webp', label: 'Đại Nội · Huế', region: 'MIỀN CỐ ĐÔ', title: 'Về miền xưa,', accent: 'nghe Huế kể.', text: 'Bước qua những cánh cổng cũ để đọc câu chuyện của một kinh đô.' },
]
type Recommendation = Pick<Heritage, 'slug' | 'ten' | 'anhBiaPath'> & { lyDo?: string }
const picks = [
  { slug: 'pho-co-hoi-an', ten: 'Phố cổ Hội An', tinhThanh: 'Hội An', tomTat: 'Mái ngói, đèn lồng và nhịp sống của một thương cảng xưa.' },
  { slug: 'thanh-dia-my-son', ten: 'Thánh địa Mỹ Sơn', tinhThanh: 'Miền Trung', tomTat: 'Những ngọn tháp gạch lưu dấu kiến trúc Chăm Pa.' },
  { slug: 'dai-noi-hue', ten: 'Đại Nội Huế', tinhThanh: 'Huế', tomTat: 'Bước qua cổng thành để khám phá không gian cung đình.' },
]

function HeritageCarousel({ videoPath }: { videoPath?: string }) {
  const [slide, setSlide] = useState(0)
  const [paused, setPaused] = useState(() => window.matchMedia('(prefers-reduced-motion: reduce)').matches)
  const [hovered, setHovered] = useState(false)
  const active = slides[slide]
  useEffect(() => {
    if (paused || hovered) return
    const timer = window.setInterval(() => setSlide(index => (index + 1) % slides.length), 3000)
    return () => window.clearInterval(timer)
  }, [paused, hovered])
  function step(delta: number) { setSlide((slide + slides.length + delta) % slides.length); setPaused(true) }

  return <section className="home-hero" aria-label="Những miền di sản" aria-roledescription="băng chuyền"
    onMouseEnter={() => setHovered(true)} onMouseLeave={() => setHovered(false)}
    onFocusCapture={() => setHovered(true)} onBlurCapture={e => { if (!e.currentTarget.contains(e.relatedTarget)) setHovered(false) }}>
    <div className="hero-images" data-paused={paused || hovered} aria-hidden="true">
      {slides.map((item, i) => <img key={item.slug} src={item.image} alt="" className={i === slide ? 'active' : ''} fetchPriority={i === 0 ? 'high' : 'auto'} />)}
      {slide === 1 && videoPath && !paused && !hovered && <video muted autoPlay loop playsInline poster={slides[1].image}
        src={mediaUrl(videoPath)} />}
    </div>
    <div className="hero-shade" />
    <div className="hero-copy" aria-live={paused ? 'polite' : 'off'} key={active.slug}>
      <p className="hero-kicker"><span /> SỔ TAY DI SẢN VIỆT NAM</p>
      <h1>{active.title}<br /><em>{active.accent}</em></h1>
      <p className="hero-text">{active.text}</p>
      <div className="hero-actions"><Link className="button gold" to="/kham-pha">Bắt đầu khám phá <ArrowUpRight size={17} /></Link>
        <a className="button glass" href="#diem-hen"><ArrowDown size={16} /> Xem gợi ý</a></div>
    </div>
    <div className="hero-bottom">
      <div className="slide-controls">
        <button aria-label="Ảnh trước" onClick={() => step(-1)}><ChevronLeft size={19} /></button>
        <span aria-label={`Ảnh ${slide + 1} trên ${slides.length}`}>0{slide + 1} <i /> 0{slides.length}</span>
        <button aria-label="Ảnh tiếp theo" onClick={() => step(1)}><ChevronRight size={19} /></button>
        <button aria-label={paused ? 'Chạy ảnh tự động' : 'Dừng ảnh tự động'} onClick={() => setPaused(p => !p)}>
          {paused ? <Play size={15} /> : <Pause size={15} />}
        </button>
      </div>
      <Link to={'/di-san/' + active.slug} className="hero-caption"><MapPin size={17} /><span><small>{active.region}</small>{active.label}</span><ArrowUpRight size={16} /></Link>
    </div>
  </section>
}

export function HomePage() {
  const { profile } = useSession()
  const catalog = useResource<Page<Heritage>>('/di-san?kichThuoc=30')
  const recommended = useResource<{ muc: Recommendation[] }>(profile?.choPhepCaNhanHoa ? '/goi-y?gioiHan=3' : null)
  const suggestions = recommended.data?.muc ?? []
  const places = suggestions.length
    ? suggestions.map(item => catalog.data?.muc.find(x => x.slug === item.slug) ?? { ...item, tomTat: item.lyDo })
    : picks.map(item => catalog.data?.muc.find(x => x.slug === item.slug) ?? item)
  const [theme, setTheme] = useState(() => document.documentElement.dataset.heritageTheme || 'drum')
  function changeTheme(value: string) {
    setTheme(value)
    document.documentElement.dataset.heritageTheme = value
    localStorage.setItem('heritage-theme', value)
  }
  const stories = [
    { slug:'hoang-thanh-thang-long', label:'HÀ NỘI · KIẾN TRÚC', title:'Những lớp đất dưới Hoàng thành' },
    { slug:'lang-gom-thanh-ha', label:'HỘI AN · LÀNG NGHỀ', title:'Chuyện kể từ bàn xoay Thanh Hà' },
    { slug:'dai-noi-hue', label:'HUẾ · LỊCH SỬ', title:'Đi qua Ngọ Môn' },
  ]
  return <>
    <div className="home-edition">
    <span>
      <img
        className="vietnam-flag"
        src="/images/theme/co-viet-nam.svg"
        alt="Quốc kỳ Việt Nam"
        width={30}
        height={20}
      />
      CẨM NANG VĂN HÓA VIỆT NAM
    </span>
    <span>SỐ 01 · TỪ PHỐ CỔ ĐẾN CỐ ĐÔ</span>
  </div>
    <HeritageCarousel videoPath={catalog.data?.muc.find(x => x.slug === 'thanh-dia-my-son')?.videoPath} />
    <section className="home-section" id="diem-hen">
      <div className="section-heading"><div><p className="eyebrow"><span className="eyebrow-mark" /> {suggestions.length ? 'DÀNH RIÊNG CHO BẠN' : 'CHỌN MỘT NƠI ĐỂ BẮT ĐẦU'}</p>
        <h2>Những điểm hẹn di sản</h2><p>Mỗi địa điểm giữ một phần ký ức của đất nước.</p></div>
        <Link className="button outline" to="/kham-pha">Xem tất cả địa điểm <ArrowRight size={16} /></Link>
      </div>
      <div className="cards three">{places.map(place => <PlaceCard key={place.slug} {...place} />)}</div>
    </section>
    <section className="home-section atlas-section">
      <div className="atlas-visual"><p className="eyebrow">BẢN ĐỒ VIỆT NAM</p>
        <img loading="lazy" src="/images/theme/viet-nam.svg" alt="Bản đồ Việt Nam có Hoàng Sa, Trường Sa, Phú Quốc và Bạch Long Vĩ" />
        <div className="inline"><a href="/images/theme/viet-nam.svg" target="_blank" rel="noreferrer">Xem bản đồ lớn</a><a href="/images/nguon-anh.html">Nguồn bản đồ</a></div>
      </div>
      <div className="atlas-copy"><p className="eyebrow"><Landmark size={16} /> NHỮNG CÂU CHUYỆN ĐÁNG ĐỌC</p><h2>Từ một địa danh, mở ra một câu chuyện.</h2>
        <p>Đọc về những gì còn lại trong một lớp đất, một cánh cổng hay một món gốm.</p>
        <div className="story-links">{stories.map((x, index) => <Link key={x.slug} to={'/di-san/'+x.slug}><span>0{index+1}</span>
          <span><small>{x.label}</small><strong>{x.title}</strong></span><ArrowRight size={17} /></Link>)}</div>
        <div className="inline block" aria-label="Chọn họa tiết nền"><button aria-pressed={theme==='drum'} className={theme==='drum'?'chip active':'chip'} onClick={()=>changeTheme('drum')}>Nền trống đồng</button>
          <button aria-pressed={theme==='map'} className={theme==='map'?'chip active':'chip'} onClick={()=>changeTheme('map')}>Nền bản đồ</button></div>
      </div>
    </section>
    <section className="home-section"><div className="section-heading"><div><p className="eyebrow">ĐI XA HƠN MỘT BÀI ĐỌC</p><h2>Tự chọn cách khám phá</h2></div></div>
      <div className="cards three home-pathways">
        <Link to="/trai-nghiem" className="surface"><Ticket size={25} /><h3>Trải nghiệm tại chỗ</h3><p>Tham quan và thực hành cùng đối tác địa phương.</p><span className="text-link">Xem trải nghiệm <ArrowRight size={15} /></span></Link>
        <Link to="/nhiem-vu" className="surface"><BookOpen size={25} /><h3>Nhiệm vụ di sản</h3><p>Đọc câu chuyện, thử câu hỏi và sưu tập huy hiệu.</p><span className="text-link">Mở nhiệm vụ <ArrowRight size={15} /></span></Link>
        <Link to="/bang-xep-hang" className="surface"><Trophy size={25} /><h3>Dấu chân cộng đồng</h3><p>Theo dõi điểm di sản và thứ hạng của bạn.</p><span className="text-link">Xem xếp hạng <ArrowRight size={15} /></span></Link>
      </div>
    </section>
    <section className="home-section field-notes">
      <div><Compass size={35} strokeWidth={1.2} /><p className="eyebrow">MỘT HÀNH TRÌNH CỦA RIÊNG BẠN</p><h2>Đi, đọc và lưu lại.</h2>
        <p>Lên lịch ghé thăm, ghi lại nơi đã đến và thêm ảnh vào sổ tay của bạn.</p><Link to="/hanh-trinh" className="text-link"><BookOpen size={18} /> Mở hành trình <ArrowRight size={16} /></Link></div>
      <div><ScrollText size={35} strokeWidth={1.2} /><p className="eyebrow">TƯ LIỆU CỘNG ĐỒNG</p><h2>Góp một câu chuyện.</h2>
        <p>Gửi ảnh hoặc thông tin về một di sản bạn biết để cán bộ văn hóa xem xét và công bố.</p>
        <Link to="/dong-gop" className="text-link">Gửi đóng góp <ArrowRight size={16} /></Link></div>
    </section>
  </>
}
