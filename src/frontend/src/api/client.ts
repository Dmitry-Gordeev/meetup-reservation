import config from '../config.json'

const TOKEN_KEY = 'meetup_token'
const API_BASE = (config.apiBaseUrl ?? '').replace(/\/$/, '')

/** Полный URL для API (для fetch, img src и т.д.) */
export function getApiUrl(path: string): string {
  return `${API_BASE}/api/v1${path.startsWith('/') ? path : '/' + path}`
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

export async function apiFetch(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const token = getToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  return fetch(getApiUrl(path), { ...options, headers })
}
