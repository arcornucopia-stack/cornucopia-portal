# Cornucopia — How It Works
### A plain-language overview for business stakeholders

---

## What is Cornucopia?

Cornucopia is a mobile AR (augmented reality) platform that lets consumers point their phone at a surface and see a 3D model of a product appear in their physical space — on a table, a shelf, or a floor. Brands submit their product models through a web portal, Cornucopia reviews and approves them, and approved models are delivered to consumers in the mobile app.

---

## The Three Pieces

| Piece | Who uses it | What it does |
|-------|-------------|--------------|
| **Partner Portal** (website) | Brand partners & Cornucopia admin | Upload models, track approvals, view engagement |
| **Mobile App** (iOS & Android) | Consumers | View and interact with 3D models in AR |
| **Backend** (invisible) | No one directly | Stores files, syncs data between portal and app |

---

## The Journey of a Model

### 1. A brand submits a model
The brand logs into the **Partner Portal** and uploads a `.glb` file — an industry-standard 3D model format. Along with the file they provide:
- Their business name
- A display name for the model
- An optional prompt question shown to consumers (e.g. *"Would you wear this?"*)
- Who they want to target — all app users, or a specific group

### 2. Cornucopia reviews it
The submission lands in the **Approval Queue**, visible only to the Cornucopia admin. The admin can approve or reject the model. Nothing goes live to consumers until this step is complete.

### 3. The model goes live
Once approved, the model appears in the mobile app. Consumers who open the app will see it in their feed and can view it in AR.

### 4. The brand sends it to users
After approval, the brand can push the model to their subscriber list directly from the portal — either the full user base or specific users they've been assigned. This gives brands control over timing and targeting.

### 5. Consumers interact
When a consumer opens the model in AR, their interaction is recorded:
- **Opens** — how many times users engaged with (voted on) the model
- **Saves** — how many users saved the model to their collection

---

## What the Portal Dashboard Shows

When a brand logs in they see four live metrics at a glance:

- **Total uploads** — all models they've ever submitted
- **Approved** — how many have passed review and are live
- **Opens** — total consumer engagement across all their models
- **Saves** — total saves across all their models

---

## User Roles

The portal has three levels of access:

| Role | What they can do |
|------|-----------------|
| **Admin** (Cornucopia team) | See all submissions, approve/reject, dispatch models to any users, manage partner subscriber lists, view the full live model catalogue |
| **Partner** (brand) | Upload models, track their own submissions, send approved models to their subscriber list |
| **Consumer** | Mobile app only — no portal access |

---

## What "Subscribers" Means

Each partner has a subscriber list — a set of app users assigned to that brand. When a partner sends a model, it is delivered specifically to their subscribers. The Cornucopia admin controls which users are assigned to which partner. This allows targeted product experiences without cluttering every user's feed.

---

## Approve vs. Push to App

When a submission appears in the Approval Queue, the admin sees two actions:

- **Approve** — marks the submission as cleared for publishing, but does not yet deliver it to anyone. The model sits in a ready state, waiting to be pushed. Useful when you want to review and pre-clear content ahead of a planned release.

- **Push to App** — the full delivery action. This approves the model (if not already approved), adds it to the live app catalogue, and assigns it to the target users' accounts so it appears in their notifications. This is what actually puts the model in front of consumers.

### How targeting works on Push to App

When a model is pushed, who receives it depends on how the submission was set up:

- **All users** — delivered to every consumer in the system (excluding admins and partners)
- **Specific users** — delivered only to the user accounts listed when the model was uploaded

For partner accounts, the recommended flow is:
1. Admin assigns a subscriber list to the partner (via Partner Subscriptions)
2. Partner uploads a model and receives admin approval
3. Partner pushes the approved model to their subscribers from their portal dashboard

Admins can also bypass this flow and push any existing model to any set of users directly via **Send Existing Model To Users**.

---

## Summary in One Sentence

Cornucopia is a managed AR marketplace: brands submit 3D product models through a web portal, Cornucopia approves them, and consumers experience them in augmented reality on their phones — with full engagement tracking back to the brand.
