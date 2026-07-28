/**
 * Oturum sona erdi ekranı (#136) — son çare.
 *
 * <p><b>Neden Vue bileşeni değil düz DOM:</b> bu ekranın görevi tam olarak Vue uygulaması
 * mount EDİLEMEDİĞİNDE görünmektir. <c>main.ts</c> mount'u <c>bootAuth()</c>'a bağlar;
 * boot yeniden giriş döngüsünde takılırsa hiçbir bileşen render edilmez. Bir Vue bileşenine
 * bağlı çözüm, çözmesi gereken durumda çalışmazdı.</p>
 *
 * <p><b>Neden düğme çıkış yapıyor, giriş değil:</b> #136'daki döngüyü besleyen şey
 * Keycloak'ın sunucu tarafındaki oturumuydu — SSO çerezi hâlâ kimlik doğruluyor ama
 * üretilen token kabul edilmiyordu. <c>login()</c> o çereze geri döner ve döngüye yeniden
 * girer; oturumu yok eden tek işlem <c>logout()</c>'tur. "Sekmeyi kapatmak" dışında çıkış
 * olmamasının sebebi de buydu: hiçbir yol çıkış çağırmıyordu.</p>
 */

const CONTAINER_ID = 'app'

export interface SessionExpiredScreenOptions {
  /** Düğmeye basınca çalışacak eylem — Keycloak oturumunu sonlandırmalıdır. */
  onLogout: () => void
  /** Ek açıklama; teşhis için (ör. "yeniden giriş 3 kez denendi"). */
  detail?: string
}

/**
 * Ekranı <c>#app</c> içine basar. Uygulama mount edilmiş olsa bile üzerine yazar —
 * bu bilinçlidir: bu noktada uygulamanın gösterdiği hiçbir şey güvenilir değildir.
 */
export function showSessionExpiredScreen(options: SessionExpiredScreenOptions): void {
  const container = document.getElementById(CONTAINER_ID)
  if (!container) return

  container.innerHTML = ''

  const wrapper = document.createElement('div')
  wrapper.setAttribute('role', 'alert')
  wrapper.style.cssText = [
    'display:flex',
    'flex-direction:column',
    'align-items:center',
    'justify-content:center',
    'gap:1rem',
    'min-height:100vh',
    'padding:2rem',
    'text-align:center',
    'font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif',
    'color:#1d1d1d',
    'background:#fafafa',
  ].join(';')

  const heading = document.createElement('h1')
  heading.textContent = 'Oturumunuz sona erdi'
  heading.style.cssText = 'margin:0;font-size:1.5rem;font-weight:600'

  const message = document.createElement('p')
  message.textContent =
    'Uzun süre işlem yapılmadığı için güvenlik gereği oturumunuz kapatıldı. ' +
    'Kaldığınız yerden devam etmek için yeniden giriş yapın.'
  message.style.cssText = 'margin:0;max-width:32rem;line-height:1.6;color:#5a5a5a'

  const button = document.createElement('button')
  button.type = 'button'
  button.textContent = 'Yeniden giriş yap'
  button.style.cssText = [
    'margin-top:0.5rem',
    'padding:0.75rem 1.5rem',
    'font-size:1rem',
    'font-weight:600',
    'color:#fff',
    'background:#1976d2',
    'border:none',
    'border-radius:4px',
    'cursor:pointer',
  ].join(';')
  button.addEventListener('click', () => {
    button.disabled = true
    button.textContent = 'Yönlendiriliyor…'
    options.onLogout()
  })

  wrapper.append(heading, message, button)

  if (options.detail) {
    const detail = document.createElement('p')
    detail.textContent = options.detail
    detail.style.cssText = 'margin:0;font-size:0.8125rem;color:#8a8a8a'
    wrapper.append(detail)
  }

  container.append(wrapper)
}
