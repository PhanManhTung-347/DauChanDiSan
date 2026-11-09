import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { AlertCircle, ArrowRight, Compass, ImageOff, LoaderCircle, MapPin } from 'lucide-react'
import { Link } from 'react-router-dom'
import { mediaUrl, privateMedia } from './api'
import type { Gallery } from './types'

export function Feedback({ error, message }: { error?: string; message?: string }) {
  if (!error && !message) return null
  return <p className={error ? 'feedback error' : 'feedback success'} role={error ? 'alert' : 'status'}>
    {error && <AlertCircle size={17} />}{error || message}</p>
}

export function LoadState({ loading, error, retry }: { loading?: boolean; error?: string; retry?: () => void }) {
  if (loading) return <div className="load-state" role="status"><LoaderCircle className="spinner" size={20} /> Đang tải dữ liệu…</div>
  if (error) return <div><Feedback error={error} />{retry && <button className="button quiet" onClick={retry}>Thử lại</button>}</div>
  return null
}

export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return <div className="empty"><Compass size={27} strokeWidth={1.2} /><h3>{title}</h3>{children}</div>
}

export function PageTitle({ eyebrow, title, text, children }: { eyebrow: string; title: string; text?: string; children?: ReactNode }) {
  return <div className="page-heading"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1>{text && <p>{text}</p>}</div>{children}</div>
}

const statuses: Record<string, string> = {
  CHO_DUYET: 'Chờ duyệt', DA_DUYET: 'Đã duyệt', TU_CHOI: 'Từ chối', DA_RUT: 'Đã rút',
  CONG_KHAI: 'Công khai', AN: 'Đang ẩn', NHAP: 'Bản nháp', DANG_DI: 'Đang đi',
  LEN_KE_HOACH: 'Đang lên kế hoạch', HOAN_THANH: 'Hoàn thành', DA_HUY: 'Đã hủy',
  CHO_XAC_NHAN: 'Chờ xác nhận', DA_XAC_NHAN: 'Đã xác nhận', DA_THAM_GIA: 'Đã tham gia',
}
export function Status({ value }: { value: string }) { return <span className={`badge state-${value.toLowerCase()}`}>{statuses[value] ?? value}</span> }

const curated = {
  'pho-co-hoi-an': '/images/di-san/hoi-an.webp',
  'thanh-dia-my-son': '/images/di-san/my-son.webp',
  'dai-noi-hue': '/images/di-san/hue.webp',
  'hoang-thanh-thang-long': '/images/di-san/thang-long.webp',
  'lang-gom-thanh-ha': '/images/di-san/thanh-ha.webp',
  'lang-gom-bat-trang': '/images/di-san/bat-trang.webp',
} as Record<string, string>

export function Cover({ slug, path, alt }: { slug: string; path?: string; alt: string }) {
  const [failed, setFailed] = useState<string>()
  const picture = path && !path.startsWith('demo/') ? mediaUrl(path) : curated[slug]
  if (!picture || failed === picture) return <div className="cover-fallback" aria-hidden="true"><Compass size={44} strokeWidth={1} /></div>
  return <img loading="lazy" src={picture} alt={alt} onError={() => setFailed(picture)} />
}
export function MediaGallery({ items, title }: { items: Gallery[]; title: string }) {
  const available = items.filter(item => (item.anhPath ?? item.videoPath) && !(item.anhPath ?? item.videoPath)?.startsWith('demo/'))
  if (!available.length) return null
  return <div className="photo-grid">{available.map(item =>
    <figure key={item.mediaId ?? item.anhPath ?? item.videoPath}>
      {item.anhPath ? <img loading="lazy" src={mediaUrl(item.anhPath)} alt={item.chuThich ?? title} />
        : <video controls playsInline preload="metadata" src={mediaUrl(item.videoPath!)} aria-label={item.chuThich ?? title} />}
      {item.chuThich && <figcaption>{item.chuThich}</figcaption>}
    </figure>)}</div>
}

export function ProtectedPhoto({ path, alt }: { path: string; alt: string }) {
  const [loaded, setLoaded] = useState<{ path: string; url: string }>()
  const [failed, setFailed] = useState<string>()
  useEffect(() => {
    const abort = new AbortController()
    let objectUrl: string | undefined
    privateMedia(path, abort.signal).then(blob => {
      if (abort.signal.aborted) return
      objectUrl = URL.createObjectURL(blob)
      setLoaded({ path, url: objectUrl })
    }).catch(() => { if (!abort.signal.aborted) setFailed(path) })
    return () => { abort.abort(); if (objectUrl) URL.revokeObjectURL(objectUrl) }
  }, [path])
  return loaded?.path === path ? <img className="review-photo" src={loaded.url} alt={alt} /> : failed === path
    ? <div className="load-state"><ImageOff size={18} /> Ảnh chưa khả dụng</div> : <div className="load-state">Đang tải ảnh…</div>
}
export function PlaceCard({ slug, ten, tinhThanh, tomTat, loaiDiSan, anhBiaPath }: {
  slug: string; ten: string; tinhThanh?: string; tomTat?: string; loaiDiSan?: string; anhBiaPath?: string
}) {
  return <Link className="place-card" to={'/di-san/' + slug}>
    <div className="place-cover"><Cover slug={slug} path={anhBiaPath} alt={ten} /></div>
    <div className="place-body"><span className="place-location"><MapPin size={14} />{tinhThanh ?? 'Di sản Việt Nam'}</span>
      <h3>{ten}</h3><p>{tomTat ?? loaiDiSan ?? 'Ghé thăm và đọc thêm câu chuyện về địa điểm.'}</p>
      <span className="place-link">Đọc câu chuyện <ArrowRight size={16} /></span></div>
  </Link>
}
