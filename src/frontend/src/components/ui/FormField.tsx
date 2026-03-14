import type { PropsWithChildren } from 'react'

interface FormFieldProps extends PropsWithChildren {
  label: string
  htmlFor: string
  required?: boolean
  className?: string
}

export function FormField({ label, htmlFor, required, className, children }: FormFieldProps) {
  return (
    <div className={className ?? 'field'}>
      <label className="field__label" htmlFor={htmlFor}>
        {label}
        {required && <span className="field__required"> *</span>}
      </label>
      {children}
    </div>
  )
}
