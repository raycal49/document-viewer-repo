import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist', 'coverage']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    rules: {
      // fails if there is complext code
      'complexity': ['error', 8],  
      'max-depth': ['error', 3],     
      'max-lines-per-function': ['error', { 
        max: 65, 
        skipBlankLines: true, 
        skipComments: true 
      }],
      'max-nested-callbacks': ['error', 3],
      'max-params': ['error', 4],    
      'max-statements': ['error', 20], 
      
      'no-console': 'warn',           
      'no-debugger': 'error',         
      'no-alert': 'error',            
      'no-var': 'error',              
      'prefer-const': 'error',       
    },
  },
])
