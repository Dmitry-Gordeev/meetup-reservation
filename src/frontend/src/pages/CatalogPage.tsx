import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { Button, Card, PageContainer, SelectInput, StatusMessage } from '../components/ui'

interface EventItem {
  id: number
  organizerId: number
  organizerName: string | null
  title: string
  startAt: string
  endAt: string
  location: string | null
  isOnline: boolean
  status: string
  createdAt: string
  categoryIds: number[]
}

interface Category {
  id: number
  name: string
}

interface CatalogResponse {
  items: EventItem[]
  nextCursor?: string
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

export default function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [events, setEvents] = useState<EventItem[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [nextCursor, setNextCursor] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const categoryIdsParam = searchParams.get('categoryIds') ?? ''
  const categoryIds = categoryIdsParam ? categoryIdsParam.split(',').map((s) => s.trim()).filter(Boolean) : []
  const sortBy = searchParams.get('sortBy') ?? 'startAt'

  const loadEvents = useCallback(
    async (cursor?: string) => {
      setLoading(true)
      setError('')
      try {
        const effectiveCategoryIds = categoryIdsParam
          ? categoryIdsParam.split(',').map((s) => s.trim()).filter(Boolean)
          : []
        const params = new URLSearchParams()
        params.set('limit', '10')
        if (sortBy) params.set('sortBy', sortBy)
        if (effectiveCategoryIds.length > 0) params.set('categoryIds', effectiveCategoryIds.join(','))
        if (cursor) params.set('cursor', cursor)

        const res = await apiFetch(`/events?${params}`)
        if (!res.ok) throw new Error('Ошибка загрузки')
        const data: CatalogResponse = await res.json()
        if (cursor) {
          setEvents((prev) => [...prev, ...data.items])
        } else {
          setEvents(data.items)
        }
        setNextCursor(data.nextCursor ?? null)
      } catch {
        setError('Не удалось загрузить события')
      } finally {
        setLoading(false)
      }
    },
    [categoryIdsParam, sortBy]
  )

  useEffect(() => {
    loadEvents()
  }, [loadEvents])

  useEffect(() => {
    apiFetch('/categories')
      .then((r) => (r.ok ? r.json() : []))
      .then(setCategories)
      .catch(() => setCategories([]))
  }, [])

  function handleFilterChange(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    setSearchParams(next)
  }

  function handleCategoryToggle(catId: number) {
    const next = new URLSearchParams(searchParams)
    const nextIds = categoryIds.includes(String(catId))
      ? categoryIds.filter((c) => c !== String(catId))
      : [...categoryIds, String(catId)]
    if (nextIds.length > 0) next.set('categoryIds', nextIds.join(','))
    else next.delete('categoryIds')
    setSearchParams(next)
  }

  function handleLoadMore() {
    if (nextCursor) loadEvents(nextCursor)
  }

  return (
    <PageContainer size="lg" className="stack-lg">
      <p>
        <Link to="/">← На главную</Link>
      </p>
      <h1>Каталог событий</h1>

      <section className="surface-card stack" aria-label="Фильтры каталога">
        <div>
          <strong>Категории:</strong>{' '}
          {categories.map((c) => (
            <label key={c.id} className="checkbox-row" style={{ display: 'inline-flex', marginRight: '1rem' }}>
              <input
                type="checkbox"
                checked={categoryIds.includes(String(c.id))}
                onChange={() => handleCategoryToggle(c.id)}
              />
              {c.name}
            </label>
          ))}
        </div>
        <label className="field">
          <span className="field__label">Сортировка</span>
          <SelectInput
            value={sortBy}
            onChange={(e) => handleFilterChange('sortBy', e.target.value)}
          >
            <option value="startAt">По дате проведения</option>
            <option value="createdAt">По дате создания</option>
          </SelectInput>
        </label>
      </section>

      {error && (
        <StatusMessage tone="error" role="alert">
          {error}
        </StatusMessage>
      )}

      {loading && events.length === 0 ? (
        <p>Загрузка...</p>
      ) : events.length === 0 ? (
        <StatusMessage tone="muted">Событий пока нет</StatusMessage>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
          {events.map((evt) => (
            <li key={evt.id}>
              <Card>
                <Link to={`/events/${evt.id}`} style={{ textDecoration: 'none', color: 'inherit' }}>
                  <h3 style={{ marginBottom: '0.5rem' }}>{evt.title}</h3>
                </Link>
                <p className="muted" style={{ margin: '0 0 0.25rem 0', fontSize: '0.92rem' }}>
                  {formatDate(evt.startAt)} — {evt.location || (evt.isOnline ? 'Онлайн' : '—')}
                </p>
                {evt.organizerName && (
                  <p style={{ margin: 0, fontSize: '0.92rem' }}>
                    Организатор: <Link to={`/organizers/${evt.organizerId}`}>{evt.organizerName}</Link>
                  </p>
                )}
              </Card>
            </li>
          ))}
        </ul>
      )}

      {nextCursor && (
        <div>
          <Button onClick={handleLoadMore} disabled={loading}>
            {loading ? 'Загрузка...' : 'Далее'}
          </Button>
        </div>
      )}
    </PageContainer>
  )
}
