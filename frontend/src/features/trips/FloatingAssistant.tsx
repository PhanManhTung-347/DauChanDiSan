import { useEffect, useId, useRef, useState } from 'react'
import { Bot, MessageCircle, Plus, Send, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { api, errorText } from '../../shared/api'
import { today } from '../../shared/format'
import './chat-trip.css'

type Reference = { loai: string; id: number; tieuDe?: string; url?: string | null }
type Reply = { tinNhanAiId: number; traLoi: string; nguon: Reference[]; diem: { ngay: number }[] }
type Message = { id: number; user: boolean; text: string; link?: { to: string; label: string }; reply?: Reply; days?: number }
type Topic = { title: string; pattern: RegExp; answer: string; to: string; label: string }

const topics: Topic[] = [
  { title: 'Check-in ở đâu?', pattern: /\b(check in|ghi dau)\b/,
    answer: 'Mở trang Check-in, chọn di sản và cho trình duyệt quyền lấy vị trí. Khi GPS đã sẵn sàng, bấm Check-in. Bạn có thể thêm ảnh và ghi chú.',
    to: '/check-in', label: 'Mở Check-in' },
  { title: 'Cách lưu hành trình?', pattern: /\b(luu hanh trinh|luu de xuat|tao hanh trinh thu cong)\b/,
    answer: 'Mở Hành trình của tôi, chọn Tạo mới rồi thêm các điểm theo ngày. Nếu trợ lý đã lập lịch, bạn cũng có thể bấm Lưu vào hành trình ngay trong ô chat.',
    to: '/hanh-trinh', label: 'Mở Hành trình của tôi' },
  { title: 'Gửi đóng góp thế nào?', pattern: /\b(dong gop|gui tu lieu)\b/,
    answer: 'Mở trang Đóng góp, chọn di sản, điền thông tin và gửi để chờ duyệt. Bạn có thể xem trạng thái ngay trên trang đó.',
    to: '/dong-gop', label: 'Mở Đóng góp' },
  { title: 'Sổ tay ở đâu?', pattern: /\b(so tay|nhat ky)\b/,
    answer: 'Mở Sổ tay, chọn hành trình rồi thêm ghi chú hoặc ảnh. Bạn có thể gắn ghi chép vào một lần check-in của chuyến đi.',
    to: '/so-tay', label: 'Mở Sổ tay' },
]
const tripSuggestions = [
  'Lên lịch Huế 2 ngày cho 2 người, ngân sách 3 triệu.',
  'Lập hành trình Hội An 3 ngày có chỗ ăn và chỗ ở.',
]
const words = (s: string) => s.toLowerCase().normalize('NFD')
  .replace(/\p{Diacritic}/gu, '').replace(/đ/g, 'd').replace(/[^a-z0-9]+/g, ' ')

function tripDays(text: string, fallback: number) {
  const match = words(text).match(/\b(\d{1,2}|mot|hai|ba|bon|nam|sau|bay)\s+ngay\b/)
  if (!match) return fallback
  const spoken: Record<string, number> = { mot: 1, hai: 2, ba: 3, bon: 4, nam: 5, sau: 6, bay: 7 }
  return Math.max(1, Math.min(14, spoken[match[1]] ?? Number(match[1])))
}

function dayAfter(start: string, offset: number) {
  return new Date(Date.parse(start) + offset * 86400000).toISOString().slice(0, 10)
}

export function FloatingAssistant({ authenticated }: { authenticated: boolean }) {
  const [open, setOpen] = useState(false)
  const [chatId, setChatId] = useState<number>()
  const [messages, setMessages] = useState<Message[]>([])
  const [draft, setDraft] = useState('')
  const [days, setDays] = useState(1)
  const [pending, setPending] = useState<string>()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string>()
  const [saved, setSaved] = useState<Record<number, number>>({})
  const request = useRef<AbortController | null>(null)
  const sequence = useRef(0)
  const launcher = useRef<HTMLButtonElement>(null)
  const input = useRef<HTMLTextAreaElement>(null)
  const thread = useRef<HTMLDivElement>(null)
  const dialogId = useId()

  useEffect(() => () => request.current?.abort(), [])
  useEffect(() => { if (open) input.current?.focus() }, [open])
  useEffect(() => {
    if (open && thread.current) thread.current.scrollTop = thread.current.scrollHeight
  }, [open, messages, pending])

  function close() {
    setOpen(false)
    launcher.current?.focus()
  }

  function append(question: string, reply: Omit<Message, 'id' | 'user'>) {
    const userId = ++sequence.current
    const assistantId = ++sequence.current
    setMessages(current => [...current, { id: userId, user: true, text: question },
      { ...reply, id: assistantId, user: false }])
  }

  async function send(raw: string) {
    const question = raw.trim()
    if (!question || request.current) return
    setError(undefined)
    const normalized = words(question)
    const planning = /\b(lich trinh|hanh trinh|xep lich|len lich|lap lich|lap chuyen)\b/.test(normalized)
    const support = /\b(cach|lam sao|o dau|the nao|huong dan)\b/.test(normalized)
    const topic = topics.find(item => item.pattern.test(normalized) && (!planning || support || item === topics[1]))
    if (topic) {
      append(question, { text: topic.answer, link: { to: topic.to, label: topic.label } })
      setDraft('')
      return
    }
    if (!authenticated) {
      append(question, { text: 'Bạn có thể hỏi cách dùng hệ thống ngay. Đăng nhập để lập lịch và lưu hành trình.',
        link: { to: '/dang-nhap', label: 'Đăng nhập' } })
      setDraft('')
      return
    }

    const abort = new AbortController()
    request.current = abort
    setBusy(true)
    setPending(question)
    setDraft(question)
    try {
      let id = chatId
      if (id === undefined) {
        const row = await api<{ hoiThoaiId: number }>('/ai/hoi-thoai', 'POST', {
          tieuDe: [...question].slice(0, 70).join(''),
        }, abort.signal)
        id = row.hoiThoaiId
        if (abort.signal.aborted) return
        setChatId(id)
      }

      const count = tripDays(question, days)
      const reply = await api<Reply>('/ai/hoi-thoai/' + id + '/tin-nhan', 'POST',
        { cauHoi: question, soNgay: count }, abort.signal)
      if (abort.signal.aborted) return
      append(question, { text: reply.traLoi, reply, days: count })
      setDraft('')
    } catch (err) {
      if (!abort.signal.aborted) setError(errorText(err))
    } finally {
      if (!abort.signal.aborted) { setBusy(false); setPending(undefined) }
      request.current = null
    }
  }

  async function save(message: Message, start: string) {
    const reply = message.reply
    if (!reply?.diem.length || !start || request.current || saved[reply.tinNhanAiId]) return
    const abort = new AbortController()
    request.current = abort
    setBusy(true)
    setError(undefined)
    try {
      const length = Math.max(message.days ?? 1, ...reply.diem.map(p => p.ngay))
      const trip = await api<{ hanhTrinhId: number }>('/hanh-trinh', 'POST', {
        tinNhanAiId: reply.tinNhanAiId, tieuDe: 'Hành trình được trợ lý gợi ý',
        ngayBatDau: start, ngayKetThuc: dayAfter(start, length - 1),
      }, abort.signal)
      if (!abort.signal.aborted) setSaved(current => ({ ...current, [reply.tinNhanAiId]: trip.hanhTrinhId }))
    } catch (err) {
      if (!abort.signal.aborted) setError(errorText(err))
    } finally {
      if (!abort.signal.aborted) setBusy(false)
      request.current = null
    }
  }

  return <aside className="dcds-chat" aria-label="Hỗ trợ người dùng"
    onKeyDown={event => { if (event.key === 'Escape' && open) close() }}>
    {open && <section id={dialogId} className="dcds-chat-window" role="dialog" aria-labelledby={dialogId + '-title'}>
      <header className="dcds-chat-header">
        <span className="dcds-chat-avatar"><Bot size={21} /></span>
        <div><strong id={dialogId + '-title'}>Trợ lý Dấu Chân Di Sản</strong>
          <small>Hỗ trợ sử dụng và gợi ý chuyến đi</small></div>
        <button type="button" title="Cuộc trò chuyện mới" aria-label="Cuộc trò chuyện mới" disabled={busy}
          onClick={() => { setChatId(undefined); setMessages([]); setDraft(''); setError(undefined); setSaved({}); input.current?.focus() }}>
          <Plus size={19} />
        </button>
        <button type="button" aria-label="Đóng chat" onClick={close}><X size={20} /></button>
      </header>
      <div ref={thread} className="dcds-chat-thread" role="log" aria-live="polite" aria-label="Tin nhắn">
        <p className="dcds-chat-bubble">Chào bạn! Mình có thể hướng dẫn check-in, lưu sổ tay hoặc gợi ý hành trình có chỗ ăn và chỗ ở.</p>
        {messages.length === 0 && <div className="dcds-chat-chips">
          {[...topics.map(topic => topic.title), ...tripSuggestions].map(question =>
            <button type="button" key={question} disabled={busy} onClick={() => void send(question)}>{question}</button>)}
        </div>}
        {messages.map(message => <article className={'dcds-chat-bubble' + (message.user ? ' dcds-chat-user' : '')} key={message.id}>
          <p>{message.text}</p>
          {message.link && <Link to={message.link.to} onClick={close}>{message.link.label}</Link>}
          {!!message.reply?.nguon.length && <ul className="dcds-chat-sources">
            {message.reply.nguon.map(source => <li key={source.loai + '-' + source.id}>
              {source.url && /^\/(di-san|trai-nghiem)\/[a-z0-9-]+$/i.test(source.url)
                ? <Link to={source.url} onClick={close}>{source.tieuDe ?? source.loai}</Link> : (source.tieuDe ?? source.loai)}
            </li>)}
          </ul>}
          {!!message.reply?.diem.length && (saved[message.reply.tinNhanAiId]
            ? <Link to={'/hanh-trinh?hanhTrinhId=' + saved[message.reply.tinNhanAiId]} onClick={close}>Mở hành trình vừa lưu</Link>
            : <form className="dcds-chat-save" onSubmit={event => {
              event.preventDefault()
              void save(message, String(new FormData(event.currentTarget).get('start') ?? ''))
            }}>
              <label>Ngày khởi hành<input type="date" name="start" defaultValue={today()} required /></label>
              <button type="submit" disabled={busy}>Lưu vào hành trình</button>
            </form>)}
        </article>)}
        {pending && <><p className="dcds-chat-bubble dcds-chat-user">{pending}</p>
          <p className="dcds-chat-bubble">Đang tìm dữ liệu hành trình…</p></>}
      </div>
      <form className="dcds-chat-compose" onSubmit={event => { event.preventDefault(); void send(draft) }}>
        {error && <p className="dcds-chat-error" role="alert">{error}</p>}
        <label>Số ngày mặc định
          <input type="number" min={1} max={14} value={days} disabled={busy}
            onChange={event => setDays(Math.max(1, Math.min(14, Number(event.target.value) || 1)))} />
        </label>
        <div>
          <textarea ref={input} rows={2} aria-label="Câu hỏi của bạn" placeholder="Nhập câu hỏi của bạn…"
            value={draft} disabled={busy} maxLength={2500} onChange={event => setDraft(event.target.value)}
            onKeyDown={event => {
              if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
                event.preventDefault()
                void send(draft)
              }
            }} />
          <button type="submit" aria-label="Gửi câu hỏi" disabled={busy || !draft.trim()}><Send size={18} /></button>
        </div>
      </form>
    </section>}
    <button className="dcds-chat-launcher" type="button" ref={launcher} aria-expanded={open}
      aria-controls={open ? dialogId : undefined} onClick={() => open ? close() : setOpen(true)}>
      <MessageCircle size={22} /><span>{open ? 'Ẩn trợ lý' : 'Trợ lý di sản'}</span>
    </button>
  </aside>
}