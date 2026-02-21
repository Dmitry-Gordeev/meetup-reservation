import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

export default function HomePage() {
  const { isAuthenticated, logout } = useAuth()
  const [me, setMe] = useState<{ id: string; email: string; roles: string[] } | null>(null)

  useEffect(() => {
    if (!isAuthenticated) return
    apiFetch('/me')
      .then((r) => (r.ok ? r.json() : null))
      .then(setMe)
      .catch(() => setMe(null))
  }, [isAuthenticated])

  return (
    <div style={{ maxWidth: 800, margin: '2rem auto', padding: '1rem' }}>
      <h1>Meetup Reservation</h1>
      <nav style={{ marginBottom: '2rem' }}>
        <Link to="/events" style={{ marginRight: '1rem' }}>
          Каталог
        </Link>
        {isAuthenticated && me?.roles?.includes('organizer') && (
          <Link to="/organizer/create" style={{ marginRight: '1rem' }}>
            Создать событие
          </Link>
        )}
        {isAuthenticated && (
          <Link to="/me/registrations" style={{ marginRight: '1rem' }}>
            Мои регистрации
          </Link>
        )}
        {isAuthenticated ? (
          <button onClick={logout} style={{ marginRight: '1rem' }}>
            Выйти
          </button>
        ) : (
          <>
            <Link to="/login" style={{ marginRight: '1rem' }}>
              Войти
            </Link>
            <Link to="/register">Регистрация</Link>
          </>
        )}
      </nav>
      {isAuthenticated && me && (
        <p>
          Вы вошли как {me.email} (роли: {me.roles.join(', ')})
        </p>
      )}
      {isAuthenticated && !me && <p>Вы авторизованы.</p>}
      {!isAuthenticated && <p>Добро пожаловать. Войдите или зарегистрируйтесь.</p>}
    </div>
  )
}
