import { Notify } from 'quasar'

export function useNotify() {
  function success(message: string) {
    Notify.create({
      type: 'positive',
      message,
      position: 'top-right',
      timeout: 3000,
    })
  }

  function error(message: string) {
    Notify.create({
      type: 'negative',
      message,
      position: 'top-right',
      timeout: 5000,
    })
  }

  function warning(message: string) {
    Notify.create({
      type: 'warning',
      message,
      position: 'top-right',
      timeout: 4000,
    })
  }

  function info(message: string) {
    Notify.create({
      type: 'info',
      message,
      position: 'top-right',
      timeout: 3000,
    })
  }

  return { success, error, warning, info }
}
