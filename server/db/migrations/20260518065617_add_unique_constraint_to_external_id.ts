import type { Knex } from 'knex'

export async function up(knex: Knex): Promise<void> {
  return knex.schema.table('supermarkets', (table) => {
    table.unique(['external_store_id'])
  })
}

export async function down(knex: Knex): Promise<void> {
  return knex.schema.table('supermarkets', (table) => {
    table.dropUnique(['external_store_id'])
  })
}
