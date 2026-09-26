<template>
  <div class="coordinator-teacher-field">
    <!-- Kaydı yazan öğretmen: oturumdaki kullanıcının öğretmen kaydı varsa sabittir.
         Yoksa (müdür, alan şefi) ve öğretmen listesini okuyabiliyorsa seçtirir. -->
    <q-field
      v-if="locked || me.teacher.value"
      :model-value="displayName"
      label="Koordinatör Öğretmen"
      outlined
      readonly
      stack-label
    >
      <template #prepend>
        <q-icon name="badge" />
      </template>
      <template #control>
        <div class="self-center full-width no-outline">
          {{ displayName ?? '—' }}
        </div>
      </template>
    </q-field>

    <q-select
      v-else-if="canPickTeacher"
      :model-value="modelValue"
      :options="teacherOpts.options.value"
      :loading="teacherOpts.loading.value"
      label="Koordinatör Öğretmen *"
      outlined
      use-input
      hide-selected
      fill-input
      input-debounce="0"
      emit-value
      map-options
      @filter="teacherOpts.filter"
      @update:model-value="emit('update:modelValue', $event)"
    >
      <template #prepend>
        <q-icon name="badge" />
      </template>
      <template #no-option>
        <SelectEmptyOption />
      </template>
    </q-select>

    <AppNotice
      v-else-if="me.loaded.value"
      type="warning"
      message="Bu hesabın aktif okulda öğretmen kaydı yok; kayıt öğretmen adına oluşturulamaz."
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useMyTeacher } from 'src/composables/useMyTeacher'
import { useTeacherOptions } from 'src/composables/useEntityOptions'
import { useAuthStore } from 'stores/auth'
import { Permissions } from 'utils/permissions'
import { useNotify } from 'src/composables/useNotify'
import AppNotice from 'components/AppNotice.vue'
import SelectEmptyOption from 'components/SelectEmptyOption.vue'

const props = withDefaults(defineProps<{
  modelValue: string | null
  /** Düzenlemede öğretmen değişmez (update komutu almaz) — yalnız adı gösterilir. */
  locked?: boolean
}>(), {
  locked: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string | null]
}>()

const authStore = useAuthStore()
const notify = useNotify()
const me = useMyTeacher()
const teacherOpts = useTeacherOptions()

// Öğretmen listesi `institution:view` ister (GET /api/teachers).
const canPickTeacher = computed(() => authStore.hasPermission(Permissions.Institution.View))

const displayName = computed(() => {
  if (me.teacher.value && me.teacher.value.id === props.modelValue) return me.teacher.value.fullName
  const picked = teacherOpts.allOptions.value.find((o) => o.value === props.modelValue)?.label
  if (picked) return picked
  // Öğretmen rolü listeyi okuyamaz (`institution:view` yok): kendisine ait olmayan kaydı
  // açtığında adı çözülemez. Boş tire yerine kaydın BAŞKASINA ait olduğu açıkça yazılır.
  if (props.modelValue && me.loaded.value && !canPickTeacher.value) return 'Başka bir öğretmen'
  return null
})

// Kendi kaydı bulunduğunda yeni kayıt onun adına açılır.
watch(() => me.teacher.value, (teacher) => {
  if (teacher && !props.locked && !props.modelValue) emit('update:modelValue', teacher.id)
})

onMounted(() => {
  me.load().catch((e: unknown) => notify.apiError(e, 'Öğretmen kaydı yüklenemedi.'))
  if (canPickTeacher.value) {
    teacherOpts.load({ institutionId: authStore.currentInstitutionId ?? undefined }).catch(() => {})
  }
})
</script>
