import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: '/backoffice/',
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5143',
        changeOrigin: true,
      },
      '/uploads': {
        target: 'http://127.0.0.1:5143',
        changeOrigin: true,
      },
    },
    allowedHosts: [
      '3c3f-181-177-24-84.ngrok-free.app'
    ]
  },
})
