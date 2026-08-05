import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  // Load env files based on mode
  const env = loadEnv(mode, process.cwd(), '')
  
  // Priority: process.env > .env file > default 3000
  const serverPort = process.env.VITE_SERVER_PORT || env.VITE_SERVER_PORT || '3000'
  
  console.log(`Proxy target: http://localhost:${serverPort}`)
  
  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: `http://localhost:${serverPort}`,
          changeOrigin: true,
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
    },
  }
})
