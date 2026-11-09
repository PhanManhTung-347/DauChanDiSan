import { ArrowLeft, Building2, Files, Landmark, ListChecks, ShieldCheck, UsersRound } from 'lucide-react'
import { Link, NavLink, Outlet } from 'react-router-dom'

export function WorkspaceLayout({ area }: { area: 'admin' | 'culture' | 'partner' }) {
  const items = area === 'admin'
    ? [{ to:'/quan-tri', name:'Tài khoản và phân quyền', icon:UsersRound }, { to:'/quan-tri/don-doi-tac', name:'Đơn đăng ký đối tác', icon:Building2 },
       { to:'/quan-tri/noi-dung', name:'Nội dung và nhiệm vụ', icon:Files }, { to:'/kiem-duyet', name:'Sang khu vực văn hóa', icon:Landmark }]
    : area === 'culture'
      ? [{ to:'/kiem-duyet', name:'Hàng chờ đóng góp', icon:ListChecks }, { to:'/kiem-duyet/di-san', name:'Di sản và đánh giá', icon:Landmark }]
      : [{ to:'/doi-tac', name:'Trải nghiệm và lịch', icon:Building2 }, { to:'/dang-ky-doi-tac', name:'Hồ sơ đối tác', icon:Files }]
  return <div className="workspace-shell"><aside className="workspace-side">
    <div className="workspace-brand"><span><ShieldCheck size={23} /></span><div><small>KHÔNG GIAN LÀM VIỆC</small>
      <strong>{area==='admin'?'Quản trị hệ thống':area==='culture'?'Ban văn hóa':'Đối tác địa phương'}</strong></div></div>
    <nav aria-label="Điều hướng khu vực làm việc">{items.map(({to,name,icon:Icon}) => <NavLink end to={to} key={to}><Icon size={18} />{name}</NavLink>)}</nav>
    <Link className="workspace-home" to="/"><ArrowLeft size={16} /> Về trang công khai</Link>
  </aside><div className="workspace-content"><Outlet /></div></div>
}
