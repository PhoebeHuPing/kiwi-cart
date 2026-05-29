import { auth } from 'express-oauth2-jwt-bearer'
import dotenv from 'dotenv'

dotenv.config()

const domain = process.env.VITE_AUTH0_DOMAIN
const audience = process.env.VITE_AUTH0_AUDIENCE

/**
 * Auth0 Middleware: Validates the Access Token sent in the Authorization header.
 * It checks the signature and ensures the token was issued by the correct Auth0 domain
 * for the intended audience (API Identifier).
 */
export const checkJwt = auth({
  audience: audience,
  issuerBaseURL: `https://${domain}/`,
  tokenSigningAlg: 'RS256',
})
