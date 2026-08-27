/**
 * Yönetim ekranının HANGİ kurumu düzenlediğini belirler.
 *
 * <p><b>Neden ayrı bir fonksiyon:</b> bu soruya iki ayrı yerde iki ayrı cevap veriliyordu ve
 * ikisi ayrışabiliyordu. `InstitutionPage` "listenin ilk satırı" diyordu, `institutionStore`
 * (dolayısıyla marka teması) "aktörün kendi kurumu". Okul rollerinde fark görünmüyordu:
 * `GetInstitutionsHandler` listeyi aktörün okuluna daraltır, yani liste tek elemanlıdır.
 * Ama `platform:tenant:manage` taşıyan aktörde daraltma YOKTUR — liste bütün okulları
 * döndürür ve sorgunun `ORDER BY`'ı yoktur.</p>
 *
 * <p><b>Ölçüldü (27.08.2026):</b> Postgres güncellenen satırı heap'te yerinden oynattığı için
 * liste sırası iki çağrı arasında değişti. Sonuç: admin ekranda Cumhuriyet'i görürken kendi
 * okulu Atatürk'tü; paleti Cumhuriyet'e yazdı, tema Atatürk'ten uygulandığı için ilk sayfa
 * geçişinde eski renge döndü. Yazma kaybolmadı — <b>yanlış okula</b> gitti.</p>
 *
 * <p><b>Kurum ağacıyla gelen üçüncü girdi (27.08.2026):</b> rota parametresi. İl/ilçe
 * yetkilisi `/institutions/:id` ile alt ağacındaki bir okulu açtığında hedef O OKULDUR —
 * kendi kurumu (İl MEM) değil. Sıra bu yüzden <b>rota → kendi kurumu → liste</b>'dir; en
 * belirgin niyet en önde.</p>
 *
 * @param routeInstitutionId Rota parametresi (`/institutions/:id`). Yoksa `null`.
 * @param ownInstitutionId Aktörün kendi kurumu (`/auth/me` → `authStore.user.institutionId`).
 *   Token'dan GELMEZ; sunucu kullanıcı kaydından üretir (ADR-0003 adım 2).
 * @param institutions Sunucudan gelen görünür kurum listesi.
 * @returns Düzenlenecek kurum kimliği; hiçbiri yoksa `null`.
 */
export function resolveEditableInstitutionId(
  routeInstitutionId: string | null | undefined,
  ownInstitutionId: string | null | undefined,
  institutions: readonly { id: string }[],
): string | null {
  // Rota parametresi en belirgin niyettir: kullanıcı BU kurumu açmak istedi. Yetki kararı
  // sunucunundur (InstitutionScopeGuard); rota, yetkinin ikinci bir kopyası değildir.
  if (routeInstitutionId) return routeInstitutionId

  // Kendi kurumu VARSA tartışma yok: listede görünmese bile hedef odur.
  if (ownInstitutionId) return ownInstitutionId

  if (institutions.length === 0) return null

  // Kurumu olmayan platform aktörü. Listeye düşülür ama SIRAYA BAĞLI KALINMAZ: sunucu sıra
  // garantisi vermiyor ve "ilk satır" her yazmadan sonra başka bir okul olabiliyordu.
  // Kimliğe göre kararlı seçim, aynı kümede her zaman aynı okulu verir.
  return [...institutions].sort((a, b) => a.id.localeCompare(b.id))[0]!.id
}
