/**
 * Kurum sayfasının "Düzenle" ve "geri dön" rotalarını üretir.
 *
 * <p><b>Neden saf fonksiyon:</b> bu karar iki bileşene dağılmıştı ve ikisi de rota
 * parametresini görmezden geliyordu. `InstitutionPage` sabit `/institution/edit`'e,
 * `InstitutionFormPage` kaydettikten sonra sabit `/institution`'a gidiyordu.</p>
 *
 * <p><b>Ölçüldü (30.08.2026, tarayıcı):</b> il yetkilisi `Kurumlar` ağacından
 * `/institutions/22df21ed-…` (Mersin İl Millî Eğitim Müdürlüğü) sayfasını açıp
 * <b>Düzenle</b>'ye bastığında id'siz `/institution/edit`'e gidiliyordu. O rotada rota
 * parametresi olmadığı için `resolveEditableInstitutionId` ikinci girdiye — DAVRANILAN
 * kuruma — düşüyor ve form <b>Atatürk Mesleki ve Teknik Anadolu Lisesi</b>'ni açıyordu.
 * Kullanıcı müdürlüğü düzenlediğini sanırken başka bir kurumu düzenliyordu; yazma
 * kaybolmaz, <b>yanlış kuruma</b> gider. `84200f2`'de kapatılan hatanın aynı sınıfı —
 * o sefer kaynak sırasız listeydi, bu sefer düşürülen rota parametresi.</p>
 *
 * @param viewedRouteId `/institutions/:id` rota parametresi; menüden gelen "Kurum Bilgileri"
 *   sayfasında yoktur (`null`).
 */
export function institutionEditRoute(viewedRouteId: string | null | undefined): string {
  return viewedRouteId ? `/institutions/${viewedRouteId}/edit` : '/institution/edit'
}

/**
 * Formdan çıkışta dönülecek rota. Ağaçtan gelen kullanıcı ağaçtaki kuruma döner, kendi
 * kurumunu düzenleyen menü sayfasına. Aksi hâlde il yetkilisi kaydettiği müdürlük yerine
 * davrandığı okulun sayfasında bulurdu kendini.
 *
 * @param viewedRouteId `/institutions/:id[/edit]` rota parametresi; yoksa `null`.
 */
export function institutionReturnRoute(viewedRouteId: string | null | undefined): string {
  return viewedRouteId ? `/institutions/${viewedRouteId}` : '/institution'
}
