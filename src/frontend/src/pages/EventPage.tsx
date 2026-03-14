import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { Button, Card, FormField, PageContainer, StatusMessage, TextInput } from '../components/ui'

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

  if (loading) {
    return (
      <PageContainer size="md">
        <p>Загрузка...</p>
      </PageContainer>
    )
  }
  if (error || !event) {
    return (
      <PageContainer size="md">
        <StatusMessage tone="error" role="alert">
          {error || 'Событие не найдено'}
        </StatusMessage>
      </PageContainer>
    )
  }

  const canRegister = event.status === 'active' && event.ticketTypes.length > 0
  const isOrganizer = me && Number(event.organizerId) === Number(me.id)

  return (
    <PageContainer size="md" className="stack-lg">
      <p>
        <Link to="/events">← Каталог</Link>
      </p>

      <h1>{event.title}</h1>

      {event.status === 'cancelled' && (
        <StatusMessage tone="error" role="alert">
          Событие отменено
        </StatusMessage>
      )}

      <p className="muted" style={{ marginBottom: '0.5rem' }}>
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

      {event.description && <Card><div style={{ whiteSpace: 'pre-wrap' }}>{event.description}</div></Card>}

      {event.ticketTypes.length > 0 && (
        <div>
          <h3>Типы билетов</h3>
          <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
            {event.ticketTypes.map((tt) => (
              <li key={tt.id}>
                <Card>
                  <strong>{tt.name}</strong> — {tt.price === 0 ? 'Бесплатно' : `${tt.price} ₽`} (мест: {tt.capacity})
                </Card>
              </li>
            ))}
          </ul>
        </div>
      )}

      {regSuccess ? (
        <StatusMessage tone="success">
          <h3 style={{ margin: '0 0 0.5rem 0', color: '#177245' }}>Регистрация успешна!</h3>
          <p style={{ margin: 0 }}>
            На указанный email будет отправлено письмо с подтверждением регистрации.
          </p>
        </StatusMessage>
      ) : (
        canRegister && (
          <Card>
            <h3>Зарегистрироваться</h3>
            <form onSubmit={handleRegister} className="stack">
              <FormField label="Тип билета" htmlFor="ticket-type" required>
                <select
                  id="ticket-type"
                  value={ticketTypeId ?? ''}
                  onChange={(e) => {
                    const v = e.target.value
                    setTicketTypeId(v ? Number(v) : null)
                    setPaymentCompleted(false)
                  }}
                  required
                  className="control"
                >
                  <option value="">— Выберите —</option>
                  {event.ticketTypes.map((tt) => (
                    <option key={tt.id} value={tt.id}>
                      {tt.name} — {tt.price === 0 ? 'Бесплатно' : `${tt.price} ₽`}
                    </option>
                  ))}
                </select>
              </FormField>

              {ticketTypeId && (event.ticketTypes.find((t) => t.id === ticketTypeId)?.price ?? 0) > 0 && (
                <div
                  style={{
                    padding: '1rem',
                    background: '#fff7e8',
                    borderRadius: 6,
                    border: '1px solid #f0d8aa',
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
                  <p className="muted" style={{ margin: '0.5rem 0 0 0', fontSize: '0.9rem' }}>
                    В реальном приложении здесь будет переход к оплате.
                  </p>
                </div>
              )}

              <FormField label="Email" htmlFor="reg-email" required>
                <TextInput
                  id="reg-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
              </FormField>
              <div className="form-row">
                <FormField label="Имя" htmlFor="reg-first-name" required>
                  <TextInput
                    id="reg-first-name"
                    type="text"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    required
                    autoComplete="given-name"
                  />
                </FormField>
                <FormField label="Фамилия" htmlFor="reg-last-name" required>
                  <TextInput
                    id="reg-last-name"
                    type="text"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    required
                    autoComplete="family-name"
                  />
                </FormField>
              </div>
              <FormField label="Отчество" htmlFor="reg-middle-name">
                <TextInput
                  id="reg-middle-name"
                  type="text"
                  value={middleName}
                  onChange={(e) => setMiddleName(e.target.value)}
                  autoComplete="additional-name"
                />
              </FormField>
              <FormField label="Телефон" htmlFor="reg-phone">
                <TextInput
                  id="reg-phone"
                  type="tel"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  autoComplete="tel"
                />
              </FormField>
              {regError && (
                <StatusMessage tone="error" role="alert">
                  {regError}
                </StatusMessage>
              )}
              <Button type="submit" variant="primary" disabled={regLoading}>
                {regLoading ? 'Отправка...' : 'Зарегистрироваться'}
              </Button>
            </form>
          </Card>
        )
      )}

      {isOrganizer && (
        <Card>
          <h3>Участники</h3>
          <div style={{ marginBottom: '1rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <Button
              type="button"
              onClick={() => handleExport('xlsx')}
            >
              Экспорт Excel
            </Button>
            <Button
              type="button"
              onClick={() => handleExport('pdf')}
            >
              Экспорт PDF
            </Button>
          </div>
          {participantsLoading ? (
            <p>Загрузка участников...</p>
          ) : participants.length === 0 ? (
            <StatusMessage tone="muted">Пока нет зарегистрированных участников.</StatusMessage>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>ФИО</th>
                    <th>Email</th>
                    <th>Телефон</th>
                    <th>Чек-ин</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {participants.map((reg) => {
                    const fullName = [reg.lastName, reg.firstName, reg.middleName].filter(Boolean).join(' ')
                    return (
                      <tr key={reg.id}>
                        <td>{fullName}</td>
                        <td>{reg.email}</td>
                        <td>{reg.phone ?? '—'}</td>
                        <td>
                          {reg.status === 'checked_in' ? (
                            <span style={{ color: '#177245' }}>✓ Да</span>
                          ) : (
                            <span className="muted">Нет</span>
                          )}
                        </td>
                        <td>
                          {confirmCancelId === reg.id ? (
                              <span style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}>
                                <Button
                                  type="button"
                                  onClick={() => handleCancelRegistration(reg.id)}
                                  disabled={actionId !== null}
                                  variant="danger"
                                  size="sm"
                                >
                                  Да
                                </Button>
                                <Button
                                  type="button"
                                  onClick={() => setConfirmCancelId(null)}
                                  size="sm"
                                >
                                  Нет
                                </Button>
                              </span>
                            ) : (
                              <>
                                {reg.status !== 'checked_in' && (
                                  <Button
                                    type="button"
                                    onClick={() => handleCheckIn(reg.id)}
                                    disabled={actionId !== null}
                                    size="sm"
                                    variant="primary"
                                    style={{ marginRight: '0.25rem' }}
                                  >
                                    {actionId === reg.id ? '...' : 'Чек-ин'}
                                  </Button>
                                )}
                                <Button
                                  type="button"
                                  onClick={() => setConfirmCancelId(reg.id)}
                                  disabled={actionId !== null}
                                  size="sm"
                                  variant="danger"
                                >
                                  Отменить
                                </Button>
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
        </Card>
      )}
    </PageContainer>
  )
}
