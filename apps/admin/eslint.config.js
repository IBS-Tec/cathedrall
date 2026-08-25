import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
  },
  {
    files: ['**/*.tsx'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          {
            group: ['**/fake', '**/fake/*'],
            message:
              'Nenhuma tela conhece a origem do dado. Consuma os hooks de modules/<modulo>/queries.ts.',
          },
          {
            group: ['**/modules/*/api', './api', '../api'],
            message:
              'api.ts é a costura com o cliente gerado. Consuma os hooks de modules/<modulo>/queries.ts.',
          },
        ],
      }],
    },
  },
])
