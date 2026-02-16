import { defineStore } from 'pinia';

interface AuthState {
  token: string | null;
  permissions: string[];
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: null,
    permissions: []
  }),

  getters: {
    isAuthenticated: state => !!state.token,
  },

  actions: {
    parseToken(token: string) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.permissions = payload.permissions || [];
      } catch {
        this.permissions = [];
      }
    },

    setToken(token: string) {
      this.token = token;
      this.parseToken(token);
    }
  }
});
