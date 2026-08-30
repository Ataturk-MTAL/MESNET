import api from 'boot/axios'

export const contextApi = {
  /** `null` bağlamı temizler ve kullanıcıyı ev kurumuna döndürür. */
  setActiveInstitution: (institutionId: string | null) =>
    api.post('/security/users/me/context', { institutionId }),
}
