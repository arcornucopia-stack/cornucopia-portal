# Cornucopia — Requirements Document

**Source:** Partner/product planning notes, 2026-08-16
**Status:** Draft for review — not yet scheduled or estimated
**Scope:** Admin Portal, Mobile App (iOS/Android), Firebase backend

This document consolidates the 2026-08-16 planning notes into discrete, numbered requirements grouped by epic. Each requirement includes acceptance criteria and the systems it touches. Open questions that need a decision before work starts are called out at the end of each epic and summarized at the bottom.

---

## Epic 1 — Partner Mailing List Upload

**Summary:** Let partners bring their own contact list into Cornucopia, with consent handling and a usage gate.

| ID | Requirement |
|----|-------------|
| R-1.1 | Partners can upload their own mailing list (contact file) from the partner-facing side of the portal. |
| R-1.2 | On upload, each contact is sent a welcome email with an explicit **Opt In** / **Decline** choice before receiving any further marketing (spam protocol / consent gate). |
| R-1.3 | Contacts who do not opt in are not marketed to; their status is tracked as pending/declined vs. confirmed. |
| R-1.4 | Uploads are capped at **1,000 contacts** for free; uploading more requires payment. |

**Open questions:**
- What payment mechanism/pricing applies above 1,000 contacts (flat fee, per-contact, subscription tier)?
- Where does opt-in status live (Firebase?), and does it need to be visible to the partner?
- Is there a suppression/unsubscribe list shared across partners, or is it per-partner?

---

## Epic 2 — Admin Portal: "Manage Uploads" Restructure

**Summary:** Consolidate the admin's upload-review workflow into one screen, simplify the left nav, and surface actions where admins are already looking.

| ID | Requirement |
|----|-------------|
| R-2.1 | Create a new admin screen, **"Manage Uploads,"** that keeps the current visual/state layout of "My Uploads" but adds the functionality currently in "Manage Uploads" (the Business / File / Target / Status / Date / Actions table with Approve, Reject, Push to App). |
| R-2.2 | The admin left nav is reduced to **three menu options** total (current "Manage Uploads" as a separate item is folded away — see R-2.1 and Epic 3 for where its pieces go). |
| R-2.3 | The **Pending** tab appears before the **Approved** tab in the admin portal. |
| R-2.4 | Clicking a "new partner upload" notification takes the admin directly to the **Pending** tab, not a generic landing page. |
| R-2.5 | Approve / Reject / Push to App action buttons are also available directly inside the product/model detail page (not just the list view), positioned **below the product thumbnail image**. |

**Open questions:**
- What are the exact three left-nav items after this change (e.g., Manage Uploads, Manage Partner, + one more)?
- Do the in-detail-page action buttons (R-2.5) disappear once a model is already approved/pushed, or stay visible with disabled/alternate states?

---

## Epic 3 — Admin Portal: "Manage Partner" Nav Item

**Summary:** Give partner-subscription management its own top-level place in the nav instead of being buried.

| ID | Requirement |
|----|-------------|
| R-3.1 | Add a standalone left-nav item, **"Manage Partner,"** that hosts the existing Partner Subscriptions screen (partner selector dropdown, subscriber checkbox list, "Refresh Partners/Users" and "Save Subscriber Mapping" actions). |

---

## Epic 4 — Admin Portal: Category Management

**Summary:** Give admins control over product categories, with a hard rule tying category visibility to whether it actually has content.

| ID | Requirement |
|----|-------------|
| R-4.1 | Build an admin-side system for creating/editing/managing product categories. |
| R-4.2 | A category with **0 models** in it must not appear on the mobile app front end. |

**Scope decision (2026-08-29):** the app has no catalog-browse screen today — users only ever see models explicitly assigned to them, never a general list/grid of all products. R-4.2 as written presumes a browsing UI that doesn't exist, so building it for real would mean a new app feature (a "Browse by Category" screen), not just an admin backend tweak. Decided to ship categories as **portal-only organization** for now: admins can create/delete categories and tag models with one; the "hide empty categories" rule is implemented as the portal's category filter dropdown only listing categories with ≥1 model. Revisit R-4.2 literally if a catalog-browse screen is ever built in the app.

