import { useState } from 'react'
import { ArrowLeft, ArrowRight, BedDouble, MapPin, Utensils } from 'lucide-react'
import { Link, useLocation, useParams, useSearchParams } from 'react-router-dom'
import { qs } from '../../shared/api'
import { useResource } from '../../shared/hooks'
import { Empty, LoadState, PageTitle } from '../../shared/ui'
import './travel-directory.css'

const root = '/luu-tru-an-uong'
const labels: Record<string, string> = { KHACH_SAN: 'Khách sạn', HOMESTAY: 'Homestay', QUAN_AN: 'Quán ăn' }
const units: Record<string, string> = { PHONG_DEM: 'phòng/đêm', MON: 'món hoặc đồ uống', NGUOI_BUA: 'người/bữa' }
const currency = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })

type Place = {
  dichVuId: number; slug: string; loai: string; ten: string
  diaChi: string | null; moTa: string | null; tinhThanhId: number; tinhThanh: string
  khuVuc: string | null; viDo: number | null; kinhDo: number | null
  laDuLieuDemo: boolean; anhBia: string | null; giaTuVnd: number | null; donViGia: string | null
}
type Area = { tinhThanhId: number; tinhThanh: string; khuVuc: string | null }
type Photo = { anhId: number; anhPath: string; chuThich: string | null }
type Price = {
  mucId: number; nhom: string | null; ten: string; moTa: string | null
  giaVnd: number | null; sucChua: number | null; anhPath: string | null
}
type Directory = { muc: Place[]; trang: number; coTrangSau: boolean }
type Detail = { dichVu: Place; hinhAnh: Photo[]; bangGia: Price[] }

function DirectoryPhoto({ path, name, food, eager = false }: {
  path?: string | null; name: string; food: boolean; eager?: boolean
}) {
  const [failed, setFailed] = useState<string>()
  const safe = path && /^\/images\/dich-vu\/(?:[a-z0-9_-]+\/)*[a-z0-9_.-]+$/i.test(path)
    && !path.includes('..') ? path : null

  if (safe && failed !== safe)
    return <img src={safe} alt={name} loading={eager ? 'eager' : 'lazy'} onError={() => setFailed(safe)} />

  return <div className="td-photo-placeholder" role="img" aria-label="Chưa có ảnh">
    {food ? <Utensils size={38} /> : <BedDouble size={38} />}
    <span>Chưa có ảnh</span>
  </div>
}

function PriceSummary({ place }: { place: Place }) {
  if (place.giaTuVnd == null) return <span className="td-muted">Chưa công bố giá</span>
  const unit = place.donViGia ? units[place.donViGia] : null
  return <span className="td-price">Từ {currency.format(place.giaTuVnd)}
    {unit && <small> / {unit}</small>}</span>
}

function PlaceCard({ place, listSearch }: { place: Place; listSearch: string }) {
  return <article className="td-card">
    <Link to={root + '/' + encodeURIComponent(place.slug)} state={{ listSearch }}
      className="td-card-link" aria-label={'Xem chi tiết ' + place.ten}>
      <div className="td-cover">
        <DirectoryPhoto path={place.anhBia} name={place.ten} food={place.loai === 'QUAN_AN'} />
        {place.laDuLieuDemo && <span className="td-demo">DỮ LIỆU MẪU</span>}
      </div>
      <div className="td-card-body">
        <small className="td-kind">{labels[place.loai] ?? place.loai}</small>
        <h2>{place.ten}</h2>
        <p className="td-address"><MapPin size={16} aria-hidden="true" /> {place.diaChi || place.khuVuc || place.tinhThanh}</p>
        {place.moTa && <p className="td-summary">{place.moTa}</p>}
        <div className="td-card-footer"><PriceSummary place={place} /><ArrowRight size={17} aria-hidden="true" /></div>
      </div>
    </Link>
  </article>
}

