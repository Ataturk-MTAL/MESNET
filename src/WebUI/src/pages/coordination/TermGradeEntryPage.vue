<template>
  <q-page padding>
    <PageHeader title="Dönem Notu Girişi">
      <q-btn
        flat
        round
        dense
        icon="refresh"
        aria-label="Yenile"
        @click="load"
      >
        <q-tooltip>Yenile</q-tooltip>
      </q-btn>
    </PageHeader>

    <!-- İki ayrı akış: işletmede staj notunu işletme girer, okulda staj notunu okul (#171).
         Sekme yalnız kullanıcının izni olduğu akış için görünür; müdürde ikisi de vardır. -->
    <q-tabs
      v-if="showModeTabs"
      v-model="mode"
      dense
      align="left"
      class="text-grey-7 q-mb-md"
      active-color="primary"
      indicator-color="primary"
      narrow-indicator
      @update:model-value="load"
    >
      <q-tab
        name="business"
        icon="business"
        label="İşletmede Staj"
      />
      <q-tab
        name="school"
        icon="school"
        label="Okulda Staj"
      />
    </q-tabs>

    <AppNotice
      v-if="!activePeriod"
      type="info"
      message="Aktif akademik dönem bulunamadı."
    />

    <template v-else>
      <AppNotice
        :type="isWindowOpen ? 'info' : 'warning'"
        :message="windowMessage"
        class="q-mb-md"
      />

      <div
        v-if="!loading && rows.length === 0"
        class="text-center q-pa-xl text-grey-7"
      >
        <q-icon
          name="groups"
          size="48px"
          class="q-mb-sm"
        />
        <div>{{ emptyMessage }}</div>
      </div>

      <AppTable
        v-else
        :rows="rows"
        :columns="columns"
        :loading="loading"
      >
        <template #body-cell-status="{ row }">
          <q-td>
            <StatusBadge
              v-if="row.status"
              :slug="row.statusSlug ?? row.status"
            />
            <span
              v-else
              class="text-grey-7"
            >Girilmedi</span>
            <!--
              Burada "Sıra sizde" ROZETİ YOK — bilerek. Bu sayfada sıra bilgisi durum
              sütununun birebir tekrarıdır: sıra bizde olan satır tam olarak "Girilmedi" ya
              da "Taslak" olan satırdır, sıra bizde olmayan satır "Gönderildi"/"Kesinleşti"
              olandır. Üstelik pencere açıldığı an hiçbir notun girilmemiş olması normaldir
              (sunucu `g == null` → `status: null` döndürür, StudentTermGradeQueryHandler.
              ToRow), yani tablonun %100'ü yanardı ve rozet hiçbir şeyi ayırt etmezdi.
              Sinyal sayfa düzeyine taşındı: pencere bildirimi (`windowMessage`) bekleyen
              öğrenci sayısını tek cümleyle söyler.
            -->
          </q-td>
        </template>
        <template #body-cell-average="{ row }">
          <q-td>{{ row.termAverage ?? '—' }}</q-td>
        </template>
        <template #body-cell-actions="{ row }">
          <q-td class="text-right">
            <q-btn
              flat
              dense
              size="sm"
              icon="edit_note"
              label="Not Gir"
              color="primary"
              :disable="!isWindowOpen || row.status === 'Submitted'"
              @click="openEntry(row)"
            >
              <q-tooltip>{{ row.status === 'Submitted' ? 'Gönderilmiş not düzenlenemez' : 'Notları gir/düzenle' }}</q-tooltip>
            </q-btn>
            <q-btn
              v-if="row.gradeId && row.status === 'Draft'"
              flat
              dense
              size="sm"
              icon="send"
              label="Gönder"
              color="positive"
              :disable="!isWindowOpen"
              @click="confirmSubmit(row)"
            >
              <q-tooltip>Notu kesin gönder (sonra düzenlenemez)</q-tooltip>
            </q-btn>
          </q-td>
        </template>
      </AppTable>
    </template>

    <FormDialog
      v-model="entryDialog"
      title="Dönem Notu Gir"
      icon="edit_note"
      color="primary"
      save-label="Kaydet (Taslak)"
      :saving="saving"
      @save="handleSave"
    >
      <div class="text-subtitle2">
        {{ entryTarget?.studentName }}
      </div>
      <div class="text-caption text-grey-7 q-mb-sm">
        {{ entryTarget?.branchName }}
      </div>
      <q-input
        v-model="form.practice"
        label="Temrin notları"
        hint="Virgülle ayır: 85, 90, 88"
        outlined
      />
      <q-input
        v-model="form.service"
        label="İş-Hizmet notları"
        hint="Virgülle ayır"
        outlined
      />
      <q-input
        v-model="form.project"
        label="Proje notları"
        hint="Virgülle ayır"
        outlined
      />
      <q-input
        v-model="form.experiment"
        label="Deney notları"
        hint="Virgülle ayır"
        outlined
      />
      <!-- Usta öğretici işletme tarafının kavramıdır; okulda staj notunda alan yoktur. -->
      <q-input
        v-if="mode === 'business'"
        v-model="form.masterInstructor"
        label="Usta Öğretici Adı"
        outlined
      />
      <AppNotice
        v-else
        type="info"
        message="Okulda staj için Dönem Not Fişi (MEB Form 8) üretilmez; not yalnız başarı değerlendirmesi için kaydedilir."
      />
    </FormDialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import type { QTableProps } from 'quasar'
import { useQuasar } from 'quasar'
import { coordinationApi, type StudentGradeRow } from 'src/api/coordination'
import { useAcademicPeriodStore } from 'stores/academicPeriod'
import { useNotify } from 'src/composables/useNotify'
import AppTable from 'components/AppTable.vue'
import FormDialog from 'components/FormDialog.vue'
import AppNotice from 'components/AppNotice.vue'
import PageHeader from 'components/PageHeader.vue'
import StatusBadge from 'components/StatusBadge.vue'
import { Permissions } from 'utils/permissions'
import { useAuthStore } from 'stores/auth'

const $q = useQuasar()
const periodStore = useAcademicPeriodStore()
const notify = useNotify()
const authStore = useAuthStore()

// İki ayrı akış (#171): işletmede staj notunu işletme girer (`company:grade:enter`),
// okulda staj notunu okul girer (`institution:school-grade:enter`). Görünürlük rol adına
// değil izne bakar (ADR-0001).
type GradeMode = 'business' | 'school'

const canEnterBusiness = computed(() => authStore.hasPermission(Permissions.Company.EnterGrade))
const canEnterSchool = computed(() => authStore.hasPermission(Permissions.Institution.SchoolGradeEnter))
const showModeTabs = computed(() => canEnterBusiness.value && canEnterSchool.value)

const mode = ref<GradeMode>(canEnterBusiness.value ? 'business' : 'school')

const emptyMessage = computed(() =>
  mode.value === 'business'
    ? 'Bu dönemde işletmenize yerleştirilmiş öğrenci bulunamadı.'
    : 'Bu dönemde okulda staj yapan öğrenci bulunamadı.',
)

const rows = ref<StudentGradeRow[]>([])
const loading = ref(false)
const saving = ref(false)
const entryDialog = ref(false)
const entryTarget = ref<StudentGradeRow | null>(null)

const form = reactive({ practice: '', service: '', project: '', experiment: '', masterInstructor: '' })

const activePeriod = computed(() => periodStore.activePeriod)

const isWindowOpen = computed(() => {
  const p = activePeriod.value
  if (!p?.gradeEntryStartDate || !p?.gradeEntryEndDate) return false
  const today = new Date().toISOString().slice(0, 10)
  return today >= p.gradeEntryStartDate && today <= p.gradeEntryEndDate
})

/**
 * "SIRA SİZDE" — Tek Ses Kuralı: bu ekranda sinyal TEK yüzeyde görünür, o da pencere
 * bildirimidir. SATIR ROZETİ YOK: bu sayfada "sıra bizde" durum sütununun birebir tekrarıdır
 * (Girilmedi/Taslak ⇒ sıra bizde, Gönderildi/Kesinleşti ⇒ değil) ve pencere açıldığı an
 * hiçbir not girilmemiş olduğu için tablonun tamamı yanardı — ayırt etmeyen sinyal sinyal
 * değildir. `isMyTurn` bu yüzden yalnız SAYMAK için kullanılır.
 *
 * Koşul VERİDEN türer, rol adından DEĞİL (ADR-0001) — üç girdinin üçü de sayfada zaten var:
 *  • `isWindowOpen` — not giriş penceresi açık değilse sinyal HİÇ doğmaz. Pencere kapalıyken
 *    "Not Gir" / "Gönder" butonları `:disable` olur; yapılamayan iş vaat edilmez. Bu sayfanın
 *    salt-okunur kapısı budur: sayfa yalnız `activePeriod` (status === 'Active') üzerinde
 *    çalışır, yani `periodStore.isReadOnly` (seçili dönem 'Closed') buraya hiç uygulanmaz —
 *    o kontrol drawer'da seçili BAŞKA bir dönemi anlatır ve sinyali yanlış yerden bastırırdı.
 *  • Durum BEYAZ LİSTEDİR: yalnız boş ("Girilmedi") ve `'Draft'` sayılır. Kara liste
 *    (`!== 'Submitted'`) yanlıştı: `StudentTermGradeStatus`'ta üçüncü bir TERMİNAL durum var —
 *    `Finalized` / "Kesinleşti" (Coordination.Core/Enums/StudentTermGradeStatus.cs:13) ve
 *    backend Draft dışındaki hiçbir kaydın düzenlenmesine izin vermez
 *    (StudentTermGradeHandler.cs:38, SchoolTermGradeHandler.cs:55 → aksi hâlde fırlatır).
 *    Bugün erişilemez bir hâl: depoda `Finalized` ATAYAN tek satır yok, yalnız enum tanımı
 *    ve StatusBadge eşlemesi var — yani bu düzeltme gizli borcu kapatır, görünür bir davranışı
 *    değiştirmez. (Aynı düzeltme "Not Gir" butonunun `:disable` ifadesine de gerekiyor;
 *    o buton bu partinin değişikliği değil, bilerek DEĞİŞTİRİLMEDİ.)
 *  • Aktif sekmenin izni (`canEnterCurrentMode`) — kaydı İLERLETEN ucun istediği izin:
 *    `POST /api/coordination/term-grades` ve `.../{id}/submit` → `Company.EnterGrade`,
 *    okul karşılıkları → `Institution.SchoolGradeEnter` (StudentTermGradeEndpoints.cs:21-22,
 *    33-37). Koşul MODE'a bağlıdır: aksi hâlde okul sekmesindeki satırlar işletme yetkisiyle
 *    sayılırdı.
 */
const canEnterCurrentMode = computed(() =>
  mode.value === 'business' ? canEnterBusiness.value : canEnterSchool.value,
)

function isMyTurn(row: StudentGradeRow): boolean {
  // `!row.status` — şablonun kendi "Girilmedi" dalıyla aynı ölçüt (`v-if="row.status"`),
  // yani sayaç ile ekrandaki etiket asla ayrışmaz.
  const editable = !row.status || row.status === 'Draft'
  return isWindowOpen.value && editable && canEnterCurrentMode.value
}

/** Sıra bizde olan satırlar — sayfa düzeyindeki tek cümlenin sayacı. */
const turnRows = computed(() => rows.value.filter(isMyTurn))

const windowMessage = computed(() => {
  const p = activePeriod.value
  if (!p?.gradeEntryStartDate || !p?.gradeEntryEndDate)
    return 'Not giriş penceresi henüz açılmadı. Okul/kurum müdürlüğünün belirleyeceği tarihleri bekleyin.'
  const range = `${formatDate(p.gradeEntryStartDate)} – ${formatDate(p.gradeEntryEndDate)}`
  if (!isWindowOpen.value) return `Not giriş penceresi kapalı (${range}).`
  // "Sıra sizde" sinyali TEK yüzeyde ve tek cümlede: satır rozeti bu sayfada ayırt etmiyordu
  // (bkz. #body-cell-status yorumu). Sayı 0 ise cümle hiç kurulmaz — bekleyen iş yoksa
  // sinyal de yoktur.
  const pending = turnRows.value.length
  return pending > 0
    ? `Not giriş penceresi açık: ${range} — sıra sizde: ${pending} öğrencinin notu bekliyor.`
    : `Not giriş penceresi açık: ${range}`
})

const columns: QTableProps['columns'] = [
  { name: 'studentName', label: 'Öğrenci', field: 'studentName', align: 'left', sortable: true },
  { name: 'branchName', label: 'Alan/Dal', field: 'branchName', align: 'left' },
  { name: 'status', label: 'Durum', field: 'status', align: 'left' },
  { name: 'average', label: 'Ortalama', field: 'termAverage', align: 'left' },
  { name: 'actions', label: '', field: 'studentId', align: 'right' },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR')
}

function parseGrades(s: string): number[] {
  return s
    .split(/[,\s]+/)
    .map((x) => Number(x.trim()))
    .filter((n) => !Number.isNaN(n) && n >= 0 && n <= 100)
}

async function load() {
  const periodId = activePeriod.value?.id
  if (!periodId) return
  loading.value = true
  try {
    const { data } =
      mode.value === 'business'
        ? await coordinationApi.getMyStudentsForGrading(periodId)
        : await coordinationApi.getSchoolStudentsForGrading(periodId)
    rows.value = data?.students ?? []
  } catch (e) {
    notify.apiError(e, 'Öğrenci listesi yüklenemedi.')
  } finally {
    loading.value = false
  }
}

function openEntry(row: StudentGradeRow) {
  entryTarget.value = row
  form.practice = row.practiceGrades.join(', ')
  form.service = row.serviceGrades.join(', ')
  form.project = row.projectGrades.join(', ')
  form.experiment = row.experimentGrades.join(', ')
  form.masterInstructor = row.masterInstructorName ?? ''
  entryDialog.value = true
}

async function handleSave() {
  const periodId = activePeriod.value?.id
  if (!entryTarget.value || !periodId) return
  saving.value = true
  try {
    const grades = {
      studentId: entryTarget.value.studentId,
      academicPeriodId: periodId,
      practiceGrades: parseGrades(form.practice),
      serviceGrades: parseGrades(form.service),
      projectGrades: parseGrades(form.project),
      experimentGrades: parseGrades(form.experiment),
    }

    if (mode.value === 'business') {
      await coordinationApi.enterTermGrade({
        ...grades,
        masterInstructorName: form.masterInstructor || null,
      })
    } else {
      await coordinationApi.enterSchoolTermGrade(grades)
    }
    notify.success('Not taslağı kaydedildi.')
    entryDialog.value = false
    await load()
  } catch (e) {
    notify.apiError(e, 'Not kaydedilemedi.')
  } finally {
    saving.value = false
  }
}

function confirmSubmit(row: StudentGradeRow) {
  $q.dialog({
    title: 'Notu Gönder',
    message: `${row.studentName} için notları kesin göndermek istiyor musunuz? Gönderilen notlar düzenlenemez.`,
    cancel: { label: 'Vazgeç', flat: true },
    ok: { label: 'Gönder', color: 'positive', unelevated: true },
  }).onOk(() => {
    doSubmit(row).catch(() => {})
  })
}

async function doSubmit(row: StudentGradeRow) {
  if (!row.gradeId) return
  try {
    if (mode.value === 'business') {
      await coordinationApi.submitTermGrade(row.gradeId)
    } else {
      await coordinationApi.submitSchoolTermGrade(row.gradeId)
    }
    notify.success('Not gönderildi.')
    await load()
  } catch (e) {
    notify.apiError(e, 'Not gönderilemedi.')
  }
}

onMounted(async () => {
  if (!periodStore.isLoaded) await periodStore.loadPeriods()
  await load()
})
</script>
