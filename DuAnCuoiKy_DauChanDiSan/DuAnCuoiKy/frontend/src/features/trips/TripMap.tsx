import { useEffect, useMemo, useRef, useState } from 'react'
import * as L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { vnDate } from '../../shared/format'
import { privateMedia } from '../../shared/api'

type Coordinates = { viDo?: number | null; kinhDo?: number | null }

export type TripMapPoint = Coordinates & {
  ngayDuKien: string
  thuTu: number
  ten: string
  gioDuKien?: string | null
  ghiChu?: string | null
}

export type TripMapCheckIn = Coordinates & {
  checkInId: number
  tenDiSan: string
  ngayDiaPhuong?: string | null
  thoiGianUtc: string
  anhPath?: string | null
  toaDoThamChieu?: boolean
}

type Props = {
  points: readonly TripMapPoint[]
  checkIns?: readonly TripMapCheckIn[]
  onOpenCheckIn?: (id: number) => void
}

const noCheckIns: readonly TripMapCheckIn[] = []
const defaultCenter: L.LatLngTuple = [16.05, 108.22]
const osm = {
  name: 'OpenStreetMap',
  url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
}
const stadia = {
  name: 'Stadia Maps',
  url: 'https://tiles.stadiamaps.com/tiles/alidade_smooth/{z}/{x}/{y}{r}.png',
  attribution: '&copy; <a href="https://stadiamaps.com/attribution/">Stadia Maps</a>'
    + ' &copy; <a href="https://openmaptiles.org/">OpenMapTiles</a> ' + osm.attribution,
}

function tileSources(hostname: string) {
  const sources = ['localhost', '127.0.0.1', '[::1]'].includes(hostname) ? [stadia, osm] : [osm]
  const url = import.meta.env.VITE_TRIP_MAP_TILE_URL?.trim()
  if (!url) return sources

  const custom = url.includes('stadiamaps.com/tiles/') ? stadia
    : url.includes('tile.openstreetmap.org/') ? osm
      : { name: 'nguồn đã cấu hình', attribution: import.meta.env.VITE_TRIP_MAP_TILE_ATTRIBUTION?.trim() ?? '' }
  return [{ ...custom, url }, ...sources.filter(source => source.url !== url)]
}

function mapPosition({ viDo, kinhDo }: Coordinates): L.LatLngTuple | undefined {
  if (typeof viDo !== 'number' || typeof kinhDo !== 'number'
    || !Number.isFinite(viDo) || !Number.isFinite(kinhDo)
    || Math.abs(viDo) > 90 || Math.abs(kinhDo) > 180) return undefined
  return [viDo, kinhDo]
}

function popup(title: string, subtitle: string, note?: string | null) {
  const content = document.createElement('div')
  const heading = document.createElement('strong')
  heading.textContent = title
  const detail = document.createElement('div')
  detail.textContent = subtitle
  content.append(heading, detail)
  if (note) {
    const text = document.createElement('p')
    text.textContent = note
    content.append(text)
  }
  return content
}

function pin(text: string, color: string) {
  const node = document.createElement('span')
  node.textContent = text
  Object.assign(node.style, {
    display: 'grid', placeItems: 'center', width: '100%', height: '100%',
    boxSizing: 'border-box', border: '2px solid white', borderRadius: '50%',
    background: color, color: '#fff', fontWeight: '700', fontSize: '13px',
    overflow: 'hidden', boxShadow: '0 2px 8px #0005',
  })
  return node
}

