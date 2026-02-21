import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch, getApiUrl } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

interface Category {
  id: number
  name: string
}

interface TicketTypeInput {
  name: string
  price: number
  capacity: number
}

export default function OrganizerCabinetPage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [categories, setCategories] = useState<Category[]>([])
  const [me, setMe] = useState<{ id: string; roles: string[] } | null>(null)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [startAt, setStartAt] = useState('')
  const [endAt, setEndAt] = useState('')
  const [location, setLocation] = useState('')
  const [isOnline, setIsOnline] = useState(false)
  const [isPublic, setIsPublic] = useState(true)
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<number[]>([])
  const [ticketTypes, setTicketTypes] = useState<TicketTypeInput[]>([{ name: 'Стандарт', price: 0, capacity: 100 }])
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!isAuthenticated) return
    apiFetch('/me')
      .then((r) => (r.ok ? r.json() : null))
      .then(setMe)
      .catch(() => setMe(null))
  }, [isAuthenticated])

  useEffect(() => {
    apiFetch('/categories')
      .then((r) => (r.ok ? r.json() : []))
      .then(setCategories)
      .catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    if (!isAuthenticated) navigate('/login')
  }, [isAuthenticated, navigate])

  const isOrganizer = me?.roles?.includes('organizer') ?? false

  function addTicketType() {
    setTicketTypes((prev) => [...prev, { name: '', price: 0, capacity: 10 }])
  }

  function removeTicketType(index: number) {
    if (ticketTypes.length <= 1) return
    setTicketTypes((prev) => prev.filter((_, i) => i !== index))
  }

  function updateTicketType(index: number, field: keyof TicketTypeInput, value: string | number) {
    setTicketTypes((prev) =>
      prev.map((tt, i) => (i === index ? { ...tt, [field]: value } : tt))
    )
  }

  function toggleCategory(id: number) {
    setSelectedCategoryIds((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id]
    )
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)

    if (!title.trim()) {
      setError('Введите название')
      setLoading(false)
      return
    }
    if (ticketTypes.some((tt) => !tt.name.trim() || tt.capacity <= 0)) {
      setError('Заполните все типы билетов (название, вместимость > 0)')
      setLoading(false)
      return
    }

    const startDate = startAt ? new Date(startAt).toISOString() : new Date().toISOString()
    const endDate = endAt ? new Date(endAt).toISOString() : new Date(Date.now() + 3600000).toISOString()

    try {
      const body = {
        title: title.trim(),
        description: description.trim() || null,
        startAt: startDate,
        endAt: endDate,
        location: location.trim() || null,
        isOnline: isOnline,
        isPublic: isPublic,
        ticketTypes: ticketTypes.map((tt) => ({
          name: tt.name.trim(),
          price: Number(tt.price) || 0,
          capacity: Number(tt.capacity) || 0,
        })),
        categoryIds: selectedCategoryIds.length > 0 ? selectedCategoryIds : null,
      }

      const res = await apiFetch('/events', {
        method: 'POST',
        body: JSON.stringify(body),
      })

      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(data.error || `Ошибка ${res.status}`)
        setLoading(false)
        return
      }

      const eventId = data.id

      if (imageFile && eventId) {
        const formData = new FormData()
        formData.append('image', imageFile)
        const token = localStorage.getItem('meetup_token')
        const imgRes = await fetch(getApiUrl(`/events/${eventId}/images`), {
          method: 'POST',
          headers: token ? { Authorization: `Bearer ${token}` } : {},
          body: formData,
        })
        if (!imgRes.ok) {
          setError('Событие создано, но не удалось загрузить фото')
        }
      }

      if (eventId) {
        navigate(`/events/${eventId}`)
      }
    } catch {
      setError('Ошибка сети')
    } finally {
      setLoading(false)
    }
  }

  if (!isAuthenticated) return null
  if (me && !isOrganizer) {
    return (
      <div style={{ maxWidth: 600, margin: '2rem auto', padding: '1rem' }}>
        <p style={{ color: 'red' }}>Доступ только для организаторов.</p>
        <Link to="/">На главную</Link>
      </div>
    )
  }

  return (
    <div style={{ maxWidth: 600, margin: '2rem auto', padding: '1rem' }}>
      <p style={{ marginBottom: '1rem' }}>
        <Link to="/">← На главную</Link>
      </p>
      <h1>Создание события</h1>

      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1rem' }}>
          <label>
            Название *{' '}
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            />
          </label>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label>
            Описание{' '}
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={4}
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            />
          </label>
        </div>

        <div style={{ marginBottom: '1rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <label>
            Начало *{' '}
            <input
              type="datetime-local"
              value={startAt}
              onChange={(e) => setStartAt(e.target.value)}
              required
              style={{ padding: '0.5rem' }}
            />
          </label>
          <label>
            Окончание *{' '}
            <input
              type="datetime-local"
              value={endAt}
              onChange={(e) => setEndAt(e.target.value)}
              required
              style={{ padding: '0.5rem' }}
            />
          </label>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label>
            Место{' '}
            <input
              type="text"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="Адрес или ссылка"
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            />
          </label>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label>
            <input
              type="checkbox"
              checked={isOnline}
              onChange={(e) => setIsOnline(e.target.checked)}
            />
            Онлайн
          </label>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label>
            <input
              type="checkbox"
              checked={isPublic}
              onChange={(e) => setIsPublic(e.target.checked)}
            />
            Публичное
          </label>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <strong>Категории:</strong>
          <div style={{ marginTop: '0.5rem' }}>
            {categories.map((c) => (
              <label key={c.id} style={{ display: 'block', marginBottom: '0.25rem' }}>
                <input
                  type="checkbox"
                  checked={selectedCategoryIds.includes(c.id)}
                  onChange={() => toggleCategory(c.id)}
                />
                {c.name}
              </label>
            ))}
          </div>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <strong>Типы билетов *</strong> (минимум один)
          {ticketTypes.map((tt, i) => (
            <div
              key={i}
              style={{
                border: '1px solid #ddd',
                borderRadius: 6,
                padding: '0.75rem',
                marginTop: '0.5rem',
                display: 'flex',
                gap: '0.5rem',
                flexWrap: 'wrap',
                alignItems: 'center',
              }}
            >
              <input
                type="text"
                placeholder="Название"
                value={tt.name}
                onChange={(e) => updateTicketType(i, 'name', e.target.value)}
                style={{ flex: 1, minWidth: 100, padding: '0.5rem' }}
              />
              <input
                type="number"
                placeholder="Цена"
                value={tt.price || ''}
                onChange={(e) => updateTicketType(i, 'price', e.target.value)}
                min={0}
                step={0.01}
                style={{ width: 80, padding: '0.5rem' }}
              />
              <input
                type="number"
                placeholder="Мест"
                value={tt.capacity || ''}
                onChange={(e) => updateTicketType(i, 'capacity', e.target.value)}
                min={1}
                style={{ width: 80, padding: '0.5rem' }}
              />
              <button type="button" onClick={() => removeTicketType(i)} disabled={ticketTypes.length <= 1}>
                ×
              </button>
            </div>
          ))}
          <button type="button" onClick={addTicketType} style={{ marginTop: '0.5rem', padding: '0.25rem 0.5rem' }}>
            + Добавить тип билета
          </button>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label>
            Фото события{' '}
            <input
              type="file"
              accept="image/*"
              onChange={(e) => setImageFile(e.target.files?.[0] ?? null)}
            />
          </label>
        </div>

        {error && <p style={{ color: 'red', marginBottom: '1rem' }}>{error}</p>}

        <button type="submit" disabled={loading} style={{ padding: '0.5rem 1rem' }}>
          {loading ? 'Создание...' : 'Создать событие'}
        </button>
      </form>
    </div>
  )
}
