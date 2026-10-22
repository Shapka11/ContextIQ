import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, Link } from 'react-router-dom'
import {
  Activity,
  ArrowUpRight,
  AudioLines,
  Fingerprint,
  History,
  Moon,
  ScanLine,
  Sparkles,
  Sun,
  UserRound,
} from 'lucide-react'
import { motion } from 'motion/react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Brand } from './Brand'
import { healthApi } from '../../api'
import { DEMO_MODE } from '../../lib/session'
import { useSession } from '../../hooks/useSession'
const links = [
  { to: '/dashboard', label: 'Моё настроение', short: 'Сканер', icon: ScanLine },
  { to: '/results', label: 'Мой вайб', short: 'Мой вайб', icon: Sparkles },
  { to: '/history', label: 'История', short: 'История', icon: History },
  { to: '/physics', label: 'Физика звука', short: 'Звук', icon: AudioLines },
  { to: '/profile', label: 'Мой профиль', short: 'Профиль', icon: UserRound },
]
export function AppLayout() {
  const location = useLocation()
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' })
  }, [location.pathname])
  const token = useSession()
  const client = useQueryClient()
  const [light, setLight] = useState(() => localStorage.getItem('contextiq.theme') === 'light')
  useEffect(() => {
    document.documentElement.dataset.theme = light ? 'light' : 'dark'
    localStorage.setItem('contextiq.theme', light ? 'light' : 'dark')
  }, [light])
  useEffect(() => {
    if (!token) client.clear()
  }, [token, client])
  const health = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => healthApi.get(signal),
    retry: false,
    refetchInterval: 60_000,
  })
  return (
    <div className="app-shell">
      <a href="#main" className="skip-link">
        К содержимому
      </a>
      <aside className="sidebar">
        <Brand />
        <div className="sidebar-caption">ТВОЁ ПРОСТРАНСТВО</div>
        <nav aria-label="Основная навигация">
          {links.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
            >
              <Icon size={20} />
              {label}
              {to === '/dashboard' && <span className="nav-spark">✦</span>}
            </NavLink>
          ))}
        </nav>
        <Link to="/physics" className="sidebar-note">
          <span className="tiny-badge">
            <Activity size={13} /> НАУКА ВНУТРИ
          </span>
          <h4>
            За голосом —<br />
            целая физика
          </h4>
          <p>Узнай, как звук становится сигналом.</p>
          <ArrowUpRight size={18} />
        </Link>
        <div className="sidebar-footer">
          <span className="avatar">
            <Fingerprint size={20} />
          </span>
          <div>
            <strong>Твоё пространство</strong>
            <small>Гостевая сессия</small>
          </div>
          <span className="status-dot" />
        </div>
      </aside>
      <div className="app-body">
        <header className="topbar">
          <div className="breadcrumb">
            Твоё пространство <span>/</span>{' '}
            <strong>{links.find((l) => l.to === location.pathname)?.label ?? 'ContextIQ'}</strong>
          </div>
          <Brand />
          <div className="topbar-right">
            <span className={`connection ${health.isError ? 'offline' : ''}`}>
              <span className="status-dot" />
              {DEMO_MODE
                ? 'Демо-режим'
                : health.isPending
                  ? 'Подключаемся'
                  : health.isError
                    ? 'API недоступен'
                    : 'Сервисы подключены'}
            </span>
            <button
              className="icon-button"
              onClick={() => setLight(!light)}
              aria-label={light ? 'Тёмная тема' : 'Светлая тема'}
            >
              {light ? <Moon size={19} /> : <Sun size={19} />}
            </button>
            <Link to="/profile" className="avatar small" aria-label="Мой профиль">
              Г
            </Link>
          </div>
        </header>
        {DEMO_MODE && (
          <div className="demo-banner">
            Демо-режим · фиксированные тестовые сценарии, без обращений к AI. Данные сбрасываются
            при перезагрузке.
          </div>
        )}
        {!token && (
          <div className="demo-banner">
            Сессия недоступна или истекла. <Link to="/">Начать новую сессию →</Link>
          </div>
        )}
        <main id="main" className="main-content">
          <motion.div
            key={location.pathname}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.18 }}
          >
            <Outlet />
          </motion.div>
          <footer className="page-footer">
            <span>
              ContextIQ <span className="muted">· В твоём ритме</span>
            </span>
            <span>
              <Fingerprint size={13} /> Личное остаётся личным
            </span>
          </footer>
        </main>
        <nav className="mobile-nav" aria-label="Мобильная навигация">
          {links.map(({ to, short, icon: Icon }) => (
            <NavLink key={to} to={to}>
              <Icon size={20} />
              <span>{short}</span>
            </NavLink>
          ))}
        </nav>
      </div>
    </div>
  )
}
