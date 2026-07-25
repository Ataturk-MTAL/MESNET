<template>
  <q-select
    v-model="model"
    :options="filteredOptions"
    :loading="teacherOpts.loading.value"
    :label="label"
    outlined
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
            v-if="isCrossBranch(scope.opt)"
            caption
            class="text-orange-8"
          >
            Alan: {{ scope.opt.branchCode }}
          </q-item-label>
        </q-item-section>
        <q-item-section
          v-if="isCrossBranch(scope.opt)"
          side
        >
          <q-badge
            color="orange"
            label="Alan dışı"
          />
        </q-item-section>
      </q-item>
    </template>

    <template #no-option>
      <SelectEmptyOption :text="branchCode ? 'Bu alanda öğretmen yok' : 'Önce alan seçin'" />
    </template>
  </q-select>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

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
  } else if (props.branchCode && !props.showCrossBranch) {
    // Arama yokken sadece alan öğretmenlerini göster
    opts = opts.filter((o) => o.branchCode === props.branchCode)
  }
  // Sıralama: kendi alan öğretmenleri üstte
  if (props.branchCode) {
    opts.sort((a, b) => {
      const aOwn = a.branchCode === props.branchCode ? 0 : 1
      const bOwn = b.branchCode === props.branchCode ? 0 : 1
      return aOwn !== bOwn ? aOwn - bOwn : a.label.localeCompare(b.label, 'tr')
    })
  }
  return opts
})

function isCrossBranch(opt: { branchCode?: string | null }) {
  return !!props.branchCode && opt.branchCode !== props.branchCode
}

function onFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    filterNeedle.value = val
  })
}

// Tüm öğretmenleri yükle — filtreleme client-side yapılır
onMounted(async () => {
  const instId = authStore.user?.institutionId ?? undefined
  await teacherOpts.load({ institutionId: instId })
})

defineExpose({
  teacherOpts,
})
</script>
