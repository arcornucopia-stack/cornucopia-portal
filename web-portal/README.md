# Cornucopia Business Portal (Web)

This portal lets subscribing business partners upload `.glb` files for approval and publication to the customer AR app.

## What is included
- Partner login
- Partner upload with targeting (`all_users` or `specific_users`)
- Admin queue to approve/reject and push submissions into app data
- Admin upload also follows same queue/push flow
- Basic analytics cards: opens and saves
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

## Setup
1. Create a Firebase web app in your existing project.
2. Copy `firebase-config.example.js` to `firebase-config.js`.
3. Paste your Firebase config into `firebase-config.js`.
4. In Realtime Database, create `users/{uid}` nodes with roles.
5. Apply `database.rules.json` and `storage.rules`.
6. Open `index.html` with a local server.

Example local server:

```powershell
cd web-portal
python -m http.server 5173
```

Then open `http://localhost:5173`.

## Unity integration notes
- Keep Unity app reading only `approved` submissions.
- Track customer analytics by writing to `events` when users open/save a model.
