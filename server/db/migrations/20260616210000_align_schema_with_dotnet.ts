import type { Knex } from 'knex'

/**
 * Migration to align the database schema with the .NET backend expectations.
 * Uses raw SQL with IF NOT EXISTS to be safe against partial runs.
 */
export async function up(knex: Knex): Promise<void> {
  // 1. Rename 'supermarkets' -> 'stores' if supermarkets still exists
  const hasSupermarkets = await knex.schema.hasTable('supermarkets')
  if (hasSupermarkets) {
    await knex.schema.renameTable('supermarkets', 'stores')
  }

  // 2. Add columns to stores if missing
  const hasStoreBrand = await knex.schema.hasColumn('stores', 'brand')
  if (!hasStoreBrand) {
    await knex.schema.alterTable('stores', (t) => t.string('brand', 100).defaultTo(''))
  }
  const hasLatitude = await knex.schema.hasColumn('stores', 'latitude')
  if (!hasLatitude) {
    const hasLat = await knex.schema.hasColumn('stores', 'lat')
    if (hasLat) {
      await knex.schema.alterTable('stores', (t) => {
        t.renameColumn('lat', 'latitude')
        t.renameColumn('lng', 'longitude')
      })
    }
  }

  // 3. Add brand to products if missing
  const hasProductBrand = await knex.schema.hasColumn('products', 'brand')
  if (!hasProductBrand) {
    await knex.schema.alterTable('products', (t) => t.string('brand', 200).defaultTo(''))
  }

  // 4. Rename prices columns if needed
  const hasStoreId = await knex.schema.hasColumn('prices', 'store_id')
  if (!hasStoreId) {
    const hasSupermarketId = await knex.schema.hasColumn('prices', 'supermarket_id')
    if (hasSupermarketId) {
      await knex.schema.alterTable('prices', (t) => t.renameColumn('supermarket_id', 'store_id'))
    }
  }
  const hasAmount = await knex.schema.hasColumn('prices', 'amount')
  if (!hasAmount) {
    const hasPrice = await knex.schema.hasColumn('prices', 'price')
    if (hasPrice) {
      await knex.schema.alterTable('prices', (t) => t.renameColumn('price', 'amount'))
    }
  }
  const hasRetrievedAt = await knex.schema.hasColumn('prices', 'retrieved_at')
  if (!hasRetrievedAt) {
    const hasUpdatedAt = await knex.schema.hasColumn('prices', 'updated_at')
    if (hasUpdatedAt) {
      await knex.schema.alterTable('prices', (t) => t.renameColumn('updated_at', 'retrieved_at'))
    }
  }

  // 5. Favorites: add product_id if missing
  const hasFavProductId = await knex.schema.hasColumn('favorites', 'product_id')
  if (!hasFavProductId) {
    await knex.schema.alterTable('favorites', (t) => t.integer('product_id').defaultTo(0))
  }

  // 6. Create missing tables
  const hasStoreTokens = await knex.schema.hasTable('store_tokens')
  if (!hasStoreTokens) {
    await knex.schema.createTable('store_tokens', (t) => {
      t.increments('id').primary()
      t.string('store_brand', 100).notNullable().unique()
      t.text('token').notNullable()
      t.timestamp('expires_at').notNullable()
    })
  }

  const hasBuckets = await knex.schema.hasTable('buckets')
  if (!hasBuckets) {
    await knex.schema.createTable('buckets', (t) => {
      t.increments('id').primary()
      t.string('user_id', 200).notNullable()
      t.string('name', 200).notNullable()
    })
  }

  const hasBucketItems = await knex.schema.hasTable('bucket_items')
  if (!hasBucketItems) {
    await knex.schema.createTable('bucket_items', (t) => {
      t.increments('id').primary()
      t.integer('bucket_id').references('buckets.id').onDelete('CASCADE')
      t.integer('product_id').notNullable()
      t.integer('quantity').notNullable().defaultTo(1)
    })
  }
}

export async function down(knex: Knex): Promise<void> {
  await knex.schema.dropTableIfExists('bucket_items')
  await knex.schema.dropTableIfExists('buckets')
  await knex.schema.dropTableIfExists('store_tokens')

  const hasStoreId = await knex.schema.hasColumn('prices', 'store_id')
  if (hasStoreId) {
    await knex.schema.alterTable('prices', (t) => {
      t.renameColumn('store_id', 'supermarket_id')
      t.renameColumn('amount', 'price')
      t.renameColumn('retrieved_at', 'updated_at')
    })
  }

  await knex.schema.alterTable('products', (t) => t.dropColumn('brand'))

  await knex.schema.alterTable('stores', (t) => {
    t.dropColumn('brand')
    t.renameColumn('latitude', 'lat')
    t.renameColumn('longitude', 'lng')
  })
  await knex.schema.renameTable('stores', 'supermarkets')
}
