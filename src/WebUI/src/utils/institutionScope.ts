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

/**
 * Görüntülenen kurum, aktörün AKTİF BAĞLAMININ KENDİSİ mi?
 *
 * <p><b>Neden ayrı bir soru:</b> "hangi kurumu düzenliyorum" (`resolveEditableInstitutionId`)
 * ile "bu görüntüleme global store'a yazabilir mi" iki farklı sorudur. İl/ilçe yetkilisi
 * `Kurumlar` ağacında BAŞKA bir okulu görüntülerken `InstitutionPage` o okulun verisini kendi
 * YEREL state'inde tutar (bu fonksiyonun konusu değil); ama sayfa mutasyon sonrası
 * `institutionStore.clear()` / `academicPeriodStore.loadPeriods(true)` gibi GLOBAL önbellek
 * tazeleme çağrıları da yapıyordu — bu çağrılar "davranılan (aktif bağlamdaki) kurum" sorusuna
 * cevap veren store'ları hedefler. Görüntülenen kurum aktif bağlamdan FARKLIYSA bu çağrılar
 * YANLIŞ kurumun (aktif bağlamın) önbelleğini boşaltır: header'daki bağlam çipi kalıcı iskelete
 * düşer, dönem seçici aktif bağlamın verisiyle değil görüntülenen kurumun (boş) verisiyle
 * tazelenir. Global store'u kullanmak/tazelemek SADECE iki soru aynı cevaba sahipken serbesttir
 * — global bağlamı geçersiz kılma yetkisi `useInstitutionContext().switchTo()`'nundur, bir
 * görüntüleme sayfası bu yetkiyi ödünç almaz.</p>
 *
 * @param viewedInstitutionId Sayfanın gösterdiği kurum (rota parametresi çözüldükten sonra).
 * @param activeContextInstitutionId `authStore.currentInstitutionId` — aktif bağlam varsa o,
 *   yoksa aktörün kendi (ev) kurumu.
 */
export function isActiveContextInstitution(
  viewedInstitutionId: string | null | undefined,
  activeContextInstitutionId: string | null | undefined,
): boolean {
  return !!viewedInstitutionId && viewedInstitutionId === activeContextInstitutionId
}
