import { CircleAlert, Sparkles } from 'lucide-react'
import { Button } from './button'
export function ErrorState({ error, retry }: { error: unknown; retry?: () => void }) {
  return (
    <div className="error-state" role="alert">
      <CircleAlert size={20} />
      <div>
        <strong>Что-то пошло не так</strong>
        <p>{error instanceof Error ? error.message : 'Не удалось загрузить данные.'}</p>
        {retry && (
          <Button variant="secondary" size="sm" onClick={retry}>
            Попробовать снова
          </Button>
        )}
      </div>
    </div>
  )
}
export function EmptyState({
  title,
  text,
  children,
}: {
  title: string
  text: string
  children?: React.ReactNode
}) {
  return (
    <div className="empty-state">
      <div className="empty-icon">
        <Sparkles />
      </div>
      <h3>{title}</h3>
      <p>{text}</p>
      {children}
    </div>
  )
}
export function Skeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="skeleton-grid" role="status" aria-label="Загрузка">
      {Array.from({ length: count }, (_, i) => (
        <div className="skeleton" key={i} />
      ))}
    </div>
  )
}
