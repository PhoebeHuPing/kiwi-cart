import { auth } from 'express-oauth2-jwt-bearer'
import dotenv from 'dotenv'

dotenv.config()

/**
 * Auth0 Middleware: Validates the Access Token sent in the Authorization header.
 * It checks the signature and ensures the token was issued by the correct Auth0 domain
 * for the intended audience (API Identifier).
 */
export const checkJwt = auth({
  audience: process.env.AUTH0_AUDIENCE,
  issuerBaseURL: `https://${process.env.AUTH0_DOMAIN}/`,
  tokenSigningAlg: 'RS256',
})
