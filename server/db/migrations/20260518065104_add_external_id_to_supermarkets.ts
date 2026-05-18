import type { Knex } from 'knex'

export async function up(knex: Knex): Promise<void> {
  return knex.schema.table('supermarkets', (table) => {
    table.string('external_store_id')
    table.string('brand') // 'paknsave', 'newworld', 'woolworths'
  })
}

export async function down(knex: Knex): Promise<void> {
  return knex.schema.table('supermarkets', (table) => {
    table.dropColumn('external_store_id')
    table.dropColumn('brand')
  })
}
