import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { Button, Card, PageContainer, StatusMessage, TextInput } from '../components/ui'

interface AdminEvent {
  id: number
  organizerId: number
  title: string
  startAt: string
  status: string
  organizerName: string | null
}

interface AdminUser {
  id: number
  email: string
  isBlocked: boolean
  roles: string[]
}

interface AdminCategory {
  id: number
  name: string
  isArchived: boolean
  sortOrder: number
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

export default function AdminPage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [me, setMe] = useState<{ id: string; roles: string[] } | null>(null)
  const [activeTab, setActiveTab] = useState<'events' | 'users' | 'categories'>('events')

  const [events, setEvents] = useState<AdminEvent[]>([])
  const [users, setUsers] = useState<AdminUser[]>([])
  const [categories, setCategories] = useState<AdminCategory[]>([])

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionId, setActionId] = useState<number | null>(null)

  const [newCategoryName, setNewCategoryName] = useState('')
  const [editingCategory, setEditingCategory] = useState<AdminCategory | null>(null)
  const tabOrder: Array<'events' | 'users' | 'categories'> = ['events', 'users', 'categories']

  const loadEvents = useCallback(() => {
    apiFetch('/admin/events')
      .then((r) => (r.ok ? r.json() : []))
      .then(setEvents)
      .catch(() => setEvents([]))
  }, [])

  const loadUsers = useCallback(() => {
    apiFetch('/admin/users')
      .then((r) => (r.ok ? r.json() : []))
      .then(setUsers)
      .catch(() => setUsers([]))
  }, [])

  const loadCategories = useCallback(() => {
    apiFetch('/admin/categories')
      .then((r) => (r.ok ? r.json() : []))
      .then(setCategories)
      .catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    if (!isAuthenticated) navigate('/login')
  }, [isAuthenticated, navigate])

  useEffect(() => {
    if (!isAuthenticated) return
    apiFetch('/me')
      .then((r) => (r.ok ? r.json() : null))
      .then(setMe)
      .catch(() => setMe(null))
  }, [isAuthenticated])

  useEffect(() => {
    if (!me?.roles?.includes('admin')) return
    setLoading(true)
    setError('')
    Promise.all([
      apiFetch('/admin/events').then((r) => (r.ok ? r.json() : [])),
      apiFetch('/admin/users').then((r) => (r.ok ? r.json() : [])),
      apiFetch('/admin/categories').then((r) => (r.ok ? r.json() : [])),
    ])
      .then(([evts, usrs, cats]) => {
        setEvents(evts)
        setUsers(usrs)
        setCategories(cats)
      })
      .catch(() => setError('Ошибка загрузки'))
      .finally(() => setLoading(false))
  }, [me])

  async function handleBlockEvent(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/events/${id}/block`, { method: 'PATCH' })
      if (res.ok) loadEvents()
    } finally {
      setActionId(null)
    }
  }

  async function handleUnblockEvent(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/events/${id}/unblock`, { method: 'PATCH' })
      if (res.ok) loadEvents()
    } finally {
      setActionId(null)
    }
  }

  async function handleBlockUser(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/users/${id}/block`, { method: 'PATCH' })
      if (res.ok) loadUsers()
    } finally {
      setActionId(null)
    }
  }

  async function handleUnblockUser(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/users/${id}/unblock`, { method: 'PATCH' })
      if (res.ok) loadUsers()
    } finally {
      setActionId(null)
    }
  }

  async function handleBlockOrganizer(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/organizers/${id}/block`, { method: 'PATCH' })
      if (res.ok) loadUsers()
    } finally {
      setActionId(null)
    }
  }

  async function handleUnblockOrganizer(id: number) {
    setActionId(id)
    try {
      const res = await apiFetch(`/admin/organizers/${id}/unblock`, { method: 'PATCH' })
      if (res.ok) loadUsers()
    } finally {
      setActionId(null)
    }
  }

  async function handleCreateCategory(e: React.FormEvent) {
    e.preventDefault()
    if (!newCategoryName.trim()) return
    setActionId(-1)
    try {
      const res = await apiFetch('/admin/categories', {
        method: 'POST',
        body: JSON.stringify({ name: newCategoryName.trim() }),
      })
      if (res.ok) {
        setNewCategoryName('')
        loadCategories()
      }
    } finally {
      setActionId(null)
    }
  }

  async function handleUpdateCategory(e: React.FormEvent) {
    e.preventDefault()
    if (!editingCategory) return
    setActionId(editingCategory.id)
    try {
      const res = await apiFetch(`/admin/categories/${editingCategory.id}`, {
        method: 'PATCH',
        body: JSON.stringify({
          name: editingCategory.name,
          isArchived: editingCategory.isArchived,
          sortOrder: editingCategory.sortOrder,
        }),
      })
      if (res.ok) {
        setEditingCategory(null)
        loadCategories()
      }
    } finally {
      setActionId(null)
    }
  }

  function handleTabKeyDown(e: React.KeyboardEvent<HTMLButtonElement>, tab: 'events' | 'users' | 'categories') {
    if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return
    e.preventDefault()
    const current = tabOrder.indexOf(tab)
    const direction = e.key === 'ArrowRight' ? 1 : -1
    const nextIndex = (current + direction + tabOrder.length) % tabOrder.length
    setActiveTab(tabOrder[nextIndex])
  }

  if (!isAuthenticated) return null
  if (me === null) return <PageContainer size="lg"><p>Загрузка...</p></PageContainer>
  if (!me.roles?.includes('admin')) {
    return (
      <PageContainer size="md" className="stack">
        <p>
          <Link to="/">← Главная</Link>
        </p>
        <StatusMessage tone="error" role="alert">
          Доступ запрещён. Требуется роль администратора.
        </StatusMessage>
      </PageContainer>
    )
  }

  return (
    <PageContainer size="lg" className="stack-lg">
      <p>
        <Link to="/">← Главная</Link>
      </p>

      <h1>Админ-панель</h1>

      <div className="tabs" role="tablist" aria-label="Разделы админ-панели">
        <button
          type="button"
          id="admin-tab-events"
          role="tab"
          aria-selected={activeTab === 'events'}
          aria-controls="admin-panel-events"
          onClick={() => setActiveTab('events')}
          onKeyDown={(e) => handleTabKeyDown(e, 'events')}
          className="tab-btn"
        >
          Модерация событий
        </button>
        <button
          type="button"
          id="admin-tab-users"
          role="tab"
          aria-selected={activeTab === 'users'}
          aria-controls="admin-panel-users"
          onClick={() => setActiveTab('users')}
          onKeyDown={(e) => handleTabKeyDown(e, 'users')}
          className="tab-btn"
        >
          Пользователи
        </button>
        <button
          type="button"
          id="admin-tab-categories"
          role="tab"
          aria-selected={activeTab === 'categories'}
          aria-controls="admin-panel-categories"
          onClick={() => setActiveTab('categories')}
          onKeyDown={(e) => handleTabKeyDown(e, 'categories')}
          className="tab-btn"
        >
          Категории
        </button>
      </div>

      {error && (
        <StatusMessage tone="error" role="alert">
          {error}
        </StatusMessage>
      )}

      {loading ? (
        <p>Загрузка...</p>
      ) : (
        <>
          {activeTab === 'events' && (
            <Card role="tabpanel" id="admin-panel-events" aria-labelledby="admin-tab-events">
              <h2>Модерация событий</h2>
              {events.length === 0 ? (
                <StatusMessage tone="muted">Нет событий для модерации.</StatusMessage>
              ) : (
                <div className="table-wrap">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Событие</th>
                        <th>Дата</th>
                        <th>Организатор</th>
                        <th>Статус</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {events.map((evt) => (
                        <tr key={evt.id}>
                          <td>
                            <Link to={`/events/${evt.id}`}>{evt.title}</Link>
                          </td>
                          <td>{formatDate(evt.startAt)}</td>
                          <td>
                            <Link to={`/organizers/${evt.organizerId}`}>{evt.organizerName ?? '—'}</Link>
                          </td>
                          <td>{evt.status === 'blocked' ? 'Заблокировано' : 'Активно'}</td>
                          <td>
                            {evt.status === 'blocked' ? (
                              <Button
                                type="button"
                                onClick={() => handleUnblockEvent(evt.id)}
                                disabled={actionId !== null}
                                size="sm"
                              >
                                Разблокировать
                              </Button>
                            ) : (
                              <Button
                                type="button"
                                onClick={() => handleBlockEvent(evt.id)}
                                disabled={actionId !== null}
                                size="sm"
                                variant="danger"
                              >
                                Заблокировать
                              </Button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          )}

          {activeTab === 'users' && (
            <Card role="tabpanel" id="admin-panel-users" aria-labelledby="admin-tab-users">
              <h2>Пользователи</h2>
              {users.length === 0 ? (
                <StatusMessage tone="muted">Нет пользователей.</StatusMessage>
              ) : (
                <div className="table-wrap">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Email</th>
                        <th>Роли</th>
                        <th>Статус</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((u) => (
                        <tr key={u.id}>
                          <td>{u.email}</td>
                          <td>{u.roles.join(', ')}</td>
                          <td>{u.isBlocked ? 'Заблокирован' : 'Активен'}</td>
                          <td>
                            {u.isBlocked ? (
                              <Button
                                type="button"
                                onClick={() =>
                                  u.roles.includes('organizer') ? handleUnblockOrganizer(u.id) : handleUnblockUser(u.id)
                                }
                                disabled={actionId !== null}
                                size="sm"
                              >
                                Разблокировать
                              </Button>
                            ) : (
                              <Button
                                type="button"
                                onClick={() =>
                                  u.roles.includes('organizer') ? handleBlockOrganizer(u.id) : handleBlockUser(u.id)
                                }
                                disabled={actionId !== null || u.roles.includes('admin')}
                                size="sm"
                                variant="danger"
                              >
                                Заблокировать
                              </Button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          )}

          {activeTab === 'categories' && (
            <Card className="stack" role="tabpanel" id="admin-panel-categories" aria-labelledby="admin-tab-categories">
              <h2>Категории</h2>
              <form onSubmit={handleCreateCategory} className="cluster">
                <TextInput
                  type="text"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  placeholder="Название категории"
                  style={{ flex: 1, minWidth: 220, maxWidth: 360 }}
                  aria-label="Название категории"
                />
                <Button type="submit" variant="primary" disabled={actionId !== null || !newCategoryName.trim()}>
                  Добавить
                </Button>
              </form>
              {categories.length === 0 ? (
                <StatusMessage tone="muted">Нет категорий.</StatusMessage>
              ) : (
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
                  {categories.map((cat) => (
                    <li
                      key={cat.id}
                      style={{
                        border: '1px solid #ddd',
                        borderRadius: 6,
                        padding: '0.75rem',
                        marginBottom: '0.5rem',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                      }}
                    >
                      {editingCategory?.id === cat.id ? (
                        <form onSubmit={handleUpdateCategory} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flex: 1 }}>
                          <TextInput
                            type="text"
                            value={editingCategory.name}
                            onChange={(e) => setEditingCategory({ ...editingCategory, name: e.target.value })}
                            style={{ flex: 1, maxWidth: 220 }}
                          />
                          <label style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.9rem' }}>
                            <input
                              type="checkbox"
                              checked={editingCategory.isArchived}
                              onChange={(e) => setEditingCategory({ ...editingCategory, isArchived: e.target.checked })}
                            />
                            Архив
                          </label>
                          <TextInput
                            type="number"
                            value={editingCategory.sortOrder}
                            onChange={(e) => setEditingCategory({ ...editingCategory, sortOrder: Number(e.target.value) || 0 })}
                            style={{ width: 80 }}
                          />
                          <Button type="submit" disabled={actionId !== null} size="sm">
                            Сохранить
                          </Button>
                          <Button type="button" onClick={() => setEditingCategory(null)} size="sm">
                            Отмена
                          </Button>
                        </form>
                      ) : (
                        <>
                          <span>
                            {cat.name} {cat.isArchived && <span className="muted" style={{ fontSize: '0.85rem' }}>(архив)</span>}
                          </span>
                          <Button type="button" onClick={() => setEditingCategory(cat)} size="sm">
                            Редактировать
                          </Button>
                        </>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          )}
        </>
      )}
    </PageContainer>
  )
}
