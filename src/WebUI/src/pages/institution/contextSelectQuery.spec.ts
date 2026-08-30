import { describe, it, expect } from 'vitest'
import {
  ACTIVE_CONTEXT_OUT_OF_SCOPE_CODE,
  resolveActiveContextErrorMessage,
} from './contextSelectQuery'

describe('resolveActiveContextErrorMessage — bulgu 3: ham GUID ekrana düşmesin', () => {
  it('Security.ActiveContextOutOfScope kodunda kurum adını içeren insan-okunur mesaj döner', () => {
    const mesaj = resolveActiveContextErrorMessage(
      ACTIVE_CONTEXT_OUT_OF_SCOPE_CODE,
      'Atatürk Mesleki ve Teknik Anadolu Lisesi',
    )

    expect(mesaj).toBe('Atatürk Mesleki ve Teknik Anadolu Lisesi yetki alanınızda değil.')
  })

  it('döndürülen mesaj GUID taşımaz', () => {
    const mesaj = resolveActiveContextErrorMessage(
      ACTIVE_CONTEXT_OUT_OF_SCOPE_CODE,
      'Örnek Kurum',
    )

    expect(mesaj).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i)
  })

  it('tanınmayan koda null döner — çağıran genel hata yoluna düşer', () => {
    expect(resolveActiveContextErrorMessage('Security.UserNotFound', 'Örnek Kurum')).toBeNull()
    expect(resolveActiveContextErrorMessage(undefined, 'Örnek Kurum')).toBeNull()
  })
})
