import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getApiUrl } from '../api/client'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'organizer' | 'participant'>('participant')
  const [name, setName] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)

    const body: Record<string, string> = {
      email: email.trim(),
      password,
      role,
    }
    if (role === 'organizer') {
      body.name = name.trim() || email.split('@')[0]
    } else {
      body.firstName = firstName.trim() || 'Участник'
      body.lastName = lastName.trim() || ' '
    }

    try {
      const res = await fetch(getApiUrl('/auth/register'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(data.error || `Ошибка ${res.status}`)
        return
      }
      navigate('/login')
    } catch {
      setError('Ошибка сети')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Регистрация</h1>
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: '1rem' }}>
          <label>
            Email <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            />
          </label>
        </div>
        <div style={{ marginBottom: '1rem' }}>
          <label>
            Пароль <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
              autoComplete="new-password"
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            />
          </label>
        </div>
        <div style={{ marginBottom: '1rem' }}>
          <label>
            Роль{' '}
            <select
              value={role}
              onChange={(e) => setRole(e.target.value as 'organizer' | 'participant')}
              style={{ display: 'block', width: '100%', padding: '0.5rem' }}
            >
              <option value="participant">Участник</option>
              <option value="organizer">Организатор</option>
            </select>
          </label>
        </div>
        {role === 'organizer' && (
          <div style={{ marginBottom: '1rem' }}>
            <label>
              Имя / Название <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Название организации или имя"
                style={{ display: 'block', width: '100%', padding: '0.5rem' }}
              />
            </label>
          </div>
        )}
        {role === 'participant' && (
          <>
            <div style={{ marginBottom: '1rem' }}>
              <label>
                Имя <input
                  type="text"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                />
              </label>
            </div>
            <div style={{ marginBottom: '1rem' }}>
              <label>
                Фамилия <input
                  type="text"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  style={{ display: 'block', width: '100%', padding: '0.5rem' }}
                />
              </label>
            </div>
          </>
        )}
        {error && <p style={{ color: 'red', marginBottom: '1rem' }}>{error}</p>}
        <button type="submit" disabled={loading} style={{ padding: '0.5rem 1rem' }}>
          {loading ? 'Регистрация...' : 'Зарегистрироваться'}
        </button>
      </form>
      <p style={{ marginTop: '1rem' }}>
        Уже есть аккаунт? <Link to="/login">Войти</Link>
      </p>
    </div>
  )
}
