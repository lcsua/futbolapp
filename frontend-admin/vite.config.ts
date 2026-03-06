import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7272',
        changeOrigin: true,
        secure: false,
      },
    },
    allowedHosts: [
      '3e29-181-177-24-84.ngrok-free.app'
    ]
  },
})
