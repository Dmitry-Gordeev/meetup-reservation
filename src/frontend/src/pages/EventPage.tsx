import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch } from '../api/client'

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
  const [event, setEvent] = useState<Event | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

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

  if (loading) return <p style={{ padding: '2rem' }}>Загрузка...</p>
  if (error || !event) return <p style={{ padding: '2rem', color: 'red' }}>{error || 'Событие не найдено'}</p>

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
    </div>
  )
}
