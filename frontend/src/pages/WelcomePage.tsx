import { useMutation } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import {
  ArrowDown,
  ArrowRight,
  AudioLines,
  BookOpen,
  Film,
  Fingerprint,
  Headphones,
  Sparkles,
  WandSparkles,
} from 'lucide-react'
import { motion } from 'motion/react'
import { sessionApi } from '../api'
import { DEMO_MODE, sessionStore } from '../lib/session'
import { Brand } from '../components/layout/Brand'
import { Orb } from '../components/layout/Orb'
import { Button } from '../components/ui/button'
import { ErrorState } from '../components/ui/states'
export default function WelcomePage() {
  const navigate = useNavigate()
  const start = useMutation({
    mutationFn: async () => {
      const existing = sessionStore.get()
      return existing ?? (await sessionApi.create())
    },
    onSuccess: (session) => {
      const existed = !!sessionStore.get()
      sessionStore.set(session)
      navigate(existed ? '/dashboard' : '/onboarding')
    },
  })
  return (
    <div className="welcome">
      <header className="welcome-header">
        <Brand />
        <nav>
          <a href="#possibilities">Возможности</a>
          <Link to="/physics">Физика звука</Link>
        </nav>
        <Button variant="secondary" onClick={() => start.mutate()} disabled={start.isPending}>
          Войти в свой ритм <ArrowRight size={15} />
        </Button>
      </header>
      {DEMO_MODE && (
        <div className="demo-banner">Демо-режим · тестовые данные без обращения к AI</div>
      )}
      <main>
        <section className="welcome-hero">
          <motion.div
            className="welcome-copy"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
          >
            <span className="pill">
              <span className="status-dot" /> ЧУВСТВОВАТЬ. ОТКРЫВАТЬ. БЫТЬ СОБОЙ.
            </span>
            <h1>
              Твой мир.
              <br />
              Твоё настроение.
              <br />
              <span className="gradient-text">Твой контекст.</span>
            </h1>
            <p>
              Иногда лучший плейлист — тот, который понимает тебя. Находи музыку, фильмы, книги и
              занятия под своё настроение.
            </p>
            <Button
              className="welcome-cta"
              onClick={() => start.mutate()}
              disabled={start.isPending}
            >
              <Sparkles size={19} />
              {start.isPending ? 'Создаём твоё пространство…' : 'Начать'}
              <ArrowRight size={19} />
            </Button>
            <span className="welcome-meta">
              <Fingerprint size={14} /> Без регистрации. С вниманием к тебе.
            </span>
            {start.isError && <ErrorState error={start.error} />}
          </motion.div>
          <div className="welcome-visual">
            <Orb />
            <div className="floating-label floating-one">
              <span>😌</span>
              <div>
                <small>У каждого момента</small>
                <strong>есть свой вайб</strong>
              </div>
            </div>
            <div className="floating-label floating-two">
              <AudioLines size={23} />
              <div>
                <small>Текст, эмоции и звук</small>
                <strong>Собираем твой контекст</strong>
              </div>
              <span className="status-dot" />
            </div>
            <span className="visual-caption">
              Визуальная метафора настроения · не данные анализа
            </span>
          </div>
        </section>
        <div className="welcome-divider">
          <span>МАЛЕНЬКИЙ ЧЕК-ИН. БОЛЬШЕ ОТКРЫТИЙ.</span>
          <ArrowDown size={17} />
        </div>
        <section id="possibilities" className="feature-grid">
          {[
            {
              icon: Headphones,
              title: 'Попасть в настроение',
              text: 'Музыка, которая откликается именно сейчас.',
              color: 'purple',
            },
            {
              icon: Film,
              title: 'Увидеть новую историю',
              text: 'Кино и книги в контексте твоих интересов.',
              color: 'blue',
            },
            {
              icon: WandSparkles,
              title: 'Найти свой момент',
              text: 'От прогулки до творчества — выбери свой ритм.',
              color: 'green',
            },
            {
              icon: BookOpen,
              title: 'Понять, почему',
              text: 'Объяснения подбора и физика твоего голоса.',
              color: 'peach',
            },
          ].map(({ icon: Icon, title, text, color }) => (
            <article key={title} className="feature-card">
              <span className={`feature-icon ${color}`}>
                <Icon size={22} />
              </span>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </section>
      </main>
      <footer className="welcome-footer">
        <Brand />
        <span>Немного науки. Немного магии. Много тебя.</span>
        <span>© 2026 ContextIQ</span>
      </footer>
    </div>
  )
}
