import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

interface TicketType {
  id: number
  name: string
  price: number
  capacity: number
}

interface Event {
  id: number
  organizerId: number
  organizerName: string | null
  title: string
  description: string | null
  startAt: string
  endAt: string
  location: string | null
  isOnline: boolean
  isPublic: boolean
  status: string
  createdAt: string
  categoryIds: number[]
  ticketTypes: TicketType[]
}

interface ParticipantProfile {
  firstName: string
  lastName: string
  middleName: string | null
  email: string
  phone: string | null
}

interface Registration {
  id: number
  firstName: string
  lastName: string
  middleName: string | null
  email: string
  phone: string | null
  status: string
  checkedInAt: string | null
}

function formatDate(s: string) {
  const d = new Date(s)
  return d.toLocaleString('ru-RU', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function EventPage() {
  const { id } = useParams<{ id: string }>()
  const { isAuthenticated } = useAuth()
  const [event, setEvent] = useState<Event | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [me, setMe] = useState<{ id: string } | null>(null)
  const [participants, setParticipants] = useState<Registration[]>([])
  const [participantsLoading, setParticipantsLoading] = useState(false)
  const [actionId, setActionId] = useState<number | null>(null)
  const [confirmCancelId, setConfirmCancelId] = useState<number | null>(null)

  const [ticketTypeId, setTicketTypeId] = useState<number | null>(null)
  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [middleName, setMiddleName] = useState('')
  const [phone, setPhone] = useState('')
  const [paymentCompleted, setPaymentCompleted] = useState(false)
  const [regLoading, setRegLoading] = useState(false)
  const [regError, setRegError] = useState('')
  const [regSuccess, setRegSuccess] = useState(false)

  useEffect(() => {
    if (!id) return
    apiFetch(`/events/${id}`)
      .then((r) => {
        if (!r.ok) throw new Error('Не найдено')
        return r.json()
      })
      .then(setEvent)
      .catch(() => setError('Событие не найдено'))
      .finally(() => setLoading(false))
  }, [id])

  const loadProfile = useCallback(() => {
    if (!isAuthenticated) return
    apiFetch('/me/profile')
      .then((r) => {
        if (!r.ok) return
        return r.json()
      })
      .then((p: ParticipantProfile | undefined) => {
        if (p) {
          setEmail(p.email)
          setFirstName(p.firstName)
          setLastName(p.lastName)
          setMiddleName(p.middleName ?? '')
          setPhone(p.phone ?? '')
        }
      })
      .catch(() => {})
  }, [isAuthenticated])

  useEffect(() => {
    loadProfile()
  }, [loadProfile])

  useEffect(() => {
    if (isAuthenticated) {
      apiFetch('/me')
        .then((r) => (r.ok ? r.json() : null))
        .then(setMe)
        .catch(() => setMe(null))
    } else {
      setMe(null)
    }
  }, [isAuthenticated])

  const loadParticipants = useCallback(() => {
    if (!id || !me) return
    setParticipantsLoading(true)
    apiFetch(`/events/${id}/registrations`)
      .then((r) => {
        if (!r.ok) throw new Error('Forbidden')
        return r.json()
      })
      .then(setParticipants)
      .catch(() => setParticipants([]))
      .finally(() => setParticipantsLoading(false))
  }, [id, me])

  useEffect(() => {
    if (event && me && Number(event.organizerId) === Number(me.id)) {
      loadParticipants()
    } else {
      setParticipants([])
    }
  }, [event, me, loadParticipants])

  async function handleCheckIn(regId: number) {
    setActionId(regId)
    try {
      const res = await apiFetch(`/registrations/${regId}/check-in`, { method: 'PATCH' })
      if (res.ok) loadParticipants()
    } finally {
      setActionId(null)
    }
  }

  async function handleCancelRegistration(regId: number) {
    setActionId(regId)
    setConfirmCancelId(null)
    try {
      const res = await apiFetch(`/registrations/${regId}`, { method: 'DELETE' })
      if (res.ok) loadParticipants()
    } finally {
      setActionId(null)
    }
  }

  function handleExport(format: 'xlsx' | 'pdf') {
    if (!event) return
    apiFetch(`/events/${event.id}/registrations/export?format=${format}`)
      .then((r) => r.blob())
      .then((blob) => {
        const url = URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = `${event.title}.${format === 'xlsx' ? 'xlsx' : 'pdf'}`
        a.click()
        URL.revokeObjectURL(url)
      })
  }

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault()
    if (!event || !ticketTypeId) return
    const tt = event.ticketTypes.find((t) => t.id === ticketTypeId)
    if (!tt) return
    if (!email.trim() || !firstName.trim() || !lastName.trim()) {
      setRegError('Заполните обязательные поля: email, имя, фамилия')
      return
    }
    if (tt.price > 0 && !paymentCompleted) {
      setRegError('Для платных билетов необходимо подтвердить оплату')
      return
    }
    setRegError('')
    setRegLoading(true)
    try {
      const res = await apiFetch(`/events/${event.id}/registrations`, {
        method: 'POST',
        body: JSON.stringify({
          ticketTypeId,
          email: email.trim().toLowerCase(),
          firstName: firstName.trim() || undefined,
          lastName: lastName.trim() || undefined,
          middleName: middleName.trim() || undefined,
          phone: phone.trim() || undefined,
          paymentCompleted: tt.price > 0 ? paymentCompleted : undefined,
        }),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        setRegError(data.error || `Ошибка ${res.status}`)
        return
      }
      setRegSuccess(true)
    } catch {
      setRegError('Ошибка сети')
    } finally {
      setRegLoading(false)
    }
  }

  if (loading) return <p style={{ padding: '2rem' }}>Загрузка...</p>
  if (error || !event) return <p style={{ padding: '2rem', color: 'red' }}>{error || 'Событие не найдено'}</p>

  const canRegister = event.status === 'active' && event.ticketTypes.length > 0
  const isOrganizer = me && Number(event.organizerId) === Number(me.id)

  return (
    <div style={{ maxWidth: 700, margin: '2rem auto', padding: '1rem' }}>
      <p style={{ marginBottom: '1rem' }}>
        <Link to="/events">← Каталог</Link>
      </p>

      <h1>{event.title}</h1>

      {event.status === 'cancelled' && (
        <p style={{ color: 'red', fontWeight: 'bold' }}>Событие отменено</p>
      )}

      <p style={{ color: '#666', marginBottom: '0.5rem' }}>
        {formatDate(event.startAt)} — {formatDate(event.endAt)}
      </p>
      <p style={{ marginBottom: '0.5rem' }}>
        {event.location || (event.isOnline ? 'Онлайн' : 'Место не указано')}
      </p>
      {event.organizerName && (
        <p style={{ marginBottom: '1rem' }}>
          Организатор: <Link to={`/organizers/${event.organizerId}`}>{event.organizerName}</Link>
        </p>
      )}

      {event.description && (
        <div style={{ marginBottom: '1.5rem', whiteSpace: 'pre-wrap' }}>{event.description}</div>
      )}

      {event.ticketTypes.length > 0 && (
        <div style={{ marginTop: '1.5rem' }}>
          <h3>Типы билетов</h3>
          <ul style={{ listStyle: 'none', padding: 0 }}>
            {event.ticketTypes.map((tt) => (
              <li
                key={tt.id}
                style={{
                  border: '1px solid #ddd',
                  borderRadius: 6,
                  padding: '0.75rem',
                  marginBottom: '0.5rem',
                }}
              >
                <strong>{tt.name}</strong> — {tt.price === 0 ? 'Бесплатно' : `${tt.price} ₽`} (мест: {tt.capacity})
              </li>
            ))}
          </ul>
        </div>
      )}

      {regSuccess ? (
        <div
          style={{
            marginTop: '2rem',
            padding: '1.5rem',
            background: '#e8f5e9',
            borderRadius: 8,
            border: '1px solid #a5d6a7',
          }}
        >
          <h3 style={{ margin: '0 0 0.5rem 0', color: '#2e7d32' }}>Регистрация успешна!</h3>
          <p style={{ margin: 0 }}>
            На указанный email будет отправлено письмо с подтверждением регистрации.
          </p>
        </div>
      ) : (
        canRegister && (
          <div style={{ marginTop: '2rem', padding: '1.5rem', border: '1px solid #ddd', borderRadius: 8 }}>
            <h3>Зарегистрироваться</h3>
            <form onSubmit={handleRegister}>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                  Тип билета
                </label>
                <select
                  value={ticketTypeId ?? ''}
                  onChange={(e) => {
                    const v = e.target.value
                    setTicketTypeId(v ? Number(v) : null)
                    setPaymentCompleted(false)
                  }}
                  required
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                >
                  <option value="">— Выберите —</option>
                  {event.ticketTypes.map((tt) => (
                    <option key={tt.id} value={tt.id}>
                      {tt.name} — {tt.price === 0 ? 'Бесплатно' : `${tt.price} ₽`}
                    </option>
                  ))}
                </select>
              </div>

              {ticketTypeId && (event.ticketTypes.find((t) => t.id === ticketTypeId)?.price ?? 0) > 0 && (
                <div
                  style={{
                    marginBottom: '1rem',
                    padding: '1rem',
                    background: '#fff3e0',
                    borderRadius: 6,
                    border: '1px solid #ffcc80',
                  }}
                >
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={paymentCompleted}
                      onChange={(e) => setPaymentCompleted(e.target.checked)}
                    />
                    <span>Оплата произведена (заглушка)</span>
                  </label>
                  <p style={{ margin: '0.5rem 0 0 0', fontSize: '0.9rem', color: '#666' }}>
                    В реальном приложении здесь будет переход к оплате.
                  </p>
                </div>
              )}

              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                  Email <span style={{ color: 'red' }}>*</span>
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                />
              </div>
              <div style={{ display: 'flex', gap: '1rem', marginBottom: '1rem' }}>
                <div style={{ flex: 1 }}>
                  <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                    Имя <span style={{ color: 'red' }}>*</span>
                  </label>
                  <input
                    type="text"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    required
                    autoComplete="given-name"
                    style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                  />
                </div>
                <div style={{ flex: 1 }}>
                  <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                    Фамилия <span style={{ color: 'red' }}>*</span>
                  </label>
                  <input
                    type="text"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    required
                    autoComplete="family-name"
                    style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                  />
                </div>
              </div>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                  Отчество
                </label>
                <input
                  type="text"
                  value={middleName}
                  onChange={(e) => setMiddleName(e.target.value)}
                  autoComplete="additional-name"
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                />
              </div>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 500 }}>
                  Телефон
                </label>
                <input
                  type="tel"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  autoComplete="tel"
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                />
              </div>
              {regError && <p style={{ color: 'red', marginBottom: '1rem' }}>{regError}</p>}
              <button type="submit" disabled={regLoading} style={{ padding: '0.5rem 1rem' }}>
                {regLoading ? 'Отправка...' : 'Зарегистрироваться'}
              </button>
            </form>
          </div>
        )
      )}

      {isOrganizer && (
        <div style={{ marginTop: '2rem', padding: '1.5rem', border: '1px solid #ddd', borderRadius: 8 }}>
          <h3>Участники</h3>
          <div style={{ marginBottom: '1rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <button
              type="button"
              onClick={() => handleExport('xlsx')}
              style={{ padding: '0.35rem 0.75rem', fontSize: '0.9rem' }}
            >
              Экспорт Excel
            </button>
            <button
              type="button"
              onClick={() => handleExport('pdf')}
              style={{ padding: '0.35rem 0.75rem', fontSize: '0.9rem' }}
            >
              Экспорт PDF
            </button>
          </div>
          {participantsLoading ? (
            <p>Загрузка участников...</p>
          ) : participants.length === 0 ? (
            <p style={{ color: '#666' }}>Пока нет зарегистрированных участников.</p>
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
                    <th style={{ padding: '0.5rem' }}>ФИО</th>
                    <th style={{ padding: '0.5rem' }}>Email</th>
                    <th style={{ padding: '0.5rem' }}>Телефон</th>
                    <th style={{ padding: '0.5rem' }}>Чек-ин</th>
                    <th style={{ padding: '0.5rem' }}></th>
                  </tr>
                </thead>
                <tbody>
                  {participants.map((reg) => {
                    const fullName = [reg.lastName, reg.firstName, reg.middleName].filter(Boolean).join(' ')
                    return (
                      <tr key={reg.id} style={{ borderBottom: '1px solid #eee' }}>
                        <td style={{ padding: '0.5rem' }}>{fullName}</td>
                        <td style={{ padding: '0.5rem' }}>{reg.email}</td>
                        <td style={{ padding: '0.5rem' }}>{reg.phone ?? '—'}</td>
                        <td style={{ padding: '0.5rem' }}>
                          {reg.status === 'checked_in' ? (
                            <span style={{ color: '#2e7d32' }}>✓ Да</span>
                          ) : (
                            <span style={{ color: '#666' }}>Нет</span>
                          )}
                        </td>
                        <td style={{ padding: '0.5rem' }}>
                          {confirmCancelId === reg.id ? (
                              <span style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}>
                                <button
                                  type="button"
                                  onClick={() => handleCancelRegistration(reg.id)}
                                  disabled={actionId !== null}
                                  style={{ padding: '0.2rem 0.4rem', fontSize: '0.8rem', color: '#c62828' }}
                                >
                                  Да
                                </button>
                                <button
                                  type="button"
                                  onClick={() => setConfirmCancelId(null)}
                                  style={{ padding: '0.2rem 0.4rem', fontSize: '0.8rem' }}
                                >
                                  Нет
                                </button>
                              </span>
                            ) : (
                              <>
                                {reg.status !== 'checked_in' && (
                                  <button
                                    type="button"
                                    onClick={() => handleCheckIn(reg.id)}
                                    disabled={actionId !== null}
                                    style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem', marginRight: '0.25rem' }}
                                  >
                                    {actionId === reg.id ? '...' : 'Чек-ин'}
                                  </button>
                                )}
                                <button
                                  type="button"
                                  onClick={() => setConfirmCancelId(reg.id)}
                                  disabled={actionId !== null}
                                  style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem', color: '#c62828' }}
                                >
                                  Отменить
                                </button>
                              </>
                            )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
