import * as Path from 'node:path'
import express from 'express'
import productsRoutes from './routes/products.ts'

const server = express()
server.use(express.json())

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
