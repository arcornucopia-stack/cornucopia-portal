# Cornucopia Business Portal (Web)

This portal lets subscribing business partners upload `.glb` files for approval and publication to the customer AR app.

## What is included
- Partner login
- Partner upload with targeting (`all_users` or `specific_users`)
- Admin queue to approve/reject and push submissions into app data
- Admin upload also follows same queue/push flow
- Basic analytics cards: opens and saves
- Partner mailing list upload with double opt-in consent gate (see below)
- Firebase Realtime Database rules template
- Firebase Storage rules template

## Suggested architecture (Datastore-mode compatible)
- Auth: Firebase Authentication (email/password)
- Data: Realtime Database
- Files: Firebase Storage

## Realtime Database paths
- `users/{uid}`
  - `role`: `admin | partner`
  - `businessId`: string
  - `businessName`: string
- `submissions/{submissionId}`
  - `businessId`, `businessName`, `uploaderUid`
  - `fileName`, `storagePath`
  - `status`: `pending | approved | rejected`
  - `targetMode`: `all_users | specific_users`
  - `targetUserIds`: array
  - `pushedToApp`, `pushedAt`, `pushedCount`
  - `createdAt`, `approvedAt`, `rejectedAt`
  - `decisionBy`
- `events/{eventId}`
  - `businessId`, `modelId`, `eventType` (`open` or `save`), `createdAt`
- `partners/{partnerUid}/mailingList/{entryId}`
  - `email`, `name` (optional)
  - `status`: `pending | opted_in | declined`
  - `uploadedAt`, `respondedAt`

## Partner mailing list (consent-gated)

Partners can upload a CSV of contacts from **Mailing List** in the sidebar. Each new
contact is saved with `status: "pending"` and gets a one-time welcome email with a
link to `optin.html`, where they choose **Opt In** or **Decline** — no further
marketing should be sent to a contact until their status is `opted_in`. The free
tier caps a partner at 1,000 total contacts; uploads that would exceed the cap are
rejected in full with a message asking them to contact you to upgrade (no payment
flow is wired up yet — see `docs/requirements/REQUIREMENTS.md`, open question OQ-1).

Email sending uses [EmailJS](https://www.emailjs.com/) directly from the browser
(no backend/Cloud Functions required, matching this project's static-hosting
setup). To enable it:
1. Create a free EmailJS account, add an Email Service, and note its Service ID.
2. Create an Email Template using these variables: `{{to_email}}`, `{{to_name}}`,
   `{{partner_name}}`, `{{optin_link}}` — the link should be a plain `<a href="{{optin_link}}">`
   that lands the recipient on `optin.html`, where they make their own Opt In / Decline choice.
3. Copy `emailjs-config.example.js` to `emailjs-config.js` and paste in your Public
   Key, Service ID, and Template ID.

Until `emailjs-config.js` has real values, contacts are still uploaded and tracked
normally — the app just tells the partner the welcome email wasn't sent.

**Note on scale:** EmailJS's free tier has a modest monthly send quota and a
per-second rate limit; sends are throttled client-side to stay under the rate
limit, but a partner uploading hundreds of contacts at once may want a paid
EmailJS plan or, longer-term, a real backend email service (see
`docs/requirements/REQUIREMENTS.md`).

## Setup
1. Create a Firebase web app in your existing project.
2. Copy `firebase-config.example.js` to `firebase-config.js`.
3. Paste your Firebase config into `firebase-config.js`.
4. Copy `emailjs-config.example.js` to `emailjs-config.js` and fill it in (optional — see above).
5. In Realtime Database, create `users/{uid}` nodes with roles.
6. Apply `database.rules.json` and `storage.rules`.
7. Open `index.html` with a local server.

Example local server:

```powershell
cd web-portal
python -m http.server 5173
```

Then open `http://localhost:5173`.

## Unity integration notes
- Keep Unity app reading only `approved` submissions.
- Track customer analytics by writing to `events` when users open/save a model.
