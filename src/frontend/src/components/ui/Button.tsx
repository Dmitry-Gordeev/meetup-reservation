import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'
import { cx } from '../../utils/cx'

type ButtonVariant = 'primary' | 'secondary' | 'danger'
type ButtonSize = 'md' | 'sm'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, PropsWithChildren {
  variant?: ButtonVariant
  size?: ButtonSize
}

export function Button({
  variant = 'secondary',
  size = 'md',
  className,
  type = 'button',
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cx('btn', `btn--${variant}`, size === 'sm' && 'btn--sm', className)}
      {...props}
    >
      {children}
    </button>
  )
}
