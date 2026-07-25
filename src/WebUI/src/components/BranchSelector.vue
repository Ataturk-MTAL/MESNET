<template>
  <!-- Alan Şefi: salt okunur chip -->
  <q-field
    v-if="authStore.isDepartmentHead && !forceSelect"
    :label="label"
    outlined
    :dense="dense"
    stack-label
  >
    <template #control>
      <div class="self-center full-width no-outline">
        <q-icon
          :name="icon"
          color="blue-7"
          class="q-mr-sm"
        />
        {{ displayLabel }}
      </div>
    </template>
  </q-field>

  <!-- Yöneticiler: seçilebilir alan dropdown -->
  <q-select
    v-else
    v-model="model"
    :options="branchOpts.options.value"
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

withDefaults(defineProps<{
  label?: string
  icon?: string
  dense?: boolean
  /** true ise isDepartmentHead olsa bile q-select gösterir (filtre amaçlı) */
  forceSelect?: boolean
}>(), {
  label: 'Alan',
  icon: 'school',
  dense: false,
  forceSelect: false,
})

const model = defineModel<string | null>({ default: null })

const authStore = useAuthStore()
const branchOpts = useBranchOptions()

const displayLabel = computed(() => {
  if (!model.value) return ''
  return branchOpts.allOptions?.value.find(
    (o: { value: string; label: string }) => o.value === model.value,
  )?.label ?? model.value
})

onMounted(async () => {
  await branchOpts.load()

  // Alan Şefi ise kendi branşını otomatik seç
  if (authStore.isDepartmentHead && authStore.user?.branchCode && !model.value) {
    model.value = authStore.user.branchCode
  }
})

defineExpose({
  /** Branch composable'ına dışarıdan erişim (getSpecializations vb.) */
  branchOpts,
})
</script>
