# Cornucopia — Implementation Plan (2026-08-16 Requirements)

This is the step-by-step checklist we'll work through together, phase by phase. It follows [REQUIREMENTS.md](./REQUIREMENTS.md) — every task below references the requirement ID(s) it satisfies.

**Status as of 2026-09-26: Phases 0–6 are complete and deployed.** Only Phase 7 (deliberately parked, planning-only) remains. On the app side, the CornucopiaV2 scenes still need a "Build All Screens" pass (UIBuilder v14) and an on-device test.

Suggested build order: data model first (everything else reads/writes it), then admin portal restructuring (so the team can manage what follows), then partner-facing monetization features, then mobile UX polish, then the engagement-tracking + reporting layer that depends on it, then the sharing report, with the "Cornucopia Store" concept parked until the reporting layer exists.

---

## Phase 0 — Firebase Data Model Foundations ✅
*Everything downstream depends on these shapes being settled first.*

- [x] Question → answer mapping (R-7.1, R-7.2) — **resolved without a rename:** the V2 app already writes `users/{uid}/models/{modelKey}/answers/{questionId}: "<answer>"`, which *is* the explicit per-question mapping the requirement asks for (not positional). Renaming the node to `question_answers` would force a data migration and a dual-read in the portal for zero functional gain, so the existing `answers` node is kept as the canonical mapping; `comment`, `opened`, `saved`, `Rating` continue alongside it unchanged.
- [x] Migration of existing `answers` data — not needed, follows from the above.
- [x] Share-event schema (R-5.1, R-5.2) — absorbed into Phase 5 (`cornucopia/events`)
- [x] Pricing/deal fields (R-9.1, R-9.2) — absorbed into Phase 3
- [x] CTA config fields (R-9.4, R-9.5) — absorbed into Phase 3
- [x] Reporting counters/fields (R-10.1, R-11.5, R-11.7, R-12.1) — absorbed into Phases 4–5
- [x] Mailing-list contact schema with opt-in status (R-1.2, R-1.3) — absorbed into Phase 2

## Phase 1 — Admin Portal Restructure ✅

- [x] Build the new **Manage Uploads** page: "My Uploads" look/state + Approve/Reject/Push-to-App table functionality (R-2.1) — reused the existing Uploads screen/table for admins rather than a duplicate screen; nav button + breadcrumb relabel to "Manage Uploads" for admin role
- [x] Reduce admin left nav to three items (R-2.2) — Dashboard, Manage Uploads, Manage Partner
- [x] Add **Manage Partner** nav item hosting the Partner Subscriptions screen (R-3.1) — moved out of the Uploads screen into its own admin-only screen; no JS changes needed since the existing logic was already keyed by element ID, not by which screen contained it
- [x] Reorder tabs so **Pending** comes before **Approved** (R-2.3)
- [x] Route "new partner upload" notifications directly to the Pending tab (R-2.4)
- [x] Add Approve / Reject / Push to App buttons inside the product detail page, below the thumbnail (R-2.5)
- [x] Build category management screen for admins (R-4.1) — scoped to portal-only per decision below
- [~] Enforce: categories with 0 models are hidden from the mobile app (R-4.2) — **scope decision:** the app has no catalog-browse screen at all (users only ever see models explicitly assigned to them), so there's nowhere for an empty category to "show" today. Implemented the portal-side equivalent instead: the category filter dropdown in Manage Uploads only lists categories with ≥1 model. Revisit for real if a catalog-browse screen is ever built in the app.

Epics 2, 3, and 4 shipped 2026-08-29 (all deployed): the old separate "Manage Uploads" / Pending Tasks screen was removed entirely — its approve/reject/push table now lives inside the same screen as "My Uploads" (relabeled per role), and the same three buttons were added to the model detail page. Both places share one click handler so they can't drift out of sync. Partner Subscriptions now has its own "Manage Partner" nav item, landing the admin nav at exactly three items. Category management (admin CRUD on `cornucopia/categories`, category assignment on the model detail page, category filter on Manage Uploads) is portal-only for now — Phase 1 is complete.

## Phase 2 — Partner Mailing List ✅ (v1 shipped 2026-08-28)

