import request from 'superagent'
import * as fs from 'fs'

async function scrapeFoodstuffs(url: string, filename: string) {
  console.log(`Scraping ${url}...`)
  try {
    const response = await request
      .get(url)
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
    
    const html = response.text
    const match = html.match(/<script id="__NEXT_DATA__" type="application\/json">(.*?)<\/script>/)
    if (match && match[1]) {
      const data = JSON.parse(match[1])
      // Sometimes stores are in props.pageProps.stores or similar
      // Based on my previous curl, it was in props.pageProps.fallback['default-store']
      // Let's dump the whole thing to see the structure
      fs.writeFileSync(filename, JSON.stringify(data, null, 2))
      console.log(`Saved data to ${filename}`)
    } else {
      console.error('Could not find __NEXT_DATA__')
    }
  } catch (err: any) {
    console.error(`Error scraping ${url}:`, err.message)
  }
}

scrapeFoodstuffs('https://www.paknsave.co.nz/store-finder', 'pns_data.json')
scrapeFoodstuffs('https://www.newworld.co.nz/store-finder', 'nw_data.json')
