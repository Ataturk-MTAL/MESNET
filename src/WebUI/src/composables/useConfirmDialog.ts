import { useQuasar } from 'quasar'

interface ConfirmOptions {
  title: string
  message: string
  okLabel?: string
  okColor?: string
  onOk: () => Promise<void> | void
}

export function useConfirmDialog() {
  const $q = useQuasar()

  function confirm(opts: ConfirmOptions) {
    $q.dialog({
      title: opts.title,
      message: opts.message,
      cancel: { flat: true, label: 'İptal' },
      ok: { color: opts.okColor ?? 'negative', label: opts.okLabel ?? 'Tamam' },
      persistent: true,
    }).onOk(opts.onOk)
  }

  return { confirm }
}
