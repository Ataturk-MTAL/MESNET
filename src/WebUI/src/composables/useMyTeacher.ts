import { ref } from 'vue'
import { enrollmentApi, type TeacherProfileDto } from 'src/api/enrollment'

const HTTP_NOT_FOUND = 404

/**
 * Oturumdaki kullanıcının öğretmen kaydı (aktif okulda).
 *
 * Neden ayrı: öğretmen rolü `institution:view` taşımaz, öğretmen listesini okuyamaz; ama
 * rehberlik ziyareti ve faaliyet raporu kaydı öğretmen kimliği ister. Kaydı olmayan kullanıcı
 * (ör. müdür) hata değildir — `teacher` boş kalır ve form öğretmen seçtirir.
 */
export function useMyTeacher() {
  const teacher = ref<TeacherProfileDto | null>(null)
  const loaded = ref(false)

  async function load(): Promise<void> {
    try {
      const { data } = await enrollmentApi.getMyTeacher()
      teacher.value = data
    } catch (e) {
      const status = (e as { response?: { status?: number } } | null)?.response?.status
      if (status !== HTTP_NOT_FOUND) throw e
      teacher.value = null
    }
    loaded.value = true
  }

  return { teacher, loaded, load }
}
