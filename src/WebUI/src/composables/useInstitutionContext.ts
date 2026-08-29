import { ref } from 'vue'
import { useAuthStore } from 'stores/auth'
import { useInstitutionStore } from 'stores/institution'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useEntityOptionsStore } from 'stores/entityOptions'
import { contextApi } from 'src/api/context'

/**
 * Aktif bağlam değiştirmenin TEK yolu.
 *
 * <p><b>Neden tek bir yer:</b> bağlam değişimi kuruma bağlı bütün önbellekleri geçersiz
 * kılmak zorundadır. Her sayfa kendi başına hatırlasaydı, biri unuttuğunda kullanıcı yeni
 * okulda ama ESKİ okulun dönem listesiyle çalışırdı ve yazma sessizce yanlış döneme
 * giderdi.</p>
 *
 * <p><b>Sıra önemlidir:</b> önce sunucu (kayıt + izin önbelleği geçersizleme), sonra
 * `/auth/me` (yeni claim'ler), sonra yerel store'lar. Ters sırada store'lar eski bağlamla
 * yeniden dolardı.</p>
 */
export function useInstitutionContext() {
  const authStore = useAuthStore()
  const institutionStore = useInstitutionStore()
  const periodStore = useAcademicPeriodStore()
  const entityOptions = useEntityOptionsStore()

  const switching = ref(false)

  async function switchTo(institutionId: string | null): Promise<void> {
    switching.value = true
    try {
      await contextApi.setActiveInstitution(institutionId)

      // Sunucu claim'leri yeniden üretti; kullanıcı bilgisini tazele.
      // NOT: authStore'da `/auth/me` çağıran metot `refreshUser` DEĞİL, `loadPermissions`'tır
      // (permission listesini de bu uçtan çeker) — brief'in varsaydığı ad gerçekte yok.
      await authStore.loadPermissions()

      // Kuruma bağlı her önbellek düşer. Hangi store'un neye ihtiyacı olduğunu burada
      // bilmek zorunda DEĞİLİZ — hepsi temizlenir, sayfalar kendi yüklemesini yapar.
      institutionStore.clear()
      periodStore.clear()
      entityOptions.clear()
    } finally {
      switching.value = false
    }
  }

  return { switching, switchTo }
}
