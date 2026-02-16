import { useAuthStore } from 'stores/auth';

export const Permissions = {
  Tenant: {
    View: 'tenant:view',
    Manage: 'tenant:manage',
    Configure: 'tenant:configure',
  },
  Student: {
    View: 'student:view',
    Manage: 'student:manage',
    ViewOwn: 'student:view-own',
  },
  // ...diğer izinler
} as const;

export function usePermissions() {
  const authStore = useAuthStore();

  const hasPermission = (permission: string) => {
    const permissions = authStore.permissions;
    return permissions.some(p => 
      p === permission || 
      (p.endsWith(':*') && permission.startsWith(p.slice(0, -1)))
    );
  };

  return {
    hasPermission,
  };
}
