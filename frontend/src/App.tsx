import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import {
  ArrowRight,
  BookOpen,
  Bookmark,
  Building2,
  Camera,
  ChevronDown,
  Compass,
  Landmark,
  LogOut,
  Map,
  Menu,
  PencilLine,
  ShieldCheck,
  Ticket,
  Trophy,
  X,
} from 'lucide-react'
import {
  BrowserRouter,
  Link,
  NavLink,
  Navigate,
  Outlet,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom'

import { AdminPage, MissionsPage } from './features/accounts/AccountPages'
import { AdminContent } from './features/accounts/AdminContent'
import { LoginPage } from './features/accounts/LoginPage'
import {
  Avatar,
  LeaderboardPage,
  ProfilePage,
} from './features/accounts/PassportPages'
import { SessionProvider } from './features/accounts/SessionProvider'
import { WorkspaceLayout } from './features/accounts/WorkspaceLayout'
import { useSession } from './features/accounts/session'
import {
  CheckInPage,
  JournalsPage,
  SharedJournalPage,
} from './features/checkins/CheckInPages'
import {
  ContributionsPage,
  ModerationPage,
} from './features/discovery/ContributionPages'
import { CultureCatalog } from './features/discovery/CultureCatalog'
import {
  ExplorePage,
  FavoritesPage,
  HeritagePage,
} from './features/discovery/DiscoveryPages'
import { HomePage } from './features/discovery/HomePage'
import {
  BookingsPage,
  ExperiencePage,
  ExperiencesPage,
  PartnerPage,
} from './features/experiences/ExperiencePages'
import {
  PartnerApplicationPage,
  PartnerApprovalPage,
} from './features/experiences/PartnerApplications'
import { FloatingAssistant } from './features/trips/FloatingAssistant'
import { AssistantPage, TripsPage } from './features/trips/TripPages'
import type { Role } from './shared/types'
import { Empty, LoadState, PageTitle } from './shared/ui'

function Brand() {
  return (
    <Link to="/" className="brand" aria-label="Dấu Chân Di Sản, trang chủ">
      <span className="brand-mark">
        <Compass size={26} strokeWidth={1.3} />
      </span>
      <span>
        <strong>Dấu Chân Di Sản</strong>
        <small>HÀNH TRÌNH QUA KÝ ỨC</small>
      </span>
    </Link>
  )
}

function Layout() {
  const { profile, logout } = useSession()
  const location = useLocation()
  const [menuAt, setMenuAt] = useState<string>()
  const [accountAt, setAccountAt] = useState<string>()

  const show = menuAt === location.key
  const accountOpen = accountAt === location.key
  const reviewer = profile?.vaiTro.some(
    role => role === 'QUAN_TRI' || role === 'KIEM_DUYET_VAN_HOA',
  )
  const admin = profile?.vaiTro.includes('QUAN_TRI')
  const partner = profile?.vaiTro.includes('DOI_TAC')
  const workspace = /^\/(quan-tri|kiem-duyet|doi-tac)(?:\/|$)/.test(location.pathname)

  const mainClassName = workspace
    ? 'container workspace-main'
    : location.pathname === '/'
      ? 'container home'
      : 'container page'

  useEffect(() => {
    if (!location.hash) window.scrollTo(0, 0)
  }, [location.pathname, location.hash])

  useEffect(() => {
    if (!accountOpen) return

    const close = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setAccountAt(undefined)
    }

    const outside = (event: PointerEvent) => {
      if (event.target instanceof Element && !event.target.closest('.account-menu')) {
        setAccountAt(undefined)
      }
    }

    window.addEventListener('keydown', close)
    window.addEventListener('pointerdown', outside)

    return () => {
      window.removeEventListener('keydown', close)
      window.removeEventListener('pointerdown', outside)
    }
  }, [accountOpen])

  return (
    <>
      <a className="skip-link" href="#noi-dung">Bỏ qua điều hướng</a>

      <header className="site-header">
        <div className="header-inner">
          <Brand />

          <button
            type="button"
            className="menu-toggle"
            aria-expanded={show}
            aria-controls="site-navigation"
            aria-label={show ? 'Đóng menu' : 'Mở menu'}
            onClick={() => setMenuAt(show ? undefined : location.key)}
          >
            {show ? <X /> : <Menu />}
          </button>

          <div className={show ? 'nav-group open' : 'nav-group'} id="site-navigation">
            <nav aria-label="Điều hướng chính">
              <NavLink to="/" end>Trang chủ</NavLink>
              <NavLink to="/kham-pha">Khám phá</NavLink>
              <NavLink to="/trai-nghiem">Trải nghiệm</NavLink>
              <NavLink to="/nhiem-vu">Nhiệm vụ</NavLink>
              <NavLink to="/bang-xep-hang">
                <Trophy size={15} /> Xếp hạng
              </NavLink>
            </nav>

            <div className="header-actions">
              {profile ? (
                <div className="account-menu">
                  <button
                    type="button"
                    className="button account-toggle"
                    aria-expanded={accountOpen}
                    aria-controls="account-options"
                    onClick={() => setAccountAt(accountOpen ? undefined : location.key)}
                  >
                    <Avatar path={profile.anhDaiDienPath} name={profile.hoTen} />
                    <span>{profile.hoTen}</span>
                    <ChevronDown size={16} />
                  </button>

                  {accountOpen && (
                    <div id="account-options" className="account-options">
                      <Link to="/tai-khoan">
                        <BookOpen size={17} /> Hồ sơ và hộ chiếu
                      </Link>
                      {!partner && (
                        <Link to="/dang-ky-doi-tac">
                          <Building2 size={17} /> Đăng ký đối tác
                        </Link>
                      )}
                      {partner && (
                        <Link to="/doi-tac">
                          <Building2 size={17} /> Khu vực đối tác
                        </Link>
                      )}
                      {reviewer && (
                        <Link to="/kiem-duyet">
                          <Landmark size={17} /> Ban văn hóa
                        </Link>
                      )}
                      {admin && (
                        <Link to="/quan-tri">
                          <ShieldCheck size={17} /> Quản trị hệ thống
                        </Link>
                      )}
                      <button
                        type="button"
                        onClick={() => {
                          setAccountAt(undefined)
                          void logout()
                        }}
                      >
                        <LogOut size={17} /> Đăng xuất
                      </button>
                    </div>
                  )}
                </div>
              ) : (
                <>
                  <Link to="/dang-nhap" className="button quiet">Đăng nhập</Link>
                  <Link to="/dang-ky" className="button primary">
                    Đăng ký <ArrowRight size={15} />
                  </Link>
                </>
              )}
            </div>
          </div>
        </div>
      </header>

      {profile && !workspace && (
        <div className="member-bar">
          <nav aria-label="Điều hướng thành viên">
            <NavLink to="/yeu-thich"><Bookmark size={16} /> Đã lưu</NavLink>
            <NavLink to="/check-in"><Camera size={16} /> Check-in</NavLink>
            <NavLink to="/hanh-trinh"><Map size={16} /> Hành trình</NavLink>
            <NavLink to="/so-tay"><BookOpen size={16} /> Sổ tay</NavLink>
            <NavLink to="/dong-gop"><PencilLine size={16} /> Đóng góp</NavLink>
            <NavLink to="/yeu-cau-tham-gia">
              <Ticket size={16} /> Vé trải nghiệm
            </NavLink>
          </nav>
        </div>
      )}

      <main id="noi-dung" className={mainClassName}>
        <Outlet />
      </main>

      <FloatingAssistant
        key={profile?.nguoiDungId ?? 'guest'}
        authenticated={!!profile}
      />

      <footer className="site-footer">
        <div className="footer-inner">
          <Brand />
          <p className="footer-heritage-note">
            <img
              className="vietnam-flag"
              src="/images/theme/co-viet-nam.svg"
              alt="Quốc kỳ Việt Nam"
              width={30}
              height={20}
            />
            <span>Khám phá, ghi chép và góp thêm tư liệu về di sản Việt Nam.</span>
          </p>
          <nav aria-label="Liên kết cuối trang">
            <Link to="/kham-pha">Khám phá</Link>
            <Link to="/dong-gop">Đóng góp</Link>
            <a href="/images/nguon-anh.html">Nguồn hình ảnh</a>
          </nav>
        </div>
      </footer>
    </>
  )
}

