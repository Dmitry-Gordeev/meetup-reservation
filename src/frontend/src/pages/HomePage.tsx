import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch } from '../api/client'
import { useAuth } from '../contexts/AuthContext'
import { Button, PageContainer, StatusMessage } from '../components/ui'

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
    <PageContainer size="md">
      <h1>Meetup Reservation</h1>
      <nav className="cluster" style={{ marginBottom: '1.5rem' }} aria-label="Главная навигация">
        <Link to="/events">
          Каталог
        </Link>
        {isAuthenticated && me?.roles?.includes('organizer') && (
          <Link to="/organizer/create">
            Создать событие
          </Link>
        )}
        {isAuthenticated && (
          <Link to="/me/registrations">
            Мои регистрации
          </Link>
        )}
        {isAuthenticated && me?.roles?.includes('admin') && (
          <Link to="/admin">
            Админ-панель
          </Link>
        )}
        {isAuthenticated ? (
          <Button onClick={logout}>
            Выйти
          </Button>
        ) : (
          <>
            <Link to="/login">
              Войти
            </Link>
            <Link to="/register">Регистрация</Link>
          </>
        )}
      </nav>
      {isAuthenticated && me && (
        <StatusMessage tone="muted">
          Вы вошли как {me.email} (роли: {me.roles.join(', ')})
        </StatusMessage>
      )}
      {isAuthenticated && !me && <StatusMessage tone="muted">Вы авторизованы.</StatusMessage>}
      {!isAuthenticated && <p>Добро пожаловать. Войдите или зарегистрируйтесь.</p>}
    </PageContainer>
  )
}
