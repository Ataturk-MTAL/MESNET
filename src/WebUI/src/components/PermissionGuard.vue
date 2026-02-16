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
  permission: string;
}

const props = defineProps<Props>();
const { hasPermission } = usePermissions();

const hasAccess = computed(() => hasPermission(props.permission));
</script>
