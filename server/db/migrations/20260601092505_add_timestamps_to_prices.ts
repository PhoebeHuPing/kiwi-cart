import type { Knex } from 'knex'

export async function up(knex: Knex): Promise<void> {
  await knex.schema.alterTable('prices', (table) => {
    table.datetime('updated_at').nullable()
  })
}

export async function down(knex: Knex): Promise<void> {
  await knex.schema.alterTable('prices', (table) => {
    table.dropColumn('updated_at')
  })
}
