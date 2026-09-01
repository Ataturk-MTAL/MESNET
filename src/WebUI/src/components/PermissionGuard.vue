<template>
  <template v-if="hasAccess">
    <slot />
  </template>
  <template v-else>
    <slot name="fallback">
      <!-- Varsayılan fallback içeriği -->
    </slot>
  </template>
</template>

<script setup lang="ts">
import { usePermissions } from 'src/utils/permissions';
import { computed } from 'vue';

interface Props {
  /**
   * Tek izin ya da izin LİSTESİ. Liste verilirse HERHANGİ BİRİ (any-of) yeterlidir —
   * sunucudaki `AnyOf` policy'lerinin (bkz. `PermissionPolicies.cs`) aynadaki karşılığı.
   *
   * Tek string davranışı DEĞİŞMEZ (geriye dönük uyumluluk — depoda tek-izinli çağıran
   * onlarca yer var). Guard'ı sunucu politikasından DAR tutmak, sunucunun kabul edeceği
   * bir aktörden eylemi arayüzde saklamak demektir — bkz. UserManagement kurum bağı bulgusu.
   */
  permission: string | string[];
}

const props = defineProps<Props>();
const { hasPermission, hasAnyPermission } = usePermissions();

const hasAccess = computed(() =>
  Array.isArray(props.permission)
    ? hasAnyPermission(props.permission)
    : hasPermission(props.permission),
);
</script>
