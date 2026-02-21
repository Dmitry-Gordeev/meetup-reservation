import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

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

  if (!isAuthenticated) return null
  if (me === null) return <p style={{ padding: '2rem' }}>Загрузка...</p>
  if (!me.roles?.includes('admin')) {
    return (
      <div style={{ maxWidth: 700, margin: '2rem auto', padding: '1rem' }}>
        <p style={{ marginBottom: '1rem' }}>
          <Link to="/">← Главная</Link>
        </p>
        <p style={{ color: 'red' }}>Доступ запрещён. Требуется роль администратора.</p>
      </div>
    )
  }

  return (
    <div style={{ maxWidth: 900, margin: '2rem auto', padding: '1rem' }}>
      <p style={{ marginBottom: '1rem' }}>
        <Link to="/">← Главная</Link>
      </p>

      <h1>Админ-панель</h1>

      <div style={{ marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', borderBottom: '1px solid #ddd' }}>
        <button
          type="button"
          onClick={() => setActiveTab('events')}
          style={{
            padding: '0.5rem 1rem',
            border: 'none',
            background: activeTab === 'events' ? '#e3f2fd' : 'transparent',
            borderBottom: activeTab === 'events' ? '2px solid #1976d2' : '2px solid transparent',
            cursor: 'pointer',
          }}
        >
          Модерация событий
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('users')}
          style={{
            padding: '0.5rem 1rem',
            border: 'none',
            background: activeTab === 'users' ? '#e3f2fd' : 'transparent',
            borderBottom: activeTab === 'users' ? '2px solid #1976d2' : '2px solid transparent',
            cursor: 'pointer',
          }}
        >
          Пользователи
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('categories')}
          style={{
            padding: '0.5rem 1rem',
            border: 'none',
            background: activeTab === 'categories' ? '#e3f2fd' : 'transparent',
            borderBottom: activeTab === 'categories' ? '2px solid #1976d2' : '2px solid transparent',
            cursor: 'pointer',
          }}
        >
          Категории
        </button>
      </div>

      {error && <p style={{ color: 'red', marginBottom: '1rem' }}>{error}</p>}

      {loading ? (
        <p>Загрузка...</p>
      ) : (
        <>
          {activeTab === 'events' && (
            <div>
              <h2>Модерация событий</h2>
              {events.length === 0 ? (
                <p style={{ color: '#666' }}>Нет событий для модерации.</p>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
                        <th style={{ padding: '0.5rem' }}>Событие</th>
                        <th style={{ padding: '0.5rem' }}>Дата</th>
                        <th style={{ padding: '0.5rem' }}>Организатор</th>
                        <th style={{ padding: '0.5rem' }}>Статус</th>
                        <th style={{ padding: '0.5rem' }}></th>
                      </tr>
                    </thead>
                    <tbody>
                      {events.map((evt) => (
                        <tr key={evt.id} style={{ borderBottom: '1px solid #eee' }}>
                          <td style={{ padding: '0.5rem' }}>
                            <Link to={`/events/${evt.id}`}>{evt.title}</Link>
                          </td>
                          <td style={{ padding: '0.5rem' }}>{formatDate(evt.startAt)}</td>
                          <td style={{ padding: '0.5rem' }}>
                            <Link to={`/organizers/${evt.organizerId}`}>{evt.organizerName ?? '—'}</Link>
                          </td>
                          <td style={{ padding: '0.5rem' }}>{evt.status === 'blocked' ? 'Заблокировано' : 'Активно'}</td>
                          <td style={{ padding: '0.5rem' }}>
                            {evt.status === 'blocked' ? (
                              <button
                                type="button"
                                onClick={() => handleUnblockEvent(evt.id)}
                                disabled={actionId !== null}
                                style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem' }}
                              >
                                Разблокировать
                              </button>
                            ) : (
                              <button
                                type="button"
                                onClick={() => handleBlockEvent(evt.id)}
                                disabled={actionId !== null}
                                style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem', color: '#c62828' }}
                              >
                                Заблокировать
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {activeTab === 'users' && (
            <div>
              <h2>Пользователи</h2>
              {users.length === 0 ? (
                <p style={{ color: '#666' }}>Нет пользователей.</p>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
                        <th style={{ padding: '0.5rem' }}>Email</th>
                        <th style={{ padding: '0.5rem' }}>Роли</th>
                        <th style={{ padding: '0.5rem' }}>Статус</th>
                        <th style={{ padding: '0.5rem' }}></th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((u) => (
                        <tr key={u.id} style={{ borderBottom: '1px solid #eee' }}>
                          <td style={{ padding: '0.5rem' }}>{u.email}</td>
                          <td style={{ padding: '0.5rem' }}>{u.roles.join(', ')}</td>
                          <td style={{ padding: '0.5rem' }}>{u.isBlocked ? 'Заблокирован' : 'Активен'}</td>
                          <td style={{ padding: '0.5rem' }}>
                            {u.isBlocked ? (
                              <button
                                type="button"
                                onClick={() =>
                                  u.roles.includes('organizer') ? handleUnblockOrganizer(u.id) : handleUnblockUser(u.id)
                                }
                                disabled={actionId !== null}
                                style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem' }}
                              >
                                Разблокировать
                              </button>
                            ) : (
                              <button
                                type="button"
                                onClick={() =>
                                  u.roles.includes('organizer') ? handleBlockOrganizer(u.id) : handleBlockUser(u.id)
                                }
                                disabled={actionId !== null || u.roles.includes('admin')}
                                style={{ padding: '0.2rem 0.5rem', fontSize: '0.8rem', color: '#c62828' }}
                              >
                                Заблокировать
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {activeTab === 'categories' && (
            <div>
              <h2>Категории</h2>
              <form onSubmit={handleCreateCategory} style={{ marginBottom: '1.5rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                <input
                  type="text"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  placeholder="Название категории"
                  style={{ padding: '0.5rem', flex: 1, maxWidth: 300 }}
                />
                <button type="submit" disabled={actionId !== null || !newCategoryName.trim()} style={{ padding: '0.5rem 1rem' }}>
                  Добавить
                </button>
              </form>
              {categories.length === 0 ? (
                <p style={{ color: '#666' }}>Нет категорий.</p>
              ) : (
                <ul style={{ listStyle: 'none', padding: 0 }}>
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
                          <input
                            type="text"
                            value={editingCategory.name}
                            onChange={(e) => setEditingCategory({ ...editingCategory, name: e.target.value })}
                            style={{ padding: '0.35rem', flex: 1, maxWidth: 200 }}
                          />
                          <label style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.9rem' }}>
                            <input
                              type="checkbox"
                              checked={editingCategory.isArchived}
                              onChange={(e) => setEditingCategory({ ...editingCategory, isArchived: e.target.checked })}
                            />
                            Архив
                          </label>
                          <input
                            type="number"
                            value={editingCategory.sortOrder}
                            onChange={(e) => setEditingCategory({ ...editingCategory, sortOrder: Number(e.target.value) || 0 })}
                            style={{ padding: '0.35rem', width: 60 }}
                          />
                          <button type="submit" disabled={actionId !== null} style={{ padding: '0.35rem 0.75rem' }}>
                            Сохранить
                          </button>
                          <button type="button" onClick={() => setEditingCategory(null)} style={{ padding: '0.35rem 0.75rem' }}>
                            Отмена
                          </button>
                        </form>
                      ) : (
                        <>
                          <span>
                            {cat.name}
                            {cat.isArchived && <span style={{ marginLeft: '0.5rem', color: '#666', fontSize: '0.85rem' }}>(архив)</span>}
                          </span>
                          <button type="button" onClick={() => setEditingCategory(cat)} style={{ padding: '0.35rem 0.75rem', fontSize: '0.85rem' }}>
                            Редактировать
                          </button>
                        </>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </>
      )}
    </div>
  )
}
