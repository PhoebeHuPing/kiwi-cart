import express from 'express'
import * as db from '../db/index.ts'
import { checkJwt } from '../auth0.ts'

const router = express.Router()

router.get('/', async (req, res) => {
  try {
    const messages = await db.getFeedbackMessages()
    res.json(messages)
  } catch (error) {
    console.error('Failed to fetch feedback:', error)
    res.status(500).send('Something went wrong')
  }
})

router.post('/', checkJwt, async (req, res) => {
  try {
    const userId = req.auth?.payload.sub
    if (!userId) return res.status(401).send('Unauthorized')

    const { message, userName } = req.body
    if (!message || !message.trim()) return res.status(400).send('Message is required')

    await db.createFeedbackMessage(userId, userName || 'Anonymous', message.trim())
    res.status(201).json({ success: true })
  } catch (error) {
    console.error('Failed to post feedback:', error)
    res.status(500).send('Something went wrong')
  }
})

export default router
