<template>
  <q-page padding>
    <div class="text-h5 text-weight-bold q-mb-lg">Rol Yetkileri</div>

    <div v-if="loading" class="flex flex-center q-pa-xl">
      <q-spinner-gears size="48px" color="primary" />
    </div>

    <div v-else class="row q-col-gutter-md">
      <div v-for="role in roles" :key="role.roleName" class="col-12 col-md-6 col-lg-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 text-weight-medium q-mb-sm">{{ role.roleName }}</div>
            <q-chip
              v-for="perm in role.permissions"
              :key="perm"
              dense
              color="blue-grey-1"
              text-color="blue-grey-8"
              class="q-ma-xs"
              size="sm"
            >
              {{ perm }}
            </q-chip>
            <div v-if="role.permissions.length === 0" class="text-caption text-grey">
              Bu rol için izin tanımlı değil.
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { securityApi, type RolePermissionsDto } from 'src/api/security'
import { useNotify } from 'src/composables/useNotify'

const notify = useNotify()
const loading = ref(false)
const roles = ref<RolePermissionsDto[]>([])

async function load() {
  loading.value = true
  try {
    const res = await securityApi.listRoles()
    roles.value = res.data ?? []
  } catch (e) {
    notify.apiError(e, 'Roller yüklenirken bir hata oluştu.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
