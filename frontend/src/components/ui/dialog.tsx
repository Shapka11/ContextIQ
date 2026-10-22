import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import type { ReactNode } from 'react'
export function ConfirmDialog({
  trigger,
  title,
  children,
  action,
}: {
  trigger: ReactNode
  title: string
  children: ReactNode
  action: ReactNode
}) {
  return (
    <Dialog.Root>
      <Dialog.Trigger asChild>{trigger}</Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="dialog-overlay" />
        <Dialog.Content className="dialog-content">
          <Dialog.Title>{title}</Dialog.Title>
          <Dialog.Description asChild>
            <div className="muted">{children}</div>
          </Dialog.Description>
          <div className="dialog-actions">
            <Dialog.Close asChild>
              <button className="button button-secondary">Отмена</button>
            </Dialog.Close>
            {action}
          </div>
          <Dialog.Close className="dialog-close" aria-label="Закрыть">
            <X size={19} />
          </Dialog.Close>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
