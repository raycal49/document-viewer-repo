import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ThemeProvider, CssBaseline } from '@mui/material'
import { createAppTheme } from './theme.ts'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={createAppTheme('dark')}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </StrictMode>,
)