export function TravelDirectoryPage() {
  const [params, setParams] = useSearchParams()
  const { search } = useLocation()
  const loai = params.get('loai') ?? ''
  const tinhThanhId = params.get('tinhThanhId') ?? ''
  const khuVuc = params.get('khuVuc') ?? ''
  const q = params.get('q') ?? ''
  const giaToiDa = params.get('giaToiDa') ?? ''
  const sapXep = params.get('sapXep') ?? 'TEN'
  const rawPage = Number(params.get('trang'))
  const trang = Number.isInteger(rawPage) && rawPage >= 1 && rawPage <= 5000 ? rawPage : 1
  const areaData = useResource<Area[]>(root + '/bo-loc')
  const data = useResource<Directory>(root + qs({ loai, tinhThanhId, khuVuc, q, giaToiDa, sapXep, trang }))
  const [cityDraft, setCityDraft] = useState<{ applied: string; value: string }>()
  const [formVersion, setFormVersion] = useState(0)
  const activeCity = cityDraft?.applied === tinhThanhId ? cityDraft.value : tinhThanhId
  const cities = [...new Map((areaData.data ?? []).map(area => [area.tinhThanhId, area.tinhThanh])).entries()]
  const zones = [...new Set((areaData.data ?? [])
    .filter(area => !activeCity || String(area.tinhThanhId) === activeCity)
    .flatMap(area => area.khuVuc ? [area.khuVuc] : []))]
    .sort((a, b) => a.localeCompare(b, 'vi'))

  function updateKind(value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set('loai', value)
    else next.delete('loai')
    next.delete('trang')
    setParams(next)
  }

  function goPage(value: number) {
    const next = new URLSearchParams(params)
    next.set('trang', String(value))
    setParams(next)
  }

  return <div className="travel-directory">
    <PageTitle eyebrow="DANH BẠ DU LỊCH" title="Lưu trú & Ăn uống"
      text="Tìm chỗ ở và quán ăn gần điểm muốn đến. Xem ảnh, giá tham khảo, menu và vị trí." />
    <div className="td-tabs" aria-label="Nhóm dịch vụ">
      {[['', 'Tất cả'], ['LUU_TRU', 'Lưu trú'], ['QUAN_AN', 'Quán ăn']].map(([value, title]) =>
        <button type="button" key={title}
          aria-pressed={loai === value || (value === 'LUU_TRU' && ['KHACH_SAN', 'HOMESTAY'].includes(loai))}
          onClick={() => updateKind(value)}>{title}</button>)}
    </div>

    <form className="surface td-filters" key={params.toString() + ':' + formVersion} onSubmit={event => {
      event.preventDefault()
      const form = new FormData(event.currentTarget)
      const next = new URLSearchParams()
      for (const key of ['loai', 'tinhThanhId', 'khuVuc', 'q', 'giaToiDa', 'sapXep']) {
        const value = String(form.get(key) ?? '').trim()
        if (value && !(key === 'sapXep' && value === 'TEN')) next.set(key, value)
      }
      setCityDraft(undefined)
      setParams(next)
    }}>
      <label className="td-search">Tìm địa điểm
        <input name="q" defaultValue={q} maxLength={100} placeholder="Tên, địa chỉ, Hội An, Huế…" />
      </label>
      <label>Loại dịch vụ
        <select name="loai" defaultValue={loai}>
          <option value="">Tất cả loại</option>
          <option value="LUU_TRU">Tất cả chỗ ở</option>
          <option value="KHACH_SAN">Khách sạn</option>
          <option value="HOMESTAY">Homestay</option>
          <option value="QUAN_AN">Quán ăn</option>
        </select>
      </label>
      <label>Tỉnh thành
        <select name="tinhThanhId" value={activeCity} onChange={event =>
          setCityDraft({ applied: tinhThanhId, value: event.target.value })}>
          <option value="">Tất cả tỉnh thành</option>
          {tinhThanhId && !cities.some(([id]) => String(id) === tinhThanhId) &&
            <option value={tinhThanhId}>Tỉnh thành đã chọn</option>}
          {cities.map(([id, name]) => <option value={id} key={id}>{name}</option>)}
        </select>
      </label>
      <label>Khu vực
        <select name="khuVuc" key={activeCity} defaultValue={activeCity === tinhThanhId ? khuVuc : ''}>
          <option value="">Tất cả khu vực</option>
          {activeCity === tinhThanhId && khuVuc && !zones.includes(khuVuc) && <option value={khuVuc}>{khuVuc}</option>}
          {zones.map(zone => <option key={zone}>{zone}</option>)}
        </select>
      </label>
      <label>Giá từ tối đa (đ)
        <input name="giaToiDa" type="number" min={0} max={99999999999999} step={1}
          defaultValue={giaToiDa} placeholder="Ví dụ 500000" />
      </label>
      <label>Sắp xếp
        <select name="sapXep" defaultValue={sapXep}>
          <option value="TEN">Theo tên</option>
          <option value="GIA_TANG">Giá từ thấp đến cao</option>
          <option value="GIA_GIAM">Giá từ cao đến thấp</option>
        </select>
      </label>
      <div className="td-filter-actions">
        <button type="submit" className="button primary">Áp dụng bộ lọc</button>
        <button type="button" className="button quiet" onClick={() => {
          setCityDraft(undefined)
          setParams({})
          setFormVersion(value => value + 1)
        }}>Xóa bộ lọc</button>
      </div>
      <small className="td-filter-note">Bộ lọc dùng mức “giá từ”. Đơn vị có thể là phòng/đêm, món hoặc người/bữa.</small>
    </form>

    <LoadState {...areaData} retry={areaData.reload} />
    <LoadState {...data} retry={data.reload} />
    {data.data?.muc.length === 0 && <Empty title="Chưa tìm thấy địa điểm phù hợp." />}
    <div className="td-grid">
      {data.data?.muc.map(place => <PlaceCard key={place.dichVuId} place={place} listSearch={search} />)}
    </div>
    {(trang > 1 || data.data?.coTrangSau) && <nav className="td-pagination" aria-label="Phân trang dịch vụ">
      <button type="button" className="button outline" disabled={trang <= 1} onClick={() => goPage(trang - 1)}>Trang trước</button>
      <span>Trang {trang}</span>
      <button type="button" className="button outline" disabled={!data.data?.coTrangSau || trang >= 5000}
        onClick={() => goPage(trang + 1)}>Trang sau</button>
    </nav>}
  </div>
}