export function TripMap({ points, checkIns = noCheckIns, onOpenCheckIn }: Props) {
  const host = useRef<HTMLDivElement>(null)
  const map = useRef<L.Map | null>(null)
  const [day, setDay] = useState('')
  const [streetMap, setStreetMap] = useState(import.meta.env.VITE_TRIP_MAP_TILES !== 'off')
  const [sources] = useState(() => tileSources(window.location.hostname))
  const [sourceIndex, setSourceIndex] = useState(0)
  const [tilesFailed, setTilesFailed] = useState(false)
  const source = sources[sourceIndex]

  const days = useMemo(() => [...new Set([
    ...points.map(p => p.ngayDuKien),
    ...checkIns.flatMap(p => p.ngayDiaPhuong ? [p.ngayDiaPhuong] : []),
  ])].filter(Boolean).sort(), [points, checkIns])
  const activeDay = days.includes(day) ? day : ''

  const planned = useMemo(() => points.filter(p => !activeDay || p.ngayDuKien === activeDay)
    .slice().sort((a, b) => a.ngayDuKien.localeCompare(b.ngayDuKien) || a.thuTu - b.thuTu),
  [points, activeDay])
  const visited = useMemo(() => checkIns.filter(p => !activeDay || p.ngayDiaPhuong === activeDay)
    .slice().sort((a, b) => Date.parse(a.thoiGianUtc) - Date.parse(b.thoiGianUtc) || a.checkInId - b.checkInId),
  [checkIns, activeDay])
  const hidden = [...planned, ...visited].filter(p => !mapPosition(p)).length

  useEffect(() => {
    if (!host.current) return
    const instance = L.map(host.current, { scrollWheelZoom: false, maxZoom: 19 })
      .setView(defaultCenter, 6)
    map.current = instance
    const observer = new ResizeObserver(() => instance.invalidateSize({ pan: false }))
    observer.observe(host.current)
    return () => {
      observer.disconnect()
      instance.remove()
      map.current = null
    }
  }, [])

  useEffect(() => {
    const instance = map.current
    if (!instance || !streetMap) return

    const layer = L.tileLayer(source.url, {
      attribution: source.attribution, maxNativeZoom: 19, maxZoom: 19,
      referrerPolicy: 'strict-origin-when-cross-origin',
    })
    let loaded = 0
    let failed = 0
    layer.on('loading', () => { loaded = 0; failed = 0 })
    layer.on('tileload', () => { loaded++ })
    layer.on('tileerror', () => { failed++ })
    layer.on('load', () => {
      if (failed > 0 && loaded === 0 && sourceIndex + 1 < sources.length) {
        setSourceIndex(sourceIndex + 1)
        return
      }
      setTilesFailed(failed > 0)
    })
    layer.addTo(instance)
    return () => { layer.remove() }
  }, [streetMap, source, sourceIndex, sources.length])

  useEffect(() => {
    const instance = map.current
    if (!instance) return
    const group = L.layerGroup().addTo(instance)
    const abort = new AbortController()
    const photos: string[] = []
    const all: L.LatLngTuple[] = []
    const checkInPath: L.LatLngTuple[] = []

    // Mỗi ngày là một nhóm tuyến dự kiến riêng.
    const paths = new Map<string, L.LatLngTuple[]>()
    for (const p of planned) {
      const position = mapPosition(p)
      if (!position) continue
      all.push(position)
      const path = paths.get(p.ngayDuKien) ?? []
      path.push(position)
      paths.set(p.ngayDuKien, path)
      const title = p.ten
      const subtitle = [vnDate(p.ngayDuKien), p.gioDuKien?.slice(0, 5)].filter(Boolean).join(' · ')
      L.marker(position, {
        title, alt: title,
        icon: L.divIcon({ className: '', html: pin(String(p.thuTu), '#245dab'),
          iconSize: [28, 28], iconAnchor: [14, 14], popupAnchor: [0, -15] }),
      }).bindPopup(popup(title, subtitle, p.ghiChu)).addTo(group)
    }

    for (const path of paths.values()) {
      if (path.length > 1) L.polyline(path, { color: '#245dab', weight: 4, dashArray: '7 8' }).addTo(group)
    }

    for (const p of visited) {
      const position = mapPosition(p)
      if (!position) continue
      all.push(position)
      checkInPath.push(position)
      const title = 'Check-in ' + p.tenDiSan
      const marker = pin('📷', '#9c1a36')
      const detail = [vnDate(p.ngayDiaPhuong ?? p.thoiGianUtc),
        p.toaDoThamChieu ? 'Vị trí tham chiếu tại di sản' : 'Vị trí check-in'].join(' · ')
      L.marker(position, {
        title, alt: title, zIndexOffset: 500,
        icon: L.divIcon({ className: '', html: marker, iconSize: [38, 38],
          iconAnchor: [19, 19], popupAnchor: [0, -20] }),
      }).bindPopup(popup(title, detail))
        .on('click', () => onOpenCheckIn?.(p.checkInId)).addTo(group)

      if (p.anhPath && !p.anhPath.startsWith('demo/')) {
        void privateMedia(p.anhPath, abort.signal).then(blob => {
          if (abort.signal.aborted) return
          const url = URL.createObjectURL(blob)
          photos.push(url)
          const photo = document.createElement('img')
          photo.alt = ''
          photo.style.cssText = 'width:100%;height:100%;object-fit:cover'
          photo.onload = () => marker.replaceChildren(photo)
          photo.src = url
        }).catch(() => { /* Ghim máy ảnh vẫn dùng được. */ })
      }
    }

    if (checkInPath.length > 1) {
      L.polyline(checkInPath, { color: '#9c1a36', weight: 3, dashArray: '3 8' }).addTo(group)
    }

    if (all.length > 0) {
      instance.fitBounds(L.latLngBounds(all), { padding: [36, 36], maxZoom: 15, animate: false })
    } else {
      instance.setView(defaultCenter, 6, { animate: false })
    }

    return () => {
      abort.abort()
      group.remove()
      photos.forEach(url => URL.revokeObjectURL(url))
    }
  }, [planned, visited, onOpenCheckIn])

  return <section className="form-stack" aria-label="Bản đồ hành trình">
    <h3>Bản đồ hành trình</h3>
    <div className="inline">
      <label>Ngày trên bản đồ
        <select value={activeDay} onChange={e => setDay(e.target.value)}>
          <option value="">Tất cả ngày</option>
          {days.map(value => <option key={value} value={value}>{vnDate(value)}</option>)}
        </select>
      </label>
      <button type="button" className="button outline" onClick={() => {
        setTilesFailed(false)
        setSourceIndex(0)
        setStreetMap(value => !value)
      }}>
        {streetMap ? 'Ẩn nền đường phố' : 'Hiện nền đường phố'}
      </button>
    </div>
    <div ref={host} aria-label="Vị trí các điểm hành trình" style={{
      height: 'clamp(320px, 50vh, 520px)', width: '100%', borderRadius: 14,
      border: '1px solid #ddcfba', position: 'relative', zIndex: 0,
      backgroundColor: '#f2eee4',
      backgroundImage: 'linear-gradient(#ded7ca 1px, transparent 1px), linear-gradient(90deg, #ded7ca 1px, transparent 1px)',
      backgroundSize: '28px 28px',
    }} />
    <small className="muted">Xanh · nối điểm dự kiến theo ngày. Đỏ · nối các check-in theo thời gian.</small>
    {streetMap && tilesFailed && <small className="muted" role="status">
      Một số ô nền từ {source.name} chưa tải được. Các điểm có tọa độ vẫn hiển thị.
    </small>}
    {hidden > 0 && <small className="muted">{hidden} điểm chưa có tọa độ để ghim.</small>}
    {planned.length === 0 && visited.length === 0 &&
      <small className="muted">Thêm điểm đến hoặc gắn check-in để xem trên bản đồ.</small>}
  </section>
}

export default TripMap
