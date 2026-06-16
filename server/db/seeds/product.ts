import type { Knex } from 'knex'

export async function seed(knex: Knex): Promise<void> {
  await knex('prices').del()
  await knex('products').del()
  await knex('stores').del()

  await knex('stores').insert([
    {
      id: 1,
      name: 'Woolworths',
      brand: 'Woolworths',
      logo_url: '/images/woolworths.webp',
      address: 'Botany Town Centre, East Auckland',
      latitude: -36.9322,
      longitude: 174.9125,
    },
    {
      id: 2,
      name: 'New World',
      brand: 'NewWorld',
      logo_url: '/images/new-world.webp',
      address: 'Albany, North Shore',
      latitude: -36.7264,
      longitude: 174.7044,
    },
    {
      id: 3,
      name: 'PaknSave',
      brand: 'PakNSave',
      logo_url: '/images/pak-n-save.webp',
      address: 'Henderson, West Auckland',
      latitude: -36.8819,
      longitude: 174.6336,
    },
  ])

  await knex('products').insert([
    {
      id: 1,
      name: 'Red Apple',
      brand: '',
      category: 'Produce',
      image_url: '/images/apple.webp',
    },
    {
      id: 2,
      name: 'Anchor Milk 2L',
      brand: '',
      category: 'Dairy',
      image_url: '/images/milk.webp',
    },
    {
      id: 3,
      name: 'White Bread',
      brand: '',
      category: 'Bakery',
      image_url: '/images/bread.webp',
    },
  ])

  await knex('prices').insert([
    // Apple
    { product_id: 1, store_id: 1, amount: 3.5 },
    { product_id: 1, store_id: 2, amount: 3.2 },
    { product_id: 1, store_id: 3, amount: 2.9 },

    // Milk
    { product_id: 2, store_id: 1, amount: 4.8 },
    { product_id: 2, store_id: 2, amount: 4.5 },
    { product_id: 2, store_id: 3, amount: 4.25 },

    // Bread
    { product_id: 3, store_id: 1, amount: 2.5 },
    { product_id: 3, store_id: 2, amount: 2.3 },
    { product_id: 3, store_id: 3, amount: 1.99 },
  ])
}