function Protected({ children, roles }: { children: ReactNode; roles?: Role[] }) {
  const { profile, loading } = useSession()
  const location = useLocation()

  if (loading) return <LoadState loading />

  if (!profile) {
    return (
      <Navigate
        to="/dang-nhap"
        state={{ from: location.pathname + location.search }}
        replace
      />
    )
  }

  if (roles && !roles.some(role => profile.vaiTro.includes(role))) {
    return (
      <>
        <PageTitle eyebrow="QUYỀN TRUY CẬP" title="Bạn chưa có quyền vào khu vực này" />
        <Link to="/">Về trang chủ</Link>
      </>
    )
  }

  return children
}

export default function App() {
  return (
    <BrowserRouter>
      <SessionProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<HomePage />} />
            <Route path="kham-pha" element={<ExplorePage />} />
            <Route path="di-san/:slug" element={<HeritagePage />} />
            <Route path="trai-nghiem" element={<ExperiencesPage />} />
            <Route path="trai-nghiem/:slug" element={<ExperiencePage />} />
            <Route path="nhiem-vu" element={<MissionsPage />} />
            <Route path="so-tay/chia-se/:code" element={<SharedJournalPage />} />
            <Route path="dang-nhap" element={<LoginPage key="login" />} />
            <Route path="dang-ky" element={<LoginPage key="register" />} />
            <Route path="bang-xep-hang" element={<LeaderboardPage />} />

            <Route
              path="dang-ky-doi-tac"
              element={<Protected><PartnerApplicationPage /></Protected>}
            />
            <Route path="tai-khoan" element={<Protected><ProfilePage /></Protected>} />
            <Route path="yeu-thich" element={<Protected><FavoritesPage /></Protected>} />
            <Route path="check-in" element={<Protected><CheckInPage /></Protected>} />
            <Route path="so-tay" element={<Protected><JournalsPage /></Protected>} />
            <Route path="hanh-trinh" element={<Protected><TripsPage /></Protected>} />
            <Route path="tro-ly" element={<Protected><AssistantPage /></Protected>} />
            <Route path="dong-gop" element={<Protected><ContributionsPage /></Protected>} />
            <Route
              path="yeu-cau-tham-gia"
              element={<Protected><BookingsPage /></Protected>}
            />

            <Route
              path="kiem-duyet"
              element={
                <Protected roles={['KIEM_DUYET_VAN_HOA', 'QUAN_TRI']}>
                  <WorkspaceLayout area="culture" />
                </Protected>
              }
            >
              <Route index element={<ModerationPage />} />
              <Route path="di-san" element={<CultureCatalog />} />
            </Route>

            <Route
              path="quan-tri"
              element={
                <Protected roles={['QUAN_TRI']}>
                  <WorkspaceLayout area="admin" />
                </Protected>
              }
            >
              <Route index element={<AdminPage />} />
              <Route path="noi-dung" element={<AdminContent />} />
              <Route path="doi-tac" element={<PartnerApprovalPage />} />
            </Route>

            <Route
              path="doi-tac"
              element={
                <Protected roles={['DOI_TAC', 'QUAN_TRI']}>
                  <WorkspaceLayout area="partner" />
                </Protected>
              }
            >
              <Route index element={<PartnerPage />} />
            </Route>

            <Route
              path="*"
              element={
                <Empty title="Không tìm thấy trang.">
                  <Link to="/">Về trang chủ</Link>
                </Empty>
              }
            />
          </Route>
        </Routes>
      </SessionProvider>
    </BrowserRouter>
  )
}