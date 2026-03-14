import type { PropsWithChildren } from 'react'
import { cx } from '../../utils/cx'

type StatusTone = 'error' | 'success' | 'muted'

interface StatusMessageProps extends PropsWithChildren {
  tone?: StatusTone
  className?: string
  role?: 'alert' | 'status'
}

export function StatusMessage({ tone = 'muted', className, role = 'status', children }: StatusMessageProps) {
  return (
    <div className={cx('status', `status--${tone}`, className)} role={role} aria-live={role === 'alert' ? 'assertive' : 'polite'}>
      {children}
    </div>
  )
}
