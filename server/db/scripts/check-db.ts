import db from '../connection.ts'

async function run() {
  const count = await db('supermarkets').count('* as cnt').first()
  console.log('Total supermarkets in DB:', count?.cnt)
  
  const sample = await db('supermarkets').limit(5)
  console.log('Sample stores:', JSON.stringify(sample, null, 2))
  
  process.exit(0)
}

run()