- [x] Build partner-facing mailing list upload flow (R-1.1) — CSV upload under new "Mailing List" nav item
- [x] Build welcome email with Opt In / Decline links (R-1.2) — via EmailJS (client-side) + new `optin.html` landing page
- [x] Track opt-in/decline status per contact (R-1.3) — `pending | opted_in | declined` on each mailing-list entry
- [x] Enforce 1,000-contact free cap (R-1.4) — soft block with a "contact us to upgrade" message; **no real payment flow built** (OQ-1 still open, deferred by design)

Known v1 limitations to revisit later: EmailJS free-tier rate/volume limits make it unsuited to large bulk sends (see web-portal/README.md); no raw CSV file is retained (only parsed rows); suppression list is per-partner, not shared (OQ-2 still open).

## Phase 3 — Partner Monetization Features ✅ shipped 2026-08-29 (deployed; app side pending device test)

- [x] Add pricing + deal-type (radio) fields to the partner portal (R-9.1, R-9.2) — new "Pricing & Deal" card on the model detail page (Brand Value, Price, None/Discount/BOGO radios)
- [x] Add CTA configuration UI to the partner portal (button type + link, or none) (R-9.4, R-9.5) — new "Call to Action" card; link is required and must start with http(s):// unless type is "none"
- [x] Build mobile app deal template(s) reflecting the configured deal type (R-9.3) — Brand Value/Price cards on Product Details now hide individually when unset and render real data instead of static placeholders; deal type shows as a "(Discount)"/"(BOGO)" suffix on the Price caption rather than a separate badge, to avoid reworking the screen's fixed pixel layout
- [x] Build mobile app CTA button rendering, including the no-CTA reflow layout (R-9.5) — the existing stubbed "Buy Now" button is now a real, data-driven CTA (label + `Application.OpenURL(ctaLink)`); when no CTA is configured, Save/Share resize and recenter at runtime to fill the row instead of leaving an empty slot

Both portal open questions this phase depended on are now resolved: **OQ-7** (deal + CTA coexistence) — yes, they're independent and can both show at once, confirmed directly by the meeting-notes mockup showing Brand Value/Price *and* a Buy Now button together. **OQ-8** (CTA link validation) — light touch, must start with `http://` or `https://`, nothing stricter.

Like Description, pricing/CTA edits only write to the submission and take effect on the live app after the next Approve/Push — not instantly. This isn't a shortcut, it's consistency with how every other partner-editable field already behaves, and (unlike Category, which is admin-only) it avoids a database-rules change, since partners have no write access to the published `cornucopia/models/{modelKey}` record.

Requires a **Build Product Details Screen** (or Build All Screens) rebuild in the Unity Editor before testing on device — this is a structural scene change (renamed `BuyButton` → `CtaButton`, dynamic Brand Value/Price cards), not just a script recompile.

## Phase 4 — Mobile App Chrome & UX Polish ✅ shipped 2026-08-29 (deployed; app side pending device test)

