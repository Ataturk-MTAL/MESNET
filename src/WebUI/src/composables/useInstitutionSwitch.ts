import { useRouter } from 'vue-router'
import { useInstitutionContext } from 'src/composables/useInstitutionContext'
import { useNotify, extractApiErrorCode } from 'src/composables/useNotify'
import {
  resolveActiveContextErrorMessage,
} from 'pages/institution/contextSelectQuery'

/**
 * "Bu kuruma geç" eylemi — `ContextSelectPage` ve `InstitutionChildrenTree` (kurum ağacındaki
 * okul satırları) aynı akışı paylaşır: bağlamı değiştir, panoya dön, sunucunun kapsam-dışı
 * hatasını (`Security.ActiveContextOutOfScope`) kurum adıyla göster.
 *
 * Tek yerde tutuluyor çünkü ikinci kopya ayrışma riski taşır — bkz.
 * `contextSelectQuery.ts#resolveActiveContextErrorMessage` yorumu (ham GUID ekrana düşmesin).
 */
export function useInstitutionSwitch() {
  const router = useRouter()
  const notify = useNotify()
  const context = useInstitutionContext()

  async function switchToInstitution(institutionId: string, institutionName: string): Promise<void> {
    try {
      await context.switchTo(institutionId)
      router.push('/dashboard').catch(() => {})
    } catch (e) {
      const message = resolveActiveContextErrorMessage(extractApiErrorCode(e), institutionName)
      if (message) {
        notify.error(message)
      } else {
        notify.apiError(e, 'Kuruma geçilirken bir hata oluştu.')
      }
    }
  }

  return { switching: context.switching, switchToInstitution }
}
