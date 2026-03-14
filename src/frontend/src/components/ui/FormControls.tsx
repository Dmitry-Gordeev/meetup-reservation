import { forwardRef, type InputHTMLAttributes, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react'
import { cx } from '../../utils/cx'

export const TextInput = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(function TextInput(
  { className, ...props },
  ref
) {
  return <input ref={ref} className={cx('control', className)} {...props} />
})

export const SelectInput = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(function SelectInput(
  { className, ...props },
  ref
) {
  return <select ref={ref} className={cx('control', className)} {...props} />
})

export const TextAreaInput = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  function TextAreaInput({ className, ...props }, ref) {
    return <textarea ref={ref} className={cx('control', className)} {...props} />
  }
)
