import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'lcov'],
      
      include: [
        'src/webrtc/hooks/webrtcHooks.ts',
        'src/webrtc/services/webrtcService.ts',
        'src/webrtc/signaling/**/*.ts',
        'src/scenes/videoCallPage/Annotation/AnnotationLogic.ts',
        'src/scenes/videoCallPage/AnnotationLogger.ts'
      ],
      
      exclude: [
        'node_modules/',
        'src/test/**',
        '**/*.d.ts',
        '**/*.config.*',
        '**/dist/**',
        '**/*.test.ts',
        '**/*.test.tsx',
        'src/main.tsx',
        'src/vite-env.d.ts'
      ],
      
      // fails if covarege is not 100%
      thresholds: {
        lines: 100,
        functions: 100,
        branches: 100,
        statements: 100
      },
      
      //all: false
    },
    
    watch: false,
    reporters: ['verbose']
  }
})
