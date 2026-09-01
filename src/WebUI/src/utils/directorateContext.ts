/**
 * Kullanıcı ŞU AN müdürlük (il/ilçe millî eğitim) olarak mı davranıyor?
 *
 * <p><b>`resolveIsUpperNode` ile karıştırmayın.</b> O fonksiyon "aktör üst düğüm mü" sorusuna
 * cevap verir ve `activeInstitutionId`'yi OR'lar — çünkü `Kurumlar` ağacı, il yetkilisi bir
 * okula geçtiğinde de görünmelidir (geri dönebilmeli). Bu fonksiyonun sorusu farklıdır: il
 * yetkilisi bir okula geçtiğinde <b>kiracısı o okuldur</b> ve okul panosunu görmelidir.</p>
 *
 * <p>`institutionStore.institution?.nodeType` aktif bağlama bağlıdır, dolayısıyla tek girdi
 * olarak doğru cevabı verir.</p>
 *
 * @param nodeType `InstitutionDto.nodeType` — `'Province'`, `'District'` ya da `'School'`.
 *   Kurum henüz yüklenmemişse `null`/`undefined` olabilir; o hâlde okul panosu gösterilir
 *   (güvenli varsayılan: müdürlük panosu okul kiracısında boş çıkardı, tersi çıkmaz).
 */
export function isActingAsDirectorate(nodeType: string | null | undefined): boolean {
  return nodeType === 'Province' || nodeType === 'District'
}

/**
 * Kullanıcı ŞU AN il müdürlüğü olarak mı davranıyor?
 *
 * <p><b>`isActingAsDirectorate` ile karıştırmayın.</b> O fonksiyon "müdürlük mü" (il VEYA
 * ilçe) sorusuna cevap verir. Bu fonksiyon daha DAR bir soruyu yanıtlar: bir düğüm kendi alt
 * ağacının İÇİNDEDİR, yani bir ilçe müdürlüğü "bana bağlı kaç ilçe var" diye sorduğunda
 * KENDİSİNİ sayar ve "İlçe: 1" görür — kendisini. İlçe müdürlüğünün altında zaten başka ilçe
 * olamaz (ağaç iki seviyelidir: il → ilçe → okul), o yüzden ilçe sayısı kartı yalnız İL
 * bağlamında anlamlıdır.</p>
 *
 * @param nodeType `InstitutionDto.nodeType` — `'Province'`, `'District'` ya da `'School'`.
 */
export function isActingAsProvince(nodeType: string | null | undefined): boolean {
  return nodeType === 'Province'
}
