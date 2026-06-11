import type { Knex } from 'knex'

export async function up(knex: Knex): Promise<void> {
  return knex.schema.createTable('feedback_messages', (table) => {
    table.increments('id').primary()
    table.string('user_id').notNullable().index()
    table.string('user_name').notNullable()
    table.text('message').notNullable()
    table.timestamps(true, true)
  })
}

export async function down(knex: Knex): Promise<void> {
  return knex.schema.dropTable('feedback_messages')
}
