import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch, getApiUrl } from '../api/client'

interface OrganizerProfile {
  id: number
  name: string
  description: string | null
  hasAvatar?: boolean
}

interface EventItem {
  id: number
  title: string
  startAt: string
  endAt: string
  location: string | null
  isOnline: boolean
}

function formatDate(s: string) {
  const d = new Date(s)
  return d.toLocaleString('ru-RU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function OrganizerPage() {
  const { id } = useParams<{ id: string }>()
  const [profile, setProfile] = useState<OrganizerProfile | null>(null)
  const [events, setEvents] = useState<EventItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return
    Promise.all([
      apiFetch(`/organizers/${id}`).then((r) => (r.ok ? r.json() : null)),
      apiFetch(`/organizers/${id}/events`).then((r) => (r.ok ? r.json() : [])),
    ])
      .then(([p, e]) => {
        setProfile(p)
        setEvents(e)
        if (!p) setError('Организатор не найден')
      })
      .catch(() => setError('Ошибка загрузки'))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) return <p style={{ padding: '2rem' }}>Загрузка...</p>
  if (error || !profile) return <p style={{ padding: '2rem', color: 'red' }}>{error || 'Страница не найдена'}</p>

  return (
    <div style={{ maxWidth: 700, margin: '2rem auto', padding: '1rem' }}>
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1.5rem', marginBottom: '1.5rem' }}>
        {profile.hasAvatar && (
          <img
            src={getApiUrl(`/organizers/${id}/avatar`)}
            alt={profile.name}
            style={{ width: 80, height: 80, borderRadius: '50%', objectFit: 'cover' }}
          />
        )}
        <div>
          <h1 style={{ margin: 0 }}>{profile.name}</h1>
          {profile.description && <p style={{ color: '#666', marginTop: '0.5rem' }}>{profile.description}</p>}
        </div>
      </div>

      <h2>События</h2>
      {events.length === 0 ? (
        <p>Пока нет событий</p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {events.map((evt) => (
            <li
              key={evt.id}
              style={{
                border: '1px solid #ccc',
                borderRadius: 8,
                padding: '1rem',
                marginBottom: '1rem',
              }}
            >
              <Link to={`/events/${evt.id}`} style={{ textDecoration: 'none', color: 'inherit' }}>
                <h3 style={{ margin: '0 0 0.5rem' }}>{evt.title}</h3>
              </Link>
              <p style={{ margin: 0, color: '#666', fontSize: '0.9rem' }}>
                {formatDate(evt.startAt)} — {evt.location || (evt.isOnline ? 'Онлайн' : '—')}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
