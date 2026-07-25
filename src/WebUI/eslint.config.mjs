import js from '@eslint/js'
import tseslint from 'typescript-eslint'
import pluginVue from 'eslint-plugin-vue'
import pluginA11y from 'eslint-plugin-vuejs-accessibility'
import globals from 'globals'

// CLAUDE.md'deki frontend kuralları bu ana kadar yalnızca elle denetleniyordu (#68).
// Otomatikleştirilebilenler aşağıda kural olarak yazılı; her birinin başında hangi
// CLAUDE.md maddesini karşıladığı belirtiliyor.
export default tseslint.config(
  {
    ignores: ['dist/**', 'node_modules/**', '.quasar/**'],
  },

  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  ...pluginA11y.configs['flat/recommended'],

  {
    files: ['**/*.{ts,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
      },
      parserOptions: {
        parser: tseslint.parser,
      },
    },
    rules: {
      // CLAUDE.md — "<script setup> zorunlu, Options API veya setup() fonksiyonu KULLANILMAZ"
      'vue/component-api-style': ['error', ['script-setup']],

      // CLAUDE.md — "İkon butonu = aria-label + <q-tooltip>. title attribute'ü KULLANILMAZ
      // (WCAG için güvenilir değil, görsel tooltip standart açılmaz)."
      // Yalnız q-btn hedefleniyor: FormDialog/DetailDialog gibi kendi bileşenlerimizde
      // title bir prop, HTML attribute değil.
      'vue/no-restricted-static-attribute': [
        'error',
        {
          key: 'title',
          element: 'q-btn',
          message:
            'İkon butonunda title kullanma — aria-label (ekran okuyucu) + <q-tooltip> (görsel) kullan. CLAUDE.md ikon butonu kuralı.',
        },
      ],

      // CLAUDE.md — "Fire-and-forget async çağrılarda .catch(() => {}) eklenir;
      // void fn() hata yutabilir."
      'no-void': ['error', { allowAsStatement: false }],

      // CLAUDE.md — "JSON.parse(JSON.stringify()) YASAK."
      'no-restricted-syntax': [
        'error',
        {
          selector:
            "CallExpression[callee.object.name='JSON'][callee.property.name='parse'] > CallExpression[callee.object.name='JSON'][callee.property.name='stringify']",
          message:
            'JSON.parse(JSON.stringify()) ile derin kopya alma — CLAUDE.md yasaklıyor. Reaktif veri için elle .map() kopyası yaz.',
        },
      ],

      // Sayfa/görünüm bileşenleri tek kelimelik (StudentList, PaymentPage) — kasıtlı.
      'vue/multi-word-component-names': 'off',

      // Kullanılmayan değişken hatası; alt çizgiyle başlayanlar kasıtlı olarak muaf.
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_', caughtErrors: 'none' },
      ],
    },
  },

  {
    files: ['**/*.spec.ts', '**/*.test.ts'],
    languageOptions: {
      globals: {
        ...globals.node,
      },
    },
  },
)
