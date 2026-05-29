import { Knex } from 'knex'

/**
 * Creates the favorites table to store user-specific tracked products.
 */
export async function up(knex: Knex): Promise<void> {
  return knex.schema.createTable('favorites', (table) => {
    table.increments('id').primary()
    table.string('user_id').notNullable().index() // Auth0 sub ID
    table.string('product_name').notNullable()
    table.timestamps(true, true)
    
    // Ensure a user doesn't favorite the exact same name twice
    table.unique(['user_id', 'product_name'])
  })
}

/**
 * Drops the favorites table.
 */
export async function down(knex: Knex): Promise<void> {
  return knex.schema.dropTable('favorites')
}
