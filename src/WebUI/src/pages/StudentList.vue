<template>
  <q-page padding>
    <h5 class="q-mt-none">Öğrenci Listesi</h5>

    <permission-guard :permission="Permissions.Student.View">
      <q-table
        :rows="students"
        :columns="columns"
        row-key="id"
        v-model:pagination="pagination"
      />

      <permission-guard :permission="Permissions.Student.Manage">
        <q-btn 
          color="primary"
          icon="add"
          label="Yeni Öğrenci"
          @click="onNewStudent"
        />
        <template #fallback>
          <q-banner class="bg-warning text-white">
            Öğrenci ekleme yetkiniz bulunmamaktadır.
          </q-banner>
        </template>
      </permission-guard>
    </permission-guard>
  </q-page>
</template>

<script setup lang="ts">
import PermissionGuard from 'components/PermissionGuard.vue';
import { Permissions } from 'src/utils/permissions';
import { ref } from 'vue';

const pagination = ref({
  rowsPerPage: 10,
  page: 1
});

const onNewStudent = () => {
  // Yeni öğrenci ekleme işlemleri
};
</script>