---

## Epic 5 — Share Tracking (Data Capture)

**Summary:** Every share action becomes durable, queryable data — this feeds the Partner Report (Epic 14) and per-product report (Epic 11).

| ID | Requirement |
|----|-------------|
| R-5.1 | Every time a user shares a product, record that interaction (who, what product, when). |
| R-5.2 | Persist, in Firebase, a list of all entities/destinations a user has shared a product with (e.g., which channel/contact/platform). |

**Open questions:**
- Can we capture the destination for every share channel (native OS share sheet often only reports "share sheet opened," not which app the user picked)? If the OS doesn't report the destination app, R-5.2 may need to be scoped down to "share initiated" + channel-if-available.

---

## Epic 6 — Mobile App: Screen Chrome Consistency

| ID | Requirement |
|----|-------------|
| R-6.1 | The Question screen has a title bar (consistent with other screens in the app). |
| R-6.2 | The Rating screen has a title bar. |

---

## Epic 7 — Firebase Data Model: Question → Answer Mapping

**Summary:** Replace the current loosely-structured `answers` node with an explicit per-question mapping.

| ID | Requirement |
|----|-------------|
| R-7.1 | Replace/supplement the current `answers` field with a **`question_answers`** field structured so each question ID is explicitly mapped to the answer given for that specific question (rather than an implicit or positional relationship). |
| R-7.2 | Existing fields such as `comment`, `opened`, `saved`, `Rating` continue to be tracked alongside the new mapping. |

**Reference (current shape, from notes):**
```
models/{modelId}/
  MName
  Rating
  answer: "pending"
  answers/
    {questionId}: "Office"
  comment: "This is an amazing statue."
  opened: true
  saved: true
```

**Open questions:**
- Is this a migration of existing data, or does `question_answers` only apply going forward? If existing data must migrate, a one-time backfill script is needed.

---

## Epic 8 — Partner Analytics Dashboard Linkage

| ID | Requirement |
|----|-------------|
| R-8.1 | User interactions with a partner's models (opens, saves, ratings, question answers, shares) roll up and are visible on that partner's own analytics dashboard — not only in the global admin view. |

---

## Epic 9 — Partner Pricing, Deals & Call-to-Action

**Summary:** Let partners monetize and drive action directly from the product card/screen.

| ID | Requirement |
|----|-------------|
| R-9.1 | Partners can configure pricing on their product: **Brand Value** and **Price** fields (e.g., "Brand Value $1,200" / "Price $900.00"). |
| R-9.2 | Partners can configure a deal type via radio buttons: e.g., **Discount**, **BOGO** (Buy One Get One). |
| R-9.3 | The mobile app renders a template on the product screen that reflects the configured deal type and pricing. |
| R-9.4 | Partners can configure a **call-to-action (CTA) button** per product, choosing one of: **Buy Now**, **Subscribe**, **Donate**, **Pre-Order**, **Join Waitlist** — each with a partner-supplied link. |
| R-9.5 | Partners may instead choose **No CTA**, in which case the product screen shows only **Save** and **Share**, and the layout reflows/adjusts to fill the space the CTA button would have occupied (no dead space). |

**Resolved (2026-08-29):**
- Can a product have both a deal and a CTA simultaneously? **Yes** — the meeting-notes mockup itself shows Brand Value/Price and a Buy Now button together; they're independent, not exclusive layouts.
- Link validation? **Light touch** — must start with `http://` or `https://`, nothing stricter (no domain allowlist).

---

## Epic 10 — Mobile App: Engagement Stats Surfaced to Users

| ID | Requirement |
|----|-------------|
| R-10.1 | Total view count and/or save count can be displayed to the end user on the product card, alongside existing info like the star rating. |

---

