import { useState } from 'react'
import { ArrowRight, Compass, KeyRound } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { sendEmailVerification, sendPasswordResetEmail, signInWithEmailAndPassword, signOut } from 'firebase/auth'
import { auth, useSession } from './session'
import { api } from '../../shared/api'
import { useAction } from '../../shared/hooks'
import { Feedback } from '../../shared/ui'
import type { Profile } from '../../shared/types'

const demos = [
  ['demo-lan', 'Thành viên', '/tai-khoan'], ['demo-van-hoa', 'Cán bộ văn hóa', '/kiem-duyet'],
  ['demo-admin', 'Quản trị viên', '/quan-tri'], ['demo-doi-tac', 'Đối tác', '/doi-tac'],
]
function landing(p: Profile) {
  if (p.vaiTro.includes('QUAN_TRI')) return '/quan-tri'
  if (p.vaiTro.includes('KIEM_DUYET_VAN_HOA')) return '/kiem-duyet'
  if (p.vaiTro.includes('DOI_TAC')) return '/doi-tac'
  return '/tai-khoan'
}
function friendly(error: unknown): never {
  const code = typeof error === 'object' && error && 'code' in error ? error.code : undefined
  const messages: Record<string,string> = {
    'auth/invalid-credential': 'Email hoặc mật khẩu chưa đúng.',
    'auth/popup-closed-by-user': 'Cửa sổ Google đã đóng trước khi đăng nhập.',
    'auth/popup-blocked': 'Trình duyệt đã chặn cửa sổ đăng nhập Google.',
    'auth/unauthorized-domain': 'Domain chưa được thêm vào Firebase Authorized domains.',
    'auth/account-exists-with-different-credential': 'Hãy đăng nhập bằng phương thức đã đăng ký cho email này.',
  }
  if (typeof code === 'string') throw new Error(messages[code] ?? 'Firebase chưa xử lý được yêu cầu. Hãy thử lại hoặc kiểm tra cấu hình.')
  throw error
}

