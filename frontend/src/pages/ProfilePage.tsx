import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Fingerprint, ShieldCheck, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { historyApi, profileApi } from '../api'
import { sessionStore } from '../lib/session'
import { dateLabel } from '../lib/utils'
import { PreferenceForm } from '../components/mood/PreferenceForm'
import { Button } from '../components/ui/button'
import { ConfirmDialog } from '../components/ui/dialog'
import { ErrorState, Skeleton } from '../components/ui/states'
export default function ProfilePage() {
  const profile = useQuery({
    queryKey: ['profile'],
    queryFn: ({ signal }) => profileApi.get(signal),
  })
  const history = useQuery({
    queryKey: ['history-count'],
    queryFn: ({ signal }) => historyApi.page(1, 1, signal),
  })
  const client = useQueryClient()
  const navigate = useNavigate()
  const remove = useMutation({
    mutationFn: () => profileApi.delete(),
    onSuccess: () => {
      sessionStore.clear()
      client.clear()
      toast.success('Данные удалены')
      navigate('/')
    },
  })
  return (
    <>
      <div className="page-heading">
        <span className="eyebrow">ТО, ЧТО ДЕЛАЕТ ТЕБЯ ТОБОЙ</span>
        <h1>
          Твой <span className="gradient-text">контекст.</span>
        </h1>
        <p>Настрой пространство под себя. Вкусы могут меняться — это нормально.</p>
      </div>
      <section className="panel profile-banner">
        <span className="profile-avatar">
          <Fingerprint size={35} />
        </span>
        <div>
          <h2>Без имени. С характером.</h2>
          <p>
            Гостевой профиль ·{' '}
            {history.isError ? 'счётчик недоступен' : `${history.data?.total ?? '…'} чек-ин`}
          </p>
        </div>
        <span className="pill">
          <ShieldCheck size={14} /> Только твоё пространство
        </span>
      </section>
      {profile.isPending ? (
        <Skeleton count={4} />
      ) : profile.isError ? (
        <ErrorState
          error={profile.error}
          retry={() => {
            void profile.refetch()
          }}
        />
      ) : (
        <>
          <PreferenceForm key={profile.data.userId} profile={profile.data} />
          <p className="field-hint">
            Последнее изменение: {dateLabel(profile.data.updatedAt)}. История изменений предпочтений
            в API не предусмотрена.
          </p>
        </>
      )}
      <section className="panel privacy-panel">
        <ShieldCheck size={24} />
        <div>
          <h3>Твои данные — твой выбор</h3>
          <p>
            Backend сохраняет отправленные текст и эмодзи, результаты анализа, рекомендации, оценки
            и предпочтения. Черновики не отправляются. Аудио передаётся для анализа только по
            отдельному нажатию; по текущему коду сервисов запись в базу не сохраняется.
          </p>
          <p>
            Гостевой токен хранится в этом браузере до окончания сессии. Очистка данных браузера
            приведёт к потере доступа к профилю. Не передавай токен другим.
          </p>
        </div>
      </section>
      <section className="danger-zone">
        <div>
          <h3>Удалить пользовательские данные</h3>
          <p>
            Профиль, история, рекомендации, оценки и сессии будут удалены без возможности
            восстановления.
          </p>
        </div>
        <ConfirmDialog
          title="Удалить своё пространство?"
          trigger={
            <Button variant="destructive">
              <Trash2 size={16} /> Удалить данные
            </Button>
          }
          action={
            <Button
              variant="destructive"
              disabled={remove.isPending}
              onClick={() => remove.mutate()}
            >
              {remove.isPending ? 'Удаляем…' : 'Удалить навсегда'}
            </Button>
          }
        >
          Это действие удалит твои данные на сервере. Вернуть их не получится.
          {remove.isError && <ErrorState error={remove.error} />}
        </ConfirmDialog>
      </section>
    </>
  )
}
