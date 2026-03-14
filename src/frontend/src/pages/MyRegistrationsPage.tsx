import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { Button, Card, PageContainer, StatusMessage } from '../components/ui'

interface Registration {
  id: number
  eventId: number
  eventTitle: string
  startAt: string
  ticketTypeName: string
  status: string
}

function formatDate(s: string) {
  const d = new Date(s)
  return d.toLocaleString('ru-RU', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function MyRegistrationsPage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [registrations, setRegistrations] = useState<Registration[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [cancellingId, setCancellingId] = useState<number | null>(null)
  const [confirmCancelId, setConfirmCancelId] = useState<number | null>(null)

  const loadRegistrations = useCallback(() => {
    if (!isAuthenticated) return
    setError('')
    setLoading(true)
    apiFetch('/me/registrations')
      .then((r) => {
        if (!r.ok) throw new Error('Ошибка загрузки')
        return r.json()
      })
      .then(setRegistrations)
      .catch(() => setError('Не удалось загрузить регистрации'))
      .finally(() => setLoading(false))
  }, [isAuthenticated])

  useEffect(() => {
    loadRegistrations()
  }, [loadRegistrations])

  useEffect(() => {
    if (!isAuthenticated) navigate('/login')
  }, [isAuthenticated, navigate])

  async function handleCancel(regId: number) {
    setCancellingId(regId)
    setConfirmCancelId(null)
    try {
      const res = await apiFetch(`/registrations/${regId}`, { method: 'DELETE' })
      if (!res.ok) {
        setError('Не удалось отменить регистрацию')
        return
      }
      loadRegistrations()
    } catch {
      setError('Ошибка сети')
    } finally {
      setCancellingId(null)
    }
  }

  if (!isAuthenticated) return null

  return (
    <PageContainer size="md" className="stack-lg">
      <p>
        <Link to="/">← Главная</Link>
      </p>

      <h1>Мои регистрации</h1>

      {loading && <p>Загрузка...</p>}
      {error && (
        <StatusMessage tone="error" role="alert">
          {error}
        </StatusMessage>
      )}

      {!loading && !error && registrations.length === 0 && (
        <StatusMessage tone="muted">
          У вас пока нет регистраций на мероприятия.{' '}
          <Link to="/events">Перейти в каталог</Link>
        </StatusMessage>
      )}

      {!loading && registrations.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
          {registrations.map((reg) => (
            <li key={reg.id}>
              <Card>
                <div className="cluster" style={{ justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem' }}>
                <div>
                  <Link to={`/events/${reg.eventId}`} style={{ fontWeight: 600, fontSize: '1.1rem' }}>
                    {reg.eventTitle}
                  </Link>
                  <p className="muted" style={{ margin: '0.25rem 0 0 0', fontSize: '0.9rem' }}>
                    {formatDate(reg.startAt)} · {reg.ticketTypeName}
                  </p>
                  {reg.status === 'checked_in' && (
                    <span style={{ display: 'inline-block', marginTop: '0.5rem', fontSize: '0.85rem', color: '#177245' }}>
                      ✓ Отмечен на мероприятии
                    </span>
                  )}
                </div>
                <div>
                  {confirmCancelId === reg.id ? (
                    <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                      <span style={{ fontSize: '0.9rem', color: '#666' }}>Отменить?</span>
                      <button
                        type="button"
                        onClick={() => handleCancel(reg.id)}
                        disabled={cancellingId !== null}
                        className="btn btn--danger btn--sm"
                      >
                        Да
                      </button>
                      <button
                        type="button"
                        onClick={() => setConfirmCancelId(null)}
                        className="btn btn--secondary btn--sm"
                      >
                        Нет
                      </button>
                    </div>
                  ) : (
                    <Button
                      type="button"
                      onClick={() => setConfirmCancelId(reg.id)}
                      disabled={cancellingId !== null}
                      size="sm"
                    >
                      {cancellingId === reg.id ? 'Отмена...' : 'Отменить регистрацию'}
                    </Button>
                  )}
                </div>
              </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </PageContainer>
  )
}
