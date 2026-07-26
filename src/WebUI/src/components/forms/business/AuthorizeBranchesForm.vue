<template>
  <FormDialog
    v-model="open"
    title="Alan Yetkileri"
    icon="verified"
    color="primary"
    width="460px"
    save-label="Kaydet"
    :saving="saving"
    @save="handleSave"
  >
    <div class="text-caption text-grey q-mb-sm">
      İşletmenin hangi alanlardan öğrenci alabileceğini belge dayanağıyla işaretleyin.
      İşaretlenmeyen alanların yetkisi kaldırılır; mevcut yerleştirmeler bundan etkilenmez,
      yalnız yeni yerleştirme engellenir.
    </div>

    <q-inner-loading :showing="branchOpts.loading.value" />

    <div
      v-if="!branchOpts.loading.value && branchOpts.allOptions.value.length === 0"
      class="text-caption text-grey q-my-md"
    >
      Kurumda açık alan bulunmuyor. Önce Kurum ayarlarından alan aktifleştirilmelidir.
    </div>

    <q-list
      v-else
      separator
    >
      <q-item
        v-for="branch in branchOpts.allOptions.value"
        :key="branch.value"
      >
        <q-item-section side>
          <q-checkbox
            :model-value="isChecked(branch.value)"
            :aria-label="`${branch.label} alanından öğrenci alabilir`"
            @update:model-value="(v: boolean) => toggle(branch.value, v)"
          />
        </q-item-section>
        <q-item-section>
          <q-item-label>{{ branch.label }}</q-item-label>
          <q-select
            v-if="isChecked(branch.value)"
            :model-value="documentOf(branch.value)"
            :options="documentOptions"
            label="Dayanak belge (opsiyonel)"
            outlined
            dense
            clearable
            emit-value
            map-options
            class="q-mt-xs"
            @update:model-value="(v: string | null) => setDocument(branch.value, v)"
          />
          <q-item-label
            v-else-if="revokedAt(branch.value)"
            caption
            class="text-warning"
          >
            Yetki kaldırıldı: {{ formatDate(revokedAt(branch.value)) }}
          </q-item-label>
        </q-item-section>
      </q-item>
    </q-list>
  </FormDialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { businessApi, type BusinessDto } from 'src/api/business'
import { useNotify } from 'src/composables/useNotify'
import { useBranchOptions } from 'src/composables/useEntityOptions'
import FormDialog from 'components/FormDialog.vue'

const open = defineModel<boolean>({ required: true })

const props = defineProps<{
  business: BusinessDto | null
}>()

const emit = defineEmits<{ saved: [] }>()

const notify = useNotify()
const saving = ref(false)
const branchOpts = useBranchOptions()

/** Alan kodu → dayanak belge kimliği (null = belge seçilmedi). İşaretli alanlar bu haritada durur. */
const selected = reactive(new Map<string, string | null>())

const documentOptions = computed(() =>
  (props.business?.documents ?? []).map((d) => ({
    label: `${d.typeSlug} — ${d.fileName}`,
    value: d.id,
  })),
)

watch(open, (isOpen) => {
  if (!isOpen) return
  selected.clear()
  for (const authorization of props.business?.authorizedBranches ?? []) {
    if (!authorization.isActive) continue
    selected.set(authorization.branchCode, authorization.basedOnDocumentId)
  }
  branchOpts.load().catch(() => {})
})

function isChecked(branchCode: string): boolean {
  return selected.has(branchCode)
}

function toggle(branchCode: string, checked: boolean) {
  if (checked) selected.set(branchCode, documentOf(branchCode))
  else selected.delete(branchCode)
}

function documentOf(branchCode: string): string | null {
  return selected.get(branchCode) ?? null
}

function setDocument(branchCode: string, documentId: string | null) {
  selected.set(branchCode, documentId)
}

function revokedAt(branchCode: string): string | null {
  const revoked = (props.business?.authorizedBranches ?? [])
    .filter((a) => a.branchCode === branchCode && !a.isActive)
    .map((a) => a.revokedAt)
    .filter((d): d is string => !!d)
    .sort()
  return revoked.at(-1) ?? null
}

function formatDate(value: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleDateString('tr-TR')
}

async function handleSave() {
  if (!props.business) return
  saving.value = true
  try {
    await businessApi.authorizeBranches(props.business.id, {
      branches: [...selected.entries()].map(([branchCode, basedOnDocumentId]) => ({
        branchCode,
        basedOnDocumentId,
      })),
    })
    notify.success('Alan yetkileri güncellendi.')
    open.value = false
    emit('saved')
  } catch (e) {
    notify.apiError(e, 'Alan yetkileri güncellenirken bir hata oluştu.')
  } finally {
    saving.value = false
  }
}
</script>
