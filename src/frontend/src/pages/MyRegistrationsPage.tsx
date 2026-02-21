import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

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
    <div style={{ maxWidth: 700, margin: '2rem auto', padding: '1rem' }}>
      <p style={{ marginBottom: '1rem' }}>
        <Link to="/">← Главная</Link>
      </p>

      <h1>Мои регистрации</h1>

      {loading && <p style={{ padding: '1rem 0' }}>Загрузка...</p>}
      {error && <p style={{ color: 'red', marginBottom: '1rem' }}>{error}</p>}

      {!loading && !error && registrations.length === 0 && (
        <p style={{ color: '#666', marginTop: '1rem' }}>
          У вас пока нет регистраций на мероприятия.{' '}
          <Link to="/events">Перейти в каталог</Link>
        </p>
      )}

      {!loading && registrations.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0, marginTop: '1rem' }}>
          {registrations.map((reg) => (
            <li
              key={reg.id}
              style={{
                border: '1px solid #ddd',
                borderRadius: 8,
                padding: '1rem',
                marginBottom: '0.75rem',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem' }}>
                <div>
                  <Link to={`/events/${reg.eventId}`} style={{ fontWeight: 600, fontSize: '1.1rem' }}>
                    {reg.eventTitle}
                  </Link>
                  <p style={{ margin: '0.25rem 0 0 0', color: '#666', fontSize: '0.9rem' }}>
                    {formatDate(reg.startAt)} · {reg.ticketTypeName}
                  </p>
                  {reg.status === 'checked_in' && (
                    <span style={{ display: 'inline-block', marginTop: '0.5rem', fontSize: '0.85rem', color: '#2e7d32' }}>
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
                        style={{ padding: '0.25rem 0.5rem', fontSize: '0.85rem', color: '#c62828' }}
                      >
                        Да
                      </button>
                      <button
                        type="button"
                        onClick={() => setConfirmCancelId(null)}
                        style={{ padding: '0.25rem 0.5rem', fontSize: '0.85rem' }}
                      >
                        Нет
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setConfirmCancelId(reg.id)}
                      disabled={cancellingId !== null}
                      style={{ padding: '0.35rem 0.75rem', fontSize: '0.9rem' }}
                    >
                      {cancellingId === reg.id ? 'Отмена...' : 'Отменить регистрацию'}
                    </button>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
