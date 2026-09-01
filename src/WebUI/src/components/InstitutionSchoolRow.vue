<template>
  <q-item>
    <q-item-section avatar>
      <q-icon name="school" />
    </q-item-section>
    <q-item-section>
      <q-item-label>{{ school.fullName }}</q-item-label>
      <q-item-label
        v-if="school.institutionCode"
        caption
        class="tabular-nums"
      >
        Kurum Kodu: {{ school.institutionCode }}
      </q-item-label>
    </q-item-section>
    <q-item-section side>
      <div class="row items-center no-wrap q-gutter-xs">
        <q-btn
          flat
          dense
          no-caps
          color="primary"
          label="Bu kuruma geç"
          :loading="switching"
          @click="emit('switch', school.id, school.fullName)"
        />
        <q-btn
          flat
          round
          dense
          icon="visibility"
          aria-label="Kurum bilgilerini görüntüle"
          @click="emit('view', school.id)"
        >
          <q-tooltip>Kurum Bilgilerini Görüntüle</q-tooltip>
        </q-btn>
      </div>
    </q-item-section>
  </q-item>
</template>

<script setup lang="ts">
import type { InstitutionDto } from 'src/api/institution'

defineProps<{
  school: InstitutionDto
  switching: boolean
}>()

const emit = defineEmits<{
  switch: [institutionId: string, institutionName: string]
  view: [institutionId: string]
}>()
</script>
