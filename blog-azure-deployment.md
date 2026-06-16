# Deploying a Full-Stack App to Azure: What Actually Happened

I recently deployed my grocery price comparison app (React + Node.js + PostgreSQL) to Azure App Service. It was supposed to be straightforward. It wasn't. Here's what I learned from all the things that went wrong.

## The Project

KiwiCart is a full-stack app that pulls real-time supermarket prices and lets users compare them side by side. The stack is React (Vite) on the front, Express on the back, Knex talking to PostgreSQL, with Auth0 for login and Google Maps for store locations. Locally it runs fine. Getting it to run on Azure was a different story.

## The Big Lesson: Build-time vs. Runtime

This one tripped me up the most and turned out to be the single most important concept for deploying a full-stack app.

Frontend variables (like `VITE_AUTH0_DOMAIN`) get baked into the code at build time. Once Vite bundles your React app, those values are frozen in the static files forever. So they need to exist in GitHub Actions, where the build happens — not on Azure.

Backend variables (like `DATABASE_URL`) get read at runtime. The Node server picks them up fresh every time it starts. These live in Azure's Application Settings.

Mixing these up means your app builds fine but breaks silently in production. I spent way too long debugging "why is Auth0 returning undefined" before this clicked.

## Things That Broke (and How I Fixed Them)

### GitHub couldn't talk to Azure

When I tried connecting GitHub Actions through Azure's Deployment Center, it threw `Cannot find SourceControlToken with name github`. The portal integration just wouldn't cooperate.

Fix: I gave up on the portal wizard and went manual. Downloaded the Publish Profile from Azure, pasted it into a GitHub Secret, and wired up the workflow file myself. Worked first try.

Except — I couldn't download the Publish Profile either. Azure had Basic Authentication disabled by default. Had to flip on "SCM Basic Auth Publishing" in the general settings before it let me grab the file.

### The workflow file had a silly mistake

My `deploy.yml` had the `app-name` set to `https://hidden-gemz.azurewebsites.net/`. It should have just been `hidden-gemz`. The full URL isn't a valid app name. Small thing, twenty minutes of confusion.

### "Cannot GET /"

Deployed successfully according to GitHub Actions. Visited the URL. `Cannot GET /`. Panic.

SSH'd into the container, ran `ls` in the default directory — nothing useful. Turns out Azure drops you in the wrong folder. The actual code lives at `/home/site/wwwroot`. Once I `cd`'d there, everything was present.

The real fix: I hadn't set `NODE_ENV=production` in Azure's environment variables. Without it, Express didn't know to serve the built static files from the `dist` folder.

### "knex: not found"

Needed to run database migrations on the server. Typed `knex migrate:latest`. Command not found.

Knex wasn't installed globally (and shouldn't be). The fix was using `npx knex migrate:latest` or `./node_modules/.bin/knex migrate:latest` from the project root.

### Auth0 login failing in production

Everything looked right. Variables were set. But login kept redirecting to an error page.

Forgot to update Auth0's allowed callback URLs. The dashboard still had `localhost:3000` listed. Added the Azure production URL to Allowed Callbacks, Allowed Logout URLs, and Allowed Web Origins. Immediately worked.

## The "Golden Checklist" I Wish I Had on Day One

**GitHub side (where the build happens):**
- Secrets: Azure Publish Profile
- Variables: All `VITE_` prefixed values (Auth0, Cloudinary, Maps keys)
- Workflow: `env` block mapping variables into the build step

**Azure side (where the app runs):**
- Environment variables: `DATABASE_URL`, `NODE_ENV=production`, backend Auth0 config
- SSL: Append `?sslmode=no-verify` to the database connection string
- Basic Auth: Enable SCM publishing so deployments can push

**Third-party services:**
- Update all callback/origin URLs from localhost to production

## What I'd Do Differently

Honestly? I'd write the checklist first. Most of my debugging time was spent on configuration gaps, not code bugs. The code didn't change at all between local and production — it was entirely about getting the environment right.

I'd also set up a health check endpoint earlier. Having `/api/health` return a 200 with the current `NODE_ENV` would have saved me multiple SSH sessions.

## Takeaways

- "It works locally" means nothing until you understand where your variables come from in production.
- SSH into your container early and often. `ls` and `cat` are your best debugging tools in the cloud.
- CI/CD is genuinely worth the setup pain. Once the workflow is right, deploying is just `git push`.
- Read the error messages properly. "No credentials found" means exactly what it says.

The whole experience took a day of troubleshooting, but now I have a deployment template I can reuse for any similar project in about five minutes. That's a pretty good trade.
