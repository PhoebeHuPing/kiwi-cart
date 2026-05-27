import request from 'superagent';

async function test() {
  console.log('Testing Woolworths Price Fetching with Session...');
  const agent = request.agent(); // To maintain cookies

  try {
    console.log('Establishing session...');
    await agent
      .post('https://www.woolworths.co.nz/api/v1/session')
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('X-Requested-With', 'OnlineShopping.WebApp')
      .send({});
    console.log('Session established.');

    console.log('Searching for Milk...');
    const response = await agent
      .get('https://www.woolworths.co.nz/api/v1/products')
      .query({ 
        target: 'search', 
        search: 'Milk',
        inStockProductsOnly: true 
      })
      .set('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36')
      .set('X-Requested-With', 'OnlineShopping.WebApp')
      .set('Accept', 'application/json');

    console.log('Response status:', response.status);
    const body = response.body;
    const items = body.products?.items || [];
    console.log(`Found ${items.length} items.`);
    if (items.length > 0) {
      console.log('First item name:', items[0].name);
      console.log('First item price:', items[0].price?.salePrice);
    }
  } catch (err: any) {
    console.error('Error:', err.message);
    if (err.response) {
      console.error('Response body:', JSON.stringify(err.response.body, null, 2));
    }
  }
}

test().catch(console.error);
