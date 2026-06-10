import * as Path from 'node:path'
import express from 'express'
import helmet from 'helmet'
import cors from 'cors'
import rateLimit from 'express-rate-limit'
import productsRoutes from './routes/products.ts'

const server = express()

// Security headers
server.use(helmet({
  contentSecurityPolicy: false, // Disable CSP to allow Google Maps scripts
}))

// CORS: allow same-origin in production, localhost in development
server.use(cors({
  origin: process.env.NODE_ENV === 'production'
    ? process.env.CORS_ORIGIN || true
    : 'http://localhost:5173',
}))

server.use(express.json())

// Rate limiting: max 30 requests per minute per IP for API routes
const apiLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 30,
  message: { error: 'Too many requests, please try again later.' },
  standardHeaders: true,
  legacyHeaders: false,
})
server.use('/api', apiLimiter)

// API ROUTES: NZ Supermarket Price Comparison
// All product and store related requests are handled by productsRoutes
server.use('/api/v1/products', productsRoutes)

// In production, serve the compiled frontend assets from the 'dist' folder
if (process.env.NODE_ENV === 'production') {
  server.use(express.static(Path.resolve('public')))
  server.use('/assets', express.static(Path.resolve('./dist/assets')))
  
  // Single Page Application (SPA) support: Redirect all unknown routes to index.html
  server.get('*', (req, res) => {
    res.sendFile(Path.resolve('./dist/index.html'))
  })
}

export default server
