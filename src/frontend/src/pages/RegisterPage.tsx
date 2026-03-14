import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getApiUrl } from '../api/client'
import { Button, FormField, PageContainer, SelectInput, StatusMessage, TextInput } from '../components/ui'

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
    <PageContainer size="sm">
      <h1>Регистрация</h1>
      <form onSubmit={handleSubmit} className="surface-card stack">
        <FormField label="Email" htmlFor="register-email" required>
          <TextInput
            id="register-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
          />
        </FormField>
        <FormField label="Пароль" htmlFor="register-password" required>
          <TextInput
            id="register-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
              autoComplete="new-password"
          />
        </FormField>
        <FormField label="Роль" htmlFor="register-role">
          <SelectInput
            id="register-role"
              value={role}
              onChange={(e) => setRole(e.target.value as 'organizer' | 'participant')}
          >
              <option value="participant">Участник</option>
              <option value="organizer">Организатор</option>
          </SelectInput>
        </FormField>
        {role === 'organizer' && (
          <FormField label="Имя / Название" htmlFor="register-org-name">
            <TextInput
                id="register-org-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Название организации или имя"
            />
          </FormField>
        )}
        {role === 'participant' && (
          <>
            <FormField label="Имя" htmlFor="register-first-name">
              <TextInput
                  id="register-first-name"
                  type="text"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
              />
            </FormField>
            <FormField label="Фамилия" htmlFor="register-last-name">
              <TextInput
                  id="register-last-name"
                  type="text"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
              />
            </FormField>
          </>
        )}
        {error && (
          <StatusMessage tone="error" role="alert">
            {error}
          </StatusMessage>
        )}
        <Button type="submit" variant="primary" disabled={loading}>
          {loading ? 'Регистрация...' : 'Зарегистрироваться'}
        </Button>
      </form>
      <p style={{ marginTop: '1rem' }} className="muted">
        Уже есть аккаунт? <Link to="/login">Войти</Link>
      </p>
    </PageContainer>
  )
}