- [x] Add title bar to the Question screen (R-6.1) — new top title, dynamic "Question X of Y" from SurveyFlow
- [x] Add title bar to the Rating screen (R-6.2) — relocated the existing "Rate this Product" text from inside the content sheet up to the standard top-title position (it was already the de facto title, just buried)
- [x] Display view/save counts on the product card (R-10.1) — new `Views`/`Saves` fields on `ProductInfo` (parsed from `data/opens`/`data/saved`, already-existing aggregate counters that just weren't surfaced to the app before), shown on the Collectibles card next to the rating chip, hidden entirely when both are zero
- [x] Replace "Recently Added" with a 24-hour countdown timer on notifications (R-13.1) — turns out "Recently added" was a literal static placeholder baked into the notification card template that nothing ever set; now a live countdown from a new `assignedAt` timestamp (added at all 4 places the portal assigns a model to a user), ticking down once per second, showing "Expired" past 24h, cleared entirely on legacy assignments with no timestamp to count from

## Phase 5 — Engagement Tracking (Instrumentation) ✅ shipped 2026-08-29 (deployed; app side pending device test)

- [x] Implement share-event logging + entities-shared-with capture (R-5.1, R-5.2) — R-5.1 fully done (every share from Product Details/AR logs a timestamped event); R-5.2 scoped down to count+timing only, see REQUIREMENTS.md
- [x] Implement Product Detail dwell-time tracking (R-12.1) — timestamp captured in `Start()`, duration logged in `OnDestroy()`; sub-1-second visits are dropped as noise
- [x] Implement CTA click tracking (R-11.7) — logged alongside the existing `Application.OpenURL` call, including which CTA type was clicked

Added a new shared `ModelRepository.LogEventAsync(modelKey, uid, type, extra)` writing to the previously-unused `cornucopia/events` node (it existed in the rules/README since the original MVP but nothing ever wrote to it). Also logs a `"view"` event alongside the existing `data/opens` counter bump in `MarkOpenedAsync` — that counter is a running total with no timestamp, and Phase 6's views-over-time report (R-11.6) needs real per-event timestamps, so this had to land here rather than waiting for the reporting phase itself.

## Phase 6 — Reporting ✅ shipped 2026-08-29 (deployed; app side pending device test) — Chart.js added as a new CDN dependency

- [x] Build per-product report: rating stats (median/avg/SD/min/max/frequency dist.) (R-11.1) — **data gap found and worked around:** `models/{key}/data/rating` is a single overwritten scalar with no history (legacy MVP schema), so real stats are computed by scanning every `users/{uid}/models/{modelKey}/Rating` instead, which does hold one real value per rater
- [x] Build per-product report: question/answer list + per-question answer distribution (R-11.2) — multiple_choice and rating-type questions previously had literal "coming soon"/"not aggregated automatically" stub text; both now show real distributions. Open text now lists actual responses instead of the stub message
- [x] Build horizontal bar graph for multiple-choice answers, with %/raw toggle (R-11.3, R-11.4)
- [x] Add time-spent-interacting metric to the report (R-11.5) — average dwell time from Phase 5's events
- [x] Add views-over-time (day/time) graph (R-11.6) — combined into one Views/CTA-clicks/Shares timeline chart per product
- [x] Add CTA-click timeline to the report (R-11.7)
- [x] Link partner's product interactions into their own analytics dashboard (R-8.1) — the whole per-product report lives on the model detail page, which partners already reach for their own submissions; no separate dashboard screen was needed
- [x] Build Partner Report sharing section: cumulative shares, share timeline (R-14.1, R-14.2) — see "Report page" note below
- [x] Build share histogram (binned shares-per-user on X, user count on Y) (R-14.3) — bins follow the requirement's own example (0, 1-10, 11-20, ...); shown on both the admin Manage Partner screen and the partner's own Report page
- [x] Add admin drill-down from a histogram bin to its user list (R-14.4) — admin-only; click a bar to list the users in that bin with their share count. The partner's own Report page shows the same histogram shape but deliberately without drill-down to other users' identities (the requirement framed the drill-down as an admin action, not something to expose to partners)

Found and removed genuinely dead code along the way: `openModelAnalytics()` and the whole `analyticsModal` (stat cards, Yes/No bar, per-question section) were fully built but called from nowhere in the UI — superseded at some point by the model detail page's own stats row. Deleted rather than left as confusing unreachable code (confirmed with the user before removing).

**Report page (added 2026-08-29, after user feedback on the first pass):** the partner Dashboard originally got Opens/Saves/Shares stat cards plus an inline "Your Sharing Activity" chart bolted directly onto it — all four cards pointed at the same destination when clicked, and the sharing content cluttered the page. Restructured per explicit request: Dashboard now has just Total Uploads, Approved, and a single **Report** tile; a new **Report** nav item (partner-only) opens a dedicated screen holding Opens/Saves plus the full sharing report (stats, timeline, histogram). The underlying share-report renderer was generalized to take a target uid + a set of element ids + a drilldown flag, so the admin (Manage Partner) and partner (Report) views share one implementation instead of two copies.

**Known scale caveat:** every report here works by fetching the full `cornucopia/events` (and, for ratings/answers, `cornucopia/users`) node client-side and filtering/aggregating in JS — there's no server-side aggregation in Realtime Database. This matches how the rest of the portal already works (e.g. the partner-subscription admin UI does the same full-user-scan), so it's consistent, but it will not scale gracefully to a very large user base or a long-lived events log. Worth revisiting with a real backend aggregation step (Cloud Functions, BigQuery export, etc.) if usage grows.

## Phase 7 — Strategic / Parked

- [ ] Scope "Cornucopia Store" baseline measurement once Phase 6 reporting exists — planning conversation only, no build yet (Epic 15)

---

## Open Questions Carried Forward

The original pre-build blockers are resolved (nav composition, deal + CTA coexistence, `question_answers`). Still open, none blocking: mailing-list overage payment, per-partner vs. shared suppression list, and in-page action-button behavior after approval. See REQUIREMENTS.md.
