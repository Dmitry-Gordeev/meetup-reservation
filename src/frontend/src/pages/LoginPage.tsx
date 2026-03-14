import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { getApiUrl } from '../api/client'
import { Button, FormField, PageContainer, StatusMessage, TextInput } from '../components/ui'

export default function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const res = await fetch(getApiUrl('/auth/login'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim(), password }),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(data.error || `Ошибка ${res.status}`)
        return
      }
      if (data.token) {
        login(data.token)
        navigate('/')
      } else {
        setError('Токен не получен')
      }
    } catch {
      setError('Ошибка сети')
    } finally {
      setLoading(false)
    }
  }

  return (
    <PageContainer size="sm">
      <h1>Вход</h1>
      <form onSubmit={handleSubmit} className="surface-card stack">
        <FormField label="Email" htmlFor="login-email" required>
          <TextInput
            id="login-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
        </FormField>
        <FormField label="Пароль" htmlFor="login-password" required>
          <TextInput
            id="login-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />
        </FormField>
        {error && (
          <StatusMessage tone="error" role="alert">
            {error}
          </StatusMessage>
        )}
        <Button type="submit" variant="primary" disabled={loading}>
          {loading ? 'Вход...' : 'Войти'}
        </Button>
      </form>
      <p style={{ marginTop: '1rem' }} className="muted">
        Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
      </p>
    </PageContainer>
  )
}
