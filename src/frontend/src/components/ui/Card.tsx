import type { HTMLAttributes, PropsWithChildren } from 'react'
import { cx } from '../../utils/cx'

interface CardProps extends PropsWithChildren, HTMLAttributes<HTMLDivElement> {}

export function Card({ className, children, ...props }: CardProps) {
  return (
    <div className={cx('surface-card', className)} {...props}>
      {children}
    </div>
  )
}
