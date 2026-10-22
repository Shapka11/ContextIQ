import { useNavigate, Navigate } from 'react-router-dom'
import { ArrowRight } from 'lucide-react'
import { Brand } from '../components/layout/Brand'
import { PreferenceForm } from '../components/mood/PreferenceForm'
import { Button } from '../components/ui/button'
import { useSession } from '../hooks/useSession'
export default function OnboardingPage() {
  const navigate = useNavigate()
  const token = useSession()
  if (!token) return <Navigate to="/" replace />
  return (
    <div className="onboarding">
      <header>
        <Brand />
        <Button variant="ghost" onClick={() => navigate('/dashboard')}>
          Пропустить <ArrowRight size={16} />
        </Button>
      </header>
      <div className="onboarding-heading">
        <span className="eyebrow">ДАВАЙ ПОЗНАКОМИМСЯ</span>
        <h1>
          Немного больше <span className="gradient-text">о тебе.</span>
        </h1>
        <p>
          Твои интересы помогут сделать рекомендации ближе.
          <br />
          Выбери то, что нравится. Здесь нет неправильных ответов.
        </p>
      </div>
      <PreferenceForm onboarding onSaved={() => navigate('/dashboard')} />
    </div>
  )
}
