/**
 * İşletme kümesi renkleri — TEK KAYNAK (#110).
 *
 * Daha önce iki ayrı palet vardı (`useClusterMap.ts` ve `BusinessClusterMap.vue`);
 * aynı küme sayfa üstündeki özet çipinde kırmızı, haritada mavi görünüyordu. Palet
 * buraya alındı, küme özetini yalnız harita bileşeni basıyor.
 *
 * Bu renkler TEMAYA BAĞLANMAZ ve bilerek anlamsal tonlardan (#104) ayrı tutulur:
 * küme numarası bir DURUM değil, kategorik kimliktir. Tema altı role sahip, küme
 * sayısı ise veriye göre değişir — rollere çökertmek hem yeterli ayrım vermez hem
 * de "kırmızı küme = kötü" gibi olmayan bir anlam yükler.
 *
 * Sıralama, ilk kümelerin durum çağrıştıran tonlara denk gelmemesi için mavi/yeşil
 * ile başlar; kırmızı ancak 4. kümede devreye girer.
 */
const CLUSTER_PALETTE = [
  '#1E88E5', '#43A047', '#FB8C00', '#E53935', '#8E24AA',
  '#00ACC1', '#F4511E', '#6D4C41', '#00897B', '#C0CA33',
  '#3949AB', '#D81B60', '#039BE5', '#7CB342', '#FFB300',
  '#5E35B1', '#0097A7', '#2E7D32', '#BF360C', '#4527A0',
]

/** Kümeye girmeyen tekil noktalar (DBSCAN gürültüsü). */
export const NOISE_COLOR = '#9E9E9E'

/** Küme kimliğinin rengi. `null` = tekil nokta. */
export function clusterColor(clusterId: number | null): string {
  if (clusterId === null) return NOISE_COLOR
  return CLUSTER_PALETTE[clusterId % CLUSTER_PALETTE.length] ?? NOISE_COLOR
}
