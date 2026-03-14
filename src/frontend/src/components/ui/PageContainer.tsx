import type { PropsWithChildren } from 'react'
import { cx } from '../../utils/cx'

type ContainerSize = 'sm' | 'md' | 'lg'

interface PageContainerProps extends PropsWithChildren {
  size?: ContainerSize
  className?: string
}

export function PageContainer({ size = 'md', className, children }: PageContainerProps) {
  return <div className={cx('page-container', `page-container--${size}`, className)}>{children}</div>
}
