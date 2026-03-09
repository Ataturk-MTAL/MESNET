<template>
  <q-select
    v-model="model"
    :options="filteredOptions"
    :loading="teacherOpts.loading.value"
    :label="label"
    filled
    :dense="dense"
    use-input
    input-debounce="0"
    emit-value
    map-options
    option-label="label"
    option-value="value"
    clearable
    @filter="onFilter"
  >
    <template #prepend>
      <q-icon :name="icon" />
    </template>

    <!-- Cross-branch öğretmen gösterimi -->
    <template #option="scope">
      <q-item v-bind="scope.itemProps">
        <q-item-section>
          <q-item-label>{{ scope.opt.label }}</q-item-label>
          <q-item-label
            v-if="showCrossBranch && branchCode && scope.opt.branchCode !== branchCode"
            caption
            class="text-orange-8"
          >
            Farklı alan: {{ scope.opt.branchCode }}
          </q-item-label>
        </q-item-section>
        <q-item-section
          v-if="showCrossBranch && branchCode && scope.opt.branchCode !== branchCode"
          side
        >
          <q-badge color="orange" label="Farklı alan" />
        </q-item-section>
      </q-item>
    </template>

    <template #no-option>
      <q-item>
        <q-item-section class="text-grey">Sonuç bulunamadı</q-item-section>
      </q-item>
    </template>
  </q-select>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'

const props = withDefaults(defineProps<{
  label?: string
  icon?: string
  dense?: boolean
  /** Aktif branch filtresi — öğretmen listesi ve cross-branch sıralama için */
  branchCode?: string | null
  /** true ise cross-branch badge gösterir (yöneticiler için) */
  showCrossBranch?: boolean
}>(), {
  label: 'Öğretmen',
  icon: 'person',
  dense: false,
  branchCode: null,
  showCrossBranch: false,
})

const model = defineModel<string | null>({ default: null })

const authStore = useAuthStore()
const teacherOpts = useTeacherOptions()

const filterNeedle = ref('')

const filteredOptions = computed(() => {
  const needle = filterNeedle.value.toLowerCase()
  let opts = [...teacherOpts.allOptions.value]
  if (needle) {
    opts = opts.filter((o) => o.label.toLowerCase().includes(needle))
  }
  // Cross-branch sıralama: kendi alan öğretmenleri üstte
  if (props.branchCode && props.showCrossBranch) {
    opts.sort((a, b) => {
      const aOwn = a.branchCode === props.branchCode ? 0 : 1
      const bOwn = b.branchCode === props.branchCode ? 0 : 1
      return aOwn !== bOwn ? aOwn - bOwn : a.label.localeCompare(b.label, 'tr')
    })
  }
  return opts
})

function onFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    filterNeedle.value = val
  })
}

// Branch değiştiğinde öğretmen listesini yeniden yükle
watch(() => props.branchCode, async (newBranch) => {
  const instId = authStore.user?.institutionId ?? undefined
  if (newBranch) {
    await teacherOpts.reload({ institutionId: instId, branchCode: newBranch })
  } else {
    await teacherOpts.reload({ institutionId: instId })
  }
})

onMounted(async () => {
  const instId = authStore.user?.institutionId ?? undefined
  if (props.branchCode) {
    await teacherOpts.reload({ institutionId: instId, branchCode: props.branchCode })
  } else {
    await teacherOpts.load({ institutionId: instId })
  }
})

defineExpose({
  teacherOpts,
})
</script>
