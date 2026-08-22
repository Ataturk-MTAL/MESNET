<template>
  <!-- Tek alana kilitli kullanıcı (yazma bağlamı): salt okunur chip -->
  <q-field
    v-if="isLockedToSingleBranch"
    :label="label"
    outlined
    :dense="dense"
    stack-label
  >
    <template #control>
      <div class="self-center full-width no-outline">
        <q-icon
          :name="icon"
          color="info"
          class="q-mr-sm"
        />
        {{ displayLabel }}
      </div>
    </template>
  </q-field>

  <!-- Seçilebilir alan dropdown'u -->
  <q-select
    v-else
    v-model="model"
    :options="visibleOptions"
    :loading="branchOpts.loading.value"
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
    @filter="branchOpts.filter"
  >
    <template #prepend>
      <q-icon :name="icon" />
    </template>
    <template #no-option>
      <SelectEmptyOption />
    </template>
  </q-select>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useBranchOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const props = withDefaults(defineProps<{
  label?: string
  icon?: string
  dense?: boolean
  /** true ise kilitli kullanıcıda bile q-select gösterir (filtre amaçlı) */
  forceSelect?: boolean
  /**
   * Seçim bir **yazma** işlemini hedefliyor mu? (#126)
   *
   * `true` ise kullanıcının yazma yetkisi olmayan alanlar listelenmez — yanlış alan
   * seçiliyken "Otomatik Dağıt + Kaydet" kazası da böyle engellenir. `false` (varsayılan)
   * salt görüntüleme/filtre bağlamıdır: alan şefi başka alanın verisini görebilir,
   * bu bilinçli bir karardır.
   */
  writeContext?: boolean
}>(), {
  label: 'Alan',
  icon: 'school',
  dense: false,
  forceSelect: false,
  writeContext: false,
})

const model = defineModel<string | null>({ default: null })

const authStore = useAuthStore()
const branchOpts = useBranchOptions()

/**
 * Yazma bağlamında kullanıcının kapsamı. `null` = kısıt yok (kurum geneli muafiyet).
 * Rol adına bakılmaz; karar permission + `branch_codes` claim'i ile verilir.
 */
const scopedCodes = computed<string[] | null>(() =>
  props.writeContext ? authStore.writableBranchCodes : null,
)

const visibleOptions = computed(() => {
  const scope = scopedCodes.value
  const options = branchOpts.options.value
  if (scope === null) return options
  return options.filter((o: { value: string }) => scope.includes(o.value))
})

/** Yazma bağlamında tek alana kilitli kullanıcıda seçici yerine salt okunur alan gösterilir. */
const isLockedToSingleBranch = computed(
  () => !props.forceSelect && scopedCodes.value?.length === 1,
)

const displayLabel = computed(() => {
  if (!model.value) return ''
  return branchOpts.allOptions?.value.find(
    (o: { value: string; label: string }) => o.value === model.value,
  )?.label ?? model.value
})

onMounted(async () => {
  await branchOpts.load()

  // Kapsamı tek alansa otomatik seç — kullanıcının seçecek başka alanı yok.
  const scope = scopedCodes.value
  if (scope?.length === 1 && !model.value) {
    model.value = scope[0] ?? null
  }
})

defineExpose({
  /** Branch composable'ına dışarıdan erişim (getSpecializations vb.) */
  branchOpts,
})
</script>
