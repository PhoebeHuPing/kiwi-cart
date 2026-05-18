import * as fs from 'fs'

const pnsData = JSON.parse(fs.readFileSync('pns_data.json', 'utf8'))

function findKey(obj: any, key: string, path = ''): any {
  if (!obj || typeof obj !== 'object') return null
  if (obj[key]) return path + '.' + key
  for (const k in obj) {
    const p = findKey(obj[k], key, path + '.' + k)
    if (p) return p
  }
  return null
}

console.log('Path to regionStoreGroupings:', findKey(pnsData, 'regionStoreGroupings'))