export function LoginPage() {
  const { profile, login, loginGoogle, loginDemo } = useSession()
  const loc = useLocation(), navigate = useNavigate()
  const register = loc.pathname === '/dang-ky'
  const action = useAction()
  const [email, setEmail] = useState(''), [name, setName] = useState(''), [password, setPassword] = useState('')
  const [challenge, setChallenge] = useState(''), [code, setCode] = useState('')
  const [mode, setMode] = useState<'normal'|'reset'|'legacy'>('normal')
  const from = (loc.state as { from?: string } | null)?.from
  const finish = (next: string) => navigate(from || next, { replace: true })
  async function send() {
    const result = await api<{ maYeuCau: string }>('/dang-ky/gui-ma', 'POST', { email: email.trim() })
    setChallenge(result.maYeuCau); setCode('')
  }

  if (profile) return <div className="surface"><h2>Bạn đang đăng nhập</h2><Link to={from || landing(profile)}>Tiếp tục</Link></div>
  return <div className="login-layout">
    <div className="login-intro"><Compass size={48} strokeWidth={1.2} /><p className="eyebrow">HỘ CHIẾU DI SẢN</p>
      <h1>Mỗi chuyến đi,<br /><em>một trang mới.</em></h1><p>Lưu dấu chân, giữ khoảnh khắc và góp thêm câu chuyện cho di sản Việt Nam.</p></div>
    <section className="surface login-box"><p className="eyebrow">{challenge ? 'BƯỚC 2 / 2 · XÁC MINH EMAIL' : 'TÀI KHOẢN THÀNH VIÊN'}</p>
      <h2>{challenge ? 'Nhập mã xác nhận' : mode === 'reset' ? 'Đặt lại mật khẩu' : mode === 'legacy' ? 'Xác minh tài khoản cũ' : register ? 'Tạo tài khoản' : 'Chào mừng trở lại'}</h2>
      {auth ? <>
        {!challenge && mode === 'normal' && <><button className="button outline google-login" disabled={action.busy} onClick={() => void action.run(async () => {
          try { finish(landing(await loginGoogle())) } catch (error) { friendly(error) }
        }, '')}><span aria-hidden="true">G</span> Tiếp tục với Google</button><div className="auth-divider">hoặc dùng email</div></>}
        <form className="form-stack" onSubmit={e => {
          e.preventDefault()
          void action.run(async () => {
            try {
              if (!auth) throw new Error('Chưa cấu hình Firebase.')
              if (mode === 'reset') { await sendPasswordResetEmail(auth, email.trim()); return }
              if (mode === 'legacy') {
                const { user } = await signInWithEmailAndPassword(auth, email.trim(), password)
                if (user.emailVerified) { finish(landing(await login(email.trim(), password))); return }
                try { await sendEmailVerification(user) } finally { await signOut(auth) }
                return
              }
              if (register && !challenge) { await send(); return }
              if (register) await api('/dang-ky/xac-nhan', 'POST', { email: email.trim(), maYeuCau: challenge, ma: code, hoTen: name, matKhau: password })
              finish(landing(await login(email.trim(), password)))
            } catch (error) { friendly(error) }
          }, mode === 'reset' ? 'Hãy kiểm tra hộp thư để đặt lại mật khẩu.' : mode === 'legacy' ? 'Hãy mở liên kết xác minh trong email.' :
            register && !challenge ? 'Đã gửi mã xác nhận.' : '')
        }}>
          {register && !challenge && <label>Họ và tên<input value={name} onChange={e => setName(e.target.value)} maxLength={120} autoComplete="name" required /></label>}
          {challenge ? <><p>Mã có hiệu lực 10 phút và được gửi tới <strong>{email}</strong>.</p>
            <label>Mã 6 chữ số<input className="otp-input" inputMode="numeric" autoComplete="one-time-code" value={code}
              onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} pattern="[0-9]{6}" required /></label></> : <>
            <label>Email<input type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} maxLength={254} required /></label>
            {mode !== 'reset' && <label>Mật khẩu<input type="password" autoComplete={register ? 'new-password' : 'current-password'} value={password}
              onChange={e => setPassword(e.target.value)} minLength={register ? 8 : 6} maxLength={128} required /></label>}</>}
          <button className="button primary" disabled={action.busy}>{action.busy ? 'Đang xử lý…' : challenge ? 'Xác nhận và tham gia' :
            mode === 'reset' ? 'Gửi email đặt lại mật khẩu' : mode === 'legacy' ? 'Gửi liên kết xác minh' : register ? 'Gửi mã xác nhận' : 'Đăng nhập'} <ArrowRight size={16} /></button>
          {challenge && <div className="inline"><button type="button" className="text-link" disabled={action.busy} onClick={() => void action.run(send, 'Đã gửi mã mới.')}>Gửi lại mã</button>
            <button type="button" className="text-link" onClick={() => { setChallenge(''); setCode('') }}>Sửa email</button></div>}
          {!register && <div className="inline">{mode === 'normal' ? <>
            <button type="button" className="text-link" onClick={() => setMode('reset')}>Quên mật khẩu?</button>
            <button type="button" className="text-link" onClick={() => setMode('legacy')}>Xác minh tài khoản cũ</button></> :
            <button type="button" className="text-link" onClick={() => setMode('normal')}>Về đăng nhập</button>}</div>}
        </form>
      </> : <p>Cần cấu hình Firebase để dùng Google và xác thực email.</p>}
      {!challenge && mode === 'normal' && <p className="auth-switch">{register ? 'Đã có tài khoản?' : 'Bạn mới đến?'} <Link state={loc.state} className="text-link" to={register ? '/dang-nhap' : '/dang-ky'}>{register ? 'Đăng nhập' : 'Tạo tài khoản'}</Link></p>}
      <Feedback error={action.error} message={action.message} />
      {import.meta.env.DEV && !challenge && <details className="demo-form"><summary><KeyRound size={16} /> Tài khoản dùng thử local</summary>
        <form className="form-stack block" onSubmit={e => {
          e.preventDefault(); const f = new FormData(e.currentTarget), uid = String(f.get('uid'))
          void action.run(async () => { await loginDemo({ uid, key: String(f.get('key')) }); finish(demos.find(x => x[0] === uid)?.[2] ?? '/tai-khoan') }, '')
        }}><label>Vai trò<select name="uid">{demos.map(([uid, label]) => <option key={uid} value={uid}>{label}</option>)}</select></label>
          <label>Khóa DevAuth<input name="key" type="password" autoComplete="off" minLength={24} required /></label>
          <button className="button outline" disabled={action.busy}>Vào chế độ dùng thử</button></form>
      </details>}
    </section>
  </div>
}
