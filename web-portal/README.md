# Cornucopia Business Portal (Web)

This portal lets subscribing business partners upload `.glb` files for approval and publication to the customer AR app.

## What is included
- Partner login
- Partner upload queue (`pending/approved/rejected`)
- Admin queue to approve/reject submissions
- Basic analytics cards: opens and saves
- Firebase security rules templates
- Optional Cloud Function template for approval workflow

## Suggested architecture
- Auth: Firebase Authentication (email/password)
- Data: Firestore
- Files: Firebase Storage
- Optional server logic: Firebase Functions

### Firestore collections
- `users/{uid}`
  - `role`: `admin | partner`
  - `businessId`: string
  - `businessName`: string
- `submissions/{submissionId}`
  - `businessId`, `businessName`, `uploaderUid`
  - `fileName`, `storagePath`
  - `status`: `pending | approved | rejected`
  - `createdAt`, `approvedAt`, `rejectedAt`
  - `decisionBy`
- `events/{eventId}`
  - `businessId`, `modelId`, `eventType` (`open` or `save`), `createdAt`

## Setup
1. Create a Firebase web app in your existing project.
2. Copy `firebase-config.example.js` to `firebase-config.js`.
3. Paste your Firebase config into `firebase-config.js`.
4. In Firebase console, create `users/{uid}` docs with proper roles.
5. Apply `firestore.rules` and `storage.rules`.
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
- For strict approval security, use Cloud Functions (template provided in `functions/index.js`).
