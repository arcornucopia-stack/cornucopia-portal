# Cornucopia — Implementation Plan (2026-08-16 Requirements)

This is the step-by-step checklist we'll work through together, phase by phase. It follows [REQUIREMENTS.md](./REQUIREMENTS.md) — every task below references the requirement ID(s) it satisfies. Nothing here has been started yet; this is the plan to review and adjust before any build work begins.

Suggested build order: data model first (everything else reads/writes it), then admin portal restructuring (so the team can manage what follows), then partner-facing monetization features, then mobile UX polish, then the engagement-tracking + reporting layer that depends on it, then the sharing report, with the "Cornucopia Store" concept parked until the reporting layer exists.

---

## Phase 0 — Firebase Data Model Foundations
*Everything downstream depends on these shapes being settled first.*

- [ ] Design the `question_answers` structure mapping each question ID to its answer (R-7.1, R-7.2)
- [ ] Decide migration approach for existing `answers` data → `question_answers` (open question, R-7.1)
- [ ] Design share-event schema: share log entries + per-user "entities shared with" list (R-5.1, R-5.2)
- [ ] Add partner pricing/deal fields to the model record: Brand Value, Price, deal type (Discount/BOGO) (R-9.1, R-9.2)
- [ ] Add CTA config fields to the model record: CTA type + partner-supplied link, or "none" (R-9.4, R-9.5)
- [ ] Add counters/fields needed for reporting: view count, save count, CTA click count, dwell-time samples (R-10.1, R-11.5, R-11.7, R-12.1)
- [ ] Add mailing-list contact schema with opt-in status per contact (R-1.2, R-1.3)

## Phase 1 — Admin Portal Restructure

- [x] Build the new **Manage Uploads** page: "My Uploads" look/state + Approve/Reject/Push-to-App table functionality (R-2.1) — reused the existing Uploads screen/table for admins rather than a duplicate screen; nav button + breadcrumb relabel to "Manage Uploads" for admin role
- [x] Reduce admin left nav to three items (R-2.2) — Dashboard, Manage Uploads, Manage Partner
- [x] Add **Manage Partner** nav item hosting the Partner Subscriptions screen (R-3.1) — moved out of the Uploads screen into its own admin-only screen; no JS changes needed since the existing logic was already keyed by element ID, not by which screen contained it
- [x] Reorder tabs so **Pending** comes before **Approved** (R-2.3)
- [x] Route "new partner upload" notifications directly to the Pending tab (R-2.4)
- [x] Add Approve / Reject / Push to App buttons inside the product detail page, below the thumbnail (R-2.5)
- [x] Build category management screen for admins (R-4.1) — scoped to portal-only per decision below
- [~] Enforce: categories with 0 models are hidden from the mobile app (R-4.2) — **scope decision:** the app has no catalog-browse screen at all (users only ever see models explicitly assigned to them), so there's nowhere for an empty category to "show" today. Implemented the portal-side equivalent instead: the category filter dropdown in Manage Uploads only lists categories with ≥1 model. Revisit for real if a catalog-browse screen is ever built in the app.

Epics 2, 3, and 4 shipped 2026-08-29 (Epic 1/2 committed & deployed; Epic 3/4 uncommitted, pending test): the old separate "Manage Uploads" / Pending Tasks screen was removed entirely — its approve/reject/push table now lives inside the same screen as "My Uploads" (relabeled per role), and the same three buttons were added to the model detail page. Both places share one click handler so they can't drift out of sync. Partner Subscriptions now has its own "Manage Partner" nav item, landing the admin nav at exactly three items. Category management (admin CRUD on `cornucopia/categories`, category assignment on the model detail page, category filter on Manage Uploads) is portal-only for now — Phase 1 is complete.

## Phase 2 — Partner Mailing List ✅ (v1 shipped 2026-08-28)

- [x] Build partner-facing mailing list upload flow (R-1.1) — CSV upload under new "Mailing List" nav item
- [x] Build welcome email with Opt In / Decline links (R-1.2) — via EmailJS (client-side) + new `optin.html` landing page
- [x] Track opt-in/decline status per contact (R-1.3) — `pending | opted_in | declined` on each mailing-list entry
- [x] Enforce 1,000-contact free cap (R-1.4) — soft block with a "contact us to upgrade" message; **no real payment flow built** (OQ-1 still open, deferred by design)

Known v1 limitations to revisit later: EmailJS free-tier rate/volume limits make it unsuited to large bulk sends (see web-portal/README.md); no raw CSV file is retained (only parsed rows); suppression list is per-partner, not shared (OQ-2 still open).

## Phase 3 — Partner Monetization Features

- [ ] Add pricing + deal-type (radio) fields to the partner portal (R-9.1, R-9.2)
- [ ] Add CTA configuration UI to the partner portal (button type + link, or none) (R-9.4, R-9.5)
- [ ] Build mobile app deal template(s) reflecting the configured deal type (R-9.3)
- [ ] Build mobile app CTA button rendering, including the no-CTA reflow layout (R-9.5)

## Phase 4 — Mobile App Chrome & UX Polish

- [ ] Add title bar to the Question screen (R-6.1)
- [ ] Add title bar to the Rating screen (R-6.2)
- [ ] Display view/save counts on the product card (R-10.1)
- [ ] Replace "Recently Added" with a 24-hour countdown timer on notifications (R-13.1)

## Phase 5 — Engagement Tracking (Instrumentation)

- [ ] Implement share-event logging + entities-shared-with capture (R-5.1, R-5.2)
- [ ] Implement Product Detail dwell-time tracking (R-12.1)
- [ ] Implement CTA click tracking (R-11.7)

## Phase 6 — Reporting

- [ ] Build per-product report: rating stats (median/avg/SD/min/max/frequency dist.) (R-11.1)
- [ ] Build per-product report: question/answer list + per-question answer distribution (R-11.2)
- [ ] Build horizontal bar graph for multiple-choice answers, with %/raw toggle (R-11.3, R-11.4)
- [ ] Add time-spent-interacting metric to the report (R-11.5)
- [ ] Add views-over-time (day/time) graph (R-11.6)
- [ ] Add CTA-click timeline to the report (R-11.7)
- [ ] Link partner's product interactions into their own analytics dashboard (R-8.1)
- [ ] Build Partner Report sharing section: cumulative shares, share timeline (R-14.1, R-14.2)
- [ ] Build share histogram (binned shares-per-user on X, user count on Y) (R-14.3)
- [ ] Add admin drill-down from a histogram bin to its user list (R-14.4)

## Phase 7 — Strategic / Parked

- [ ] Scope "Cornucopia Store" baseline measurement once Phase 6 reporting exists — planning conversation only, no build yet (Epic 15)

---

## Before We Start Building

Resolve the open questions listed at the bottom of REQUIREMENTS.md — particularly:
1. Mailing-list overage payment mechanism (blocks Phase 2 completion)
2. Final 3-item nav composition (blocks Phase 1)
3. Migration plan for `answers` → `question_answers` (blocks Phase 0)
4. Deal + CTA mutual exclusivity (blocks Phase 3 UI design)

Once phases are reviewed and reprioritized as needed, we'll tackle them one checkbox at a time.
