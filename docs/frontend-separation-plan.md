# Option A: Separate React Frontend from Node.js API

## Goal
- `kiwicart.azurewebsites.net` → React static files only (keeps the domain)
- `kiwicart-api.azurewebsites.net` → .NET backend API (already deployed)
- Node.js backend retired from production

## Steps

### 1. Update Frontend API Base URL
In `client/apis/products.ts`, change relative URLs to point to the .NET API:
```ts
const API_BASE = import.meta.env.VITE_API_URL || ''
const rootURL = `${API_BASE}/api/v1/products`
```
Add `VITE_API_URL=https://kiwicart-api.azurewebsites.net` to production env.

### 2. Update Vite Build for Static Hosting
The existing `vite build` already outputs to `dist/`. No server-side code needed.
Ensure `index.html` handles client-side routing (SPA fallback).

### 3. Configure Azure App Service for Static Files
Option: Change `kiwicart.azurewebsites.net` to serve static files:
- Switch runtime to Node 22
- Use a minimal `server.js` that serves `dist/` with SPA fallback:
```js
const express = require('express')
const path = require('path')
const app = express()
app.use(express.static(path.join(__dirname, 'dist')))
app.get('*', (req, res) => res.sendFile(path.join(__dirname, 'dist', 'index.html')))
app.listen(process.env.PORT || 3000)
```
Or switch to Azure Static Web Apps (free tier) and point custom domain.

### 4. Update CORS on .NET Backend
In `Program.cs`, ensure CORS allows the frontend origin:
```csharp
policy.WithOrigins("https://kiwicart.azurewebsites.net")
```
Already configured ✅

### 5. Update GitHub Actions Deploy Workflow
Modify `main_kiwicart.yml` to:
- Build only the frontend (`npm run build:client`)
- Deploy only the `dist/` folder + minimal server
- Remove Node.js backend build steps

### 6. Environment Variables
Add to GitHub Secrets:
- `VITE_API_URL=https://kiwicart-api.azurewebsites.net`

Add to Azure App Service (`kiwicart`) settings:
- No backend env vars needed anymore (no DB, no Auth0 server-side)

### 7. Auth0 Configuration
Update Auth0 dashboard:
- Allowed Callback URLs: keep `https://kiwicart.azurewebsites.net`
- Allowed Origins: keep `https://kiwicart.azurewebsites.net`
- API Audience: ensure .NET backend validates tokens from same Auth0 tenant

### 8. DNS / Domain
No changes needed — `kiwicart.azurewebsites.net` stays as-is.

## Verification
- [ ] `https://kiwicart.azurewebsites.net` loads React app
- [ ] Search calls `https://kiwicart-api.azurewebsites.net/api/v1/products/compare?q=Milk`
- [ ] CORS headers present on API responses
- [ ] Auth0 login still works
- [ ] Google Maps still works

---

## Modern Hosting Alternatives for Step 3

### Option 3A: pm2 serve (keep App Service + domain, zero code)
Keep `kiwicart.azurewebsites.net` as a Node App Service but serve only static files.
- Deploy only `dist/` folder
- Set Startup Command in Azure Portal → Configuration → General settings:
  ```
  pm2 serve /home/site/wwwroot/dist --no-daemon --spa
  ```
- No `server.js` needed
- SPA fallback handled by `--spa` flag

### Option 3B: Azure Static Web Apps (most modern, free tier)
- Global CDN, auto HTTPS, SPA routing built-in
- GitHub Actions integration out of the box
- Configure via `staticwebapp.config.json`:
  ```json
  {
    "navigationFallback": {
      "rewrite": "/index.html"
    }
  }
  ```
- New domain: `*.azurestaticapps.net` (or configure custom domain)
- ⚠️ Cannot use `kiwicart.azurewebsites.net` — that belongs to App Service
- Can add custom domain (e.g., `kiwicart.co.nz`) to both Static Web Apps and API

### Option 3C: Vercel / Netlify / Cloudflare Pages
- Free tier, global CDN, instant deploys
- Best DX (deploy previews on PRs, instant rollbacks)
- Custom domain support
- ⚠️ Same as 3B — different domain unless you own a custom one

### Recommendation
- **Keep `kiwicart.azurewebsites.net`** → Use Option 3A (pm2 serve)
- **Best practice for portfolio/production** → Use Option 3B (Azure Static Web Apps) + custom domain
- **Best DX** → Use Option 3C (Vercel) + custom domain

---

## API Protection: Restrict .NET API to Frontend Only

### Current Protection (already in place)
- CORS: only `localhost:5173` and `kiwicart.azurewebsites.net` allowed
- Rate Limiting: 60 requests/min per IP
- Auth0 JWT: on `[Authorize]` endpoints (favorites, delete)

### Recommended Addition: HMAC Timestamp Signature

Frontend generates a signature per request; backend validates it.

**Frontend (React):**
```ts
import CryptoJS from 'crypto-js'

const API_SECRET = import.meta.env.VITE_API_SECRET

function getAuthHeaders() {
  const timestamp = Math.floor(Date.now() / 1000).toString()
  const signature = CryptoJS.HmacSHA256(timestamp, API_SECRET).toString()
  return {
    'X-Api-Timestamp': timestamp,
    'X-Api-Signature': signature,
  }
}
```

**Backend (.NET middleware):**
```csharp
public class ApiSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secret;

    public ApiSignatureMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _secret = config["ApiSecurity:Secret"] ?? "";
    }

    public async Task Invoke(HttpContext context)
    {
        // Skip health check
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var timestamp = context.Request.Headers["X-Api-Timestamp"].FirstOrDefault();
        var signature = context.Request.Headers["X-Api-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
        {
            context.Response.StatusCode = 401;
            return;
        }

        // Check timestamp within 5 minutes
        if (!long.TryParse(timestamp, out var ts)
            || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > 300)
        {
            context.Response.StatusCode = 401;
            return;
        }

        // Validate HMAC
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(_secret));
        var expectedBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(timestamp));
        var expected = Convert.ToHexString(expectedBytes).ToLower();

        if (signature != expected)
        {
            context.Response.StatusCode = 401;
            return;
        }

        await _next(context);
    }
}
```

**Environment Variables:**
- Frontend: `VITE_API_SECRET=your-shared-secret`
- Backend (Azure App Settings): `ApiSecurity__Secret=your-shared-secret`

**Security Properties:**
- Requests are only valid for 5 minutes (replay window)
- Secret not visible as plaintext in network requests
- Requires reverse-engineering bundled JS to extract secret
- Combined with CORS + Rate Limiting = strong enough for most use cases

### Alternative: Simple API Key (easier, less secure)
- Frontend sends `X-Api-Key: your-key` header
- Backend checks against env var
- Key visible in browser DevTools network tab
- Still useful combined with CORS (blocks non-browser access from other origins)

### Not Recommended for Public APIs
- Azure VNet/IP restriction (App Service outbound IPs change)
- Client certificate auth (overkill for SPA)
