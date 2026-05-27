import { fetchWoolworthsPrices } from './server/services/woolworths.ts';

async function test() {
  console.log('Testing Woolworths Price Fetching from Service...');
  const results = await fetchWoolworthsPrices('Milk');
  console.log(`Found ${results.length} results for 'Milk'`);
  if (results.length > 0) {
    console.log('First result:', results[0]);
  } else {
    console.log('No results found.');
  }
}

test().catch(console.error);
