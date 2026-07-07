import { useState, useEffect } from 'react'
import { Link } from 'react-router'
import { useAuth0 } from '@auth0/auth0-react'
import request from 'superagent'
import { buildApiUrl } from '../apis/apiBaseUrl'

interface FeedbackMessage {
  id: number
  user_name: string
  message: string
  created_at: string
}

function FeedbackBoard() {
  const { isAuthenticated, getAccessTokenSilently, user, loginWithRedirect } =
    useAuth0()
  const [messages, setMessages] = useState<FeedbackMessage[]>([])
  const [newMessage, setNewMessage] = useState('')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    request
      .get(buildApiUrl('/v1/feedback'))
      .then((res) => {
        setMessages(res.body)
        setLoading(false)
      })
      .catch(() => setLoading(false))
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!newMessage.trim()) return

    setSubmitting(true)
    try {
      const token = await getAccessTokenSilently()
      await request
        .post(buildApiUrl('/v1/feedback'))
        .set('Authorization', `Bearer ${token}`)
        .send({
          message: newMessage,
          userName: user?.nickname || user?.name || 'Anonymous',
        })

      setNewMessage('')
      const res = await request.get(buildApiUrl('/v1/feedback'))
      setMessages(res.body)
    } catch (err) {
      console.error('Failed to post message:', err)
    }
    setSubmitting(false)
  }

  return (
    <div className="max-w-3xl mx-auto">
      <Link
        to="/"
        className="inline-flex items-center gap-2 text-kiwi font-bold mb-6 hover:text-kiwi-dark transition-colors no-underline"
      >
        ← Back to Home
      </Link>
      <h1 className="text-4xl font-black text-kiwi-dark mb-2">
        Community Feedback
      </h1>
      <p className="text-gray-500 mb-8">
        Report a price bug or share your thoughts with the KiwiCart community.
      </p>

      {isAuthenticated ? (
        <form onSubmit={handleSubmit} className="mb-10">
          <textarea
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            placeholder="Share your feedback..."
            className="w-full p-4 border-2 border-gray-200 rounded-2xl focus:border-kiwi focus:outline-none resize-none min-h-[100px] text-base"
            maxLength={500}
          />
          <div className="flex justify-between items-center mt-3">
            <span className="text-sm text-gray-400">
              {newMessage.length}/500
            </span>
            <button
              type="submit"
              disabled={submitting || !newMessage.trim()}
              className="bg-kiwi text-white font-bold px-6 py-2.5 rounded-xl hover:bg-kiwi-dark transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {submitting ? 'Posting...' : 'Post'}
            </button>
          </div>
        </form>
      ) : (
        <div className="mb-10 p-6 bg-gray-50 rounded-2xl text-center">
          <p className="text-gray-600 mb-3">Sign in to leave feedback</p>
          <button
            onClick={() => loginWithRedirect()}
            className="bg-kiwi text-white font-bold px-6 py-2.5 rounded-xl hover:bg-kiwi-dark transition-colors"
          >
            Sign In
          </button>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-400">
          Loading messages...
        </div>
      ) : messages.length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          No messages yet. Be the first!
        </div>
      ) : (
        <div className="space-y-4">
          {messages.map((msg) => (
            <div
              key={msg.id}
              className="p-5 bg-white border border-gray-100 rounded-2xl shadow-sm"
            >
              <div className="flex justify-between items-center mb-2">
                <span className="font-bold text-kiwi-dark">
                  {msg.user_name}
                </span>
                <span className="text-xs text-gray-400">
                  {new Date(msg.created_at).toLocaleDateString('en-NZ', {
                    day: 'numeric',
                    month: 'short',
                    year: 'numeric',
                  })}
                </span>
              </div>
              <p className="text-gray-700 whitespace-pre-wrap">{msg.message}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default FeedbackBoard
