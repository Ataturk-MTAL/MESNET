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