export function TravelDirectoryDetailPage() {
  const { slug } = useParams()
  const { state } = useLocation()
  const lastSearch = typeof state?.listSearch === 'string' && state.listSearch.startsWith('?') ? state.listSearch : ''
  const result = useResource<Detail>(slug ? root + '/' + encodeURIComponent(slug) : null)
  const detail = result.data
  const place = detail?.dichVu
  const food = place?.loai === 'QUAN_AN'
  const mapUrl = place?.viDo != null && place.kinhDo != null
    ? 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(place.viDo + ',' + place.kinhDo)
    : null
  const groups = new Map<string, Price[]>()
  for (const item of detail?.bangGia ?? []) {
    const key = item.nhom || (food ? 'Các món' : 'Các loại phòng')
    const current = groups.get(key) ?? []
    current.push(item)
    groups.set(key, current)
  }

  return <div className="travel-directory">
    <Link className="button outline td-back" to={root + lastSearch}>
      <ArrowLeft size={16} aria-hidden="true" /> Về danh sách
    </Link>
    <LoadState {...result} retry={result.reload} />
    {place && detail && <>
      <PageTitle eyebrow={labels[place.loai] ?? place.loai} title={place.ten}
        text={[place.khuVuc, place.tinhThanh].filter(Boolean).join(' · ')} />
      {place.laDuLieuDemo && <p className="td-demo-note">Dữ liệu mẫu. Ảnh và giá chỉ dùng để minh họa.</p>}
      <div className="td-detail-grid">
        <div className="td-detail-cover">
          <DirectoryPhoto path={place.anhBia} name={place.ten} food={food} eager />
        </div>
        <section className="surface td-location">
          <h2>Thông tin & vị trí</h2>
          {place.moTa && <p>{place.moTa}</p>}
          <p className="td-address"><MapPin size={17} aria-hidden="true" /> {place.diaChi || 'Chưa có địa chỉ'}</p>
          {mapUrl ? <>
            <p className="td-muted">Tọa độ {place.viDo}, {place.kinhDo}</p>
            <a className="button outline" href={mapUrl} target="_blank" rel="noopener noreferrer">Mở vị trí trên bản đồ</a>
          </> : <p className="td-muted">Chưa có tọa độ bản đồ.</p>}
          <p><PriceSummary place={place} /></p>
        </section>
      </div>

      {detail.hinhAnh.length > 1 && <section className="surface td-detail-section">
        <h2>Hình ảnh</h2>
        <div className="td-gallery">
          {detail.hinhAnh.map(photo => <figure key={photo.anhId}>
            <div><DirectoryPhoto path={photo.anhPath} name={photo.chuThich || place.ten} food={food} /></div>
            {photo.chuThich && <figcaption>{photo.chuThich}</figcaption>}
          </figure>)}
        </div>
      </section>}

      <section className="surface td-detail-section">
        <h2>{food ? 'Menu & giá món' : 'Loại phòng & giá tham khảo'}</h2>
        {groups.size === 0 && <p className="td-muted">{food ? 'Chưa có menu chi tiết.' : 'Chưa có bảng giá theo loại phòng.'}</p>}
        {[...groups].map(([name, items]) => <section className="td-price-group" key={name}>
          <h3>{name}</h3>
          <ul>
            {items.map(item => <li key={item.mucId}>
              {item.anhPath && <div className="td-price-photo">
                <DirectoryPhoto path={item.anhPath} name={item.ten} food={food} />
              </div>}
              <div>
                <strong>{item.ten}</strong>
                {item.moTa && <p>{item.moTa}</p>}
                {item.sucChua != null && <small>Tối đa {item.sucChua} người</small>}
              </div>
              <span className="td-price">{item.giaVnd == null ? 'Chưa có giá' :
                currency.format(item.giaVnd) + (food ? '' : ' / đêm')}</span>
            </li>)}
          </ul>
        </section>)}
        <small className="td-muted">Giá hiển thị để tham khảo và có thể thay đổi.</small>
      </section>
    </>}
  </div>
}