## Epic 11 — Reporting: Per-Product Interaction Report

**Summary:** A dedicated report page per product with full statistical detail on how users engage with it.

| ID | Requirement |
|----|-------------|
| R-11.1 | Rating statistics: Median, Average, Standard Deviation, Min, Max, Frequency Distribution, and any other statistically relevant rating metrics. |
| R-11.2 | Full list of question/answer responses, plus the distribution of answers per question. |
| R-11.3 | For multiple-choice questions specifically: a **horizontal bar graph** of answer distribution. |
| R-11.4 | The bar graph supports two view modes: **percentage** and **raw values** (e.g., number of clicks) — user-togglable. |
| R-11.5 | Time spent interacting with the product is reported (see Epic 12 for the underlying capture). |
| R-11.6 | Views-over-time graph, broken down by day and time. |
| R-11.7 | CTA click counts are included in the report and shown on a timeline, in the same style as the views-over-time graph. |

---

## Epic 12 — Engagement Timing (Dwell Time)

| ID | Requirement |
|----|-------------|
| R-12.1 | Track how long a user keeps the Product Detail page open (a per-view session/dwell timer), feeding into R-11.5. |

---

## Epic 13 — Notifications: Countdown Urgency Timer

| ID | Requirement |
|----|-------------|
| R-13.1 | Replace the current "Recently Added" label/sort on notifications with a live **countdown timer starting at 24 hours**, ticking down to create urgency and encourage the user to open the notification sooner. |

---

## Epic 14 — Partner Report: Sharing Metrics

**Summary:** A new report component built on top of the share-tracking data from Epic 5, designed to surface and reward the partners' best growth-driving users.

| ID | Requirement |
|----|-------------|
| R-14.1 | Report the cumulative number of times users have shared a partner's product(s). |
| R-14.2 | Show the distribution of shares over time, in the same timeline-chart style used for other engagement metrics. |
| R-14.3 | Build a **share histogram**: X axis = binned ranges of total shares per user (e.g., 0, 1–10, 11–20, 21–30, …); Y axis = number of users falling into each bin. Purpose: identify which sharing-volume bin holds the bulk of users. |
| R-14.4 | In the admin backend, clicking a histogram bin surfaces the list of users in that bin, so the team can identify and promote/reward high-sharing users (the primary growth vector for the system). |

---

## Epic 15 — Strategic Concept: "Cornucopia Store" (not a build item yet)

**Summary:** Captured for context and future planning — no engineering work requested yet.

- Idea: treat Cornucopia itself as a "store" alongside partner stores, applying the same success metrics/reporting being built for partners (Epics 8, 11, 14) as a baseline for Cornucopia's own product promotion — analogous to "Amazon plus its third-party sellers."
- Framed explicitly as *start measuring a baseline now*, using infrastructure already being built, rather than a new standalone project.
- Positioned as (a) a new marketing/sales channel leaning on "Gift Economy" dynamics, and (b) a low-cost way to gauge product demand before committing to further physical/product development.
- **Action for now:** no build task — revisit once Epics 8/11/14 reporting exists, since it's meant to reuse that data.

---

## Summary of Open Questions

**Still open:**
1. Mailing-list overage pricing/payment flow (Epic 1).
2. Opt-in/suppression list scope — per-partner or shared (Epic 1).
3. Behavior of in-page action buttons after approval (Epic 2).
4. Share destination capture limits imposed by native OS share sheets (Epic 5).
5. Migration plan for existing `answers` data into `question_answers` (Epic 7).

**Resolved:**
- ~~Final 3-item admin left-nav naming/composition (Epic 2)~~ — Dashboard, Manage Uploads, Manage Partner (shipped 2026-08-29).
- ~~Can a product show both a deal and a CTA at once (Epic 9)?~~ — Yes, confirmed by the mockup (shipped 2026-08-29).
- ~~CTA link validation rules (Epic 9)~~ — must start with http(s):// (shipped 2026-08-29).
