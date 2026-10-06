# Ada-nCoa Design Language

> Server-rendered Bootstrap PWA for redistributing food people would otherwise throw away, used mostly on phones by donors and receivers in Romania.

**Coverage note:** this file reconciles two sources, both read on 2026-09-28: the shipped code (`wwwroot/css/tokens.css`, `wwwroot/css/site.css`, the Razor views under `Pages/`) and the actual "Ada-nCoa — PWA UI/UX" design canvas (48 artboards covering every screen, plus a `Fundatii.dc.html` foundations/components board) that `.claude/rules/ui.md` names as visual source of truth. Where they agree, that's stated as settled. Where they diverge, see **"Design canvas vs. shipped app — gaps"** near the end — those are real drift, not documentation noise, and are listed for a decision, not fixed silently here.

## 1. Visual Theme

**Theme statement:** trustworthy, warm, unfussy.

**Character:** A civic/nonprofit utility app, not a marketplace — the tone should read like a well-run community bulletin board, not a storefront. Deep green (trust, food, "give back") carries almost all brand weight; a single orange accent is used sparingly as a spark of warmth, never as a primary action color. Generous pill shapes and rounded cards throughout keep the tone soft and approachable rather than corporate.

**References:** Bootstrap 5's default component shapes (unstyled otherwise), civic-utility apps (transit/city-services apps — clear status, low ornamentation), Plus Jakarta Sans as a geometric-but-friendly display face.

## 2. Color Palette

All colors are CSS custom properties in `wwwroot/css/tokens.css`, plus Bootstrap variable overrides so stock Bootstrap classes (`.btn-success`, `.text-success`, `.bg-success`, links) inherit the brand automatically. **Never hardcode a hex value in a view or inline style — reference a token, per `.claude/rules/ui.md`.**

### Canonical palette (from `Fundatii.dc.html`, the design canvas's foundations board)
This is the named palette the design canvas itself documents — the ground truth. Code tokens below map onto it; **rows marked "missing" have no corresponding `tokens.css` variable today.**

| Name (canvas) | Hex | Code token | Status |
|---|---|---|---|
| Emerald 700 · primar | `#047857` | `--dmd-green` | ✓ matches |
| Emerald 500 | `#10B981` | — | **missing** — no token; not found anywhere in `tokens.css`/`site.css` |
| Mint 50 | `#E8F7EF` | `--dmd-green-soft` | ✓ matches |
| Grey 50 | `#F6F7F5` | `--dmd-bg` (`#F7F9F8`) | **drift** — close but not identical; pick one and update the other |
| White · fundal | `#FFFFFF` | (literal `#fff`/`white`) | ✓ |
| Ink · text | `#111814` | `--dmd-ink` | ✓ matches |
| Grey 600 | `#4A5550` | `--dmd-ink-soft` | ✓ matches |
| Grey 100 · fill | `#F1F3F2` | `--dmd-surface-soft` | ✓ matches |
| Orange · urgență | `#F97316` | `--dmd-accent` | ✓ matches value — **but see the contrast note below: the canvas calls this "urgency," the code only uses it decoratively** |
| Orange bg | `#FFF1E6` | — | **missing** — no soft-orange token exists |
| Red · eroare | `#C0262D` | — | **missing** — the app has no error/danger override; `alert-danger`/`btn-outline-danger` currently render in Bootstrap's stock red, not this one |

(`--dmd-border` `#ECEFED` and `--dmd-ink-faint` `#5F6A64` are also in the canvas's swatches/captions but weren't given standalone names on the foundations board — treating them as settled.)

### Brand tokens
| Token | Value | Role | Used |
|-------|-------|------|------|
| `--dmd-green` | `#047857` | Primary brand color. Maps to `--bs-primary` and `--bs-success`. Buttons, links, focus rings, "Available" status. | High |
| `--dmd-green-dark` | `#036b4d` | Hover/active state for brand green (buttons, links). | Med |
| `--dmd-green-soft` | `#e8f7ef` | Soft brand surface — tinted backgrounds behind green content. | Low |
| `--dmd-accent` | `#f97316` | Decorative accent only — currently just the hyphen in the brand mark. **Do not use for text or button fills** (see contrast note below). | Low |

### Neutrals / ink scale
| Token | Value | Role |
|-------|-------|------|
| `--dmd-ink` | `#111814` | Primary text, headings. |
| `--dmd-ink-soft` | `#4a5550` | Secondary text. |
| `--dmd-ink-faint` | `#5f6a64` | Tertiary/helper text — use sparingly (see contrast note). |
| `--dmd-border` | `#ecefed` | Hairline borders, dividers. Not for text. |
| `--dmd-surface-soft` | `#f1f3f2` | Muted card/section backgrounds. |
| `--dmd-bg` | `#f7f9f8` | Page background. |

### Status badges (semantic, via Bootstrap's `text-bg-*`)
`DonationStatus.ToBadgeClass()` (`Infrastructure/DisplayExtensions.cs`) is the single source of truth — don't duplicate this mapping elsewhere:

| Status | Class | Renders as |
|--------|-------|-----------|
| Available | `text-bg-success` | Brand green (overridden) |
| Reserved | `text-bg-warning` | Bootstrap default amber |
| Completed | `text-bg-primary` | Brand green (overridden — same as Available; the label text is what distinguishes them) |
| Cancelled | `text-bg-secondary` | Bootstrap default gray |
| Expired | `text-bg-dark` | Near-black |

### Contrast audit (WCAG 2.2, computed 2026-09-28)

| Pair | Ratio | Verdict |
|------|-------|---------|
| White text on `--dmd-green` (`.btn-success`) | 5.48:1 | AA ✓ |
| White text on `--dmd-green-dark` (hover) | 6.53:1 | AA ✓ |
| `--dmd-ink` on `--dmd-bg` / white | 17–18:1 | AAA ✓ |
| `--dmd-ink-soft` on `--dmd-bg` / white | 7.3–7.8:1 | AAA ✓ |
| `--dmd-ink-faint` on `--dmd-bg` / white | 5.3–5.6:1 | AA ✓ (fine for normal text, but has no headroom — don't pair it with anything lighter than white/`--dmd-bg`) |
| `--dmd-green` text on white (links) | 5.48:1 | AA ✓ |
| `--dmd-green` on `--dmd-green-soft` (badges) | 4.96:1 | AA ✓ |
| **`--dmd-accent` on white, either direction** | **2.80:1** | **FAIL** — below the 4.5:1 (text) and 3:1 (large text/UI) thresholds |
| `--dmd-border` on white | 1.16:1 | Expected — it's a hairline divider, not a text/UI-boundary color; don't use it where 3:1 is required (e.g. a default input border relying on it alone) |

**The real finding:** the design canvas names `--dmd-accent` (`#f97316`) "Orange · urgență" — it's meant to signal urgency (e.g. an expiring-soon badge), not just decorate the brand mark. But it fails AA badly as a text or solid-fill color (2.80:1), so an "expires today" badge in solid orange-on-white or white-on-orange would ship an accessibility bug. The canvas doesn't appear to actually use solid orange for the expiration indicator either (see the Gaps section) — treat this as an open design question, not a green light to add orange text/badges as-is. If an urgency color is needed, either darken it until it clears 4.5:1, or use it as a fill only where an icon/border 3:1 threshold applies, or use `#FFF1E6` (the canvas's "Orange bg") as a soft background with `--dmd-ink` text and orange only as an accent stripe/icon.

## 3. Typography

**Font family:** `'Plus Jakarta Sans', system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif` — loaded from Google Fonts (weights 400/500/600/700/800), token `--dmd-font`, applied to `--bs-body-font-family` so it's the default everywhere.

**Headings:** all `h1`–`h6` (and `.h1`–`.h6`) are weight **800**, `letter-spacing: -0.01em`. No separate display/heading typeface — weight and size carry the hierarchy.

**Scale:** the design canvas *does* define one (`Fundatii.dc.html`), it's just not yet formalized as CSS in the shipped code — views currently hit these sizes with ad-hoc utility classes / inline sizing rather than named tokens. Worth turning into real classes or CSS custom properties in `tokens.css` next time typography work is touched:

| Role | Size | Weight | Line height | Color |
|------|------|--------|-------------|-------|
| Page title | 26px | 800 | 32px | `--dmd-ink` |
| Section | 18px | 800 | 24px | `--dmd-ink` |
| Card title | 15px | 700 | 21px | `--dmd-ink` |
| Body | 15px | 400 | 21px | `--dmd-ink` |
| Meta | 13px | 500 | 18px | `--dmd-ink-soft` |

This is close to but not identical to the global `h1`–`h6` rule in `site.css` (weight 800 for all heading levels) — the canvas's scale implies only "Page title" and "Section" are meant to be that heavy; card titles are 700, not 800.

## 4. Components

| Component | Hierarchy | Purpose | Key variants / notes |
|-----------|-----------|---------|----------------------|
| `.btn` | Primitive | All buttons — 44px min-height, pill radius, bold. Meets the 44×44px tap-target rule by default. | `btn-success`/`btn-outline-success` (primary), `btn-outline-danger` (cancel/release actions), `btn-outline-secondary`, `btn-secondary`, `btn-link`, `btn-chip` / `btn-chip-cta` (pill filter chips), `btn-logout` |

**Button hierarchy per the design canvas** (`Fundatii.dc.html`) — 4 treatments, not just success/outline:
| Treatment | Example | Background | Text | Border |
|---|---|---|---|---|
| Primary | "Rezervă" | `#047857` | white | `1.5px solid #047857` |
| Secondary | "Detalii" | `#F1F3F2` (grey fill) | `--dmd-ink` | `1.5px solid #F1F3F2` |
| Tertiary / text | "Renunță" | transparent | `#047857` | `1.5px solid transparent` |
| Destructive | "Anulează anunțul" | `#FEECEC` (soft red fill) | `#C0262D` | `1.5px solid #FEECEC` |
| Disabled | "Indisponibil" | `#F1F3F2` | `--dmd-ink-faint` | `1.5px solid #F1F3F2` |

**Gap:** the shipped code's destructive actions (`btn-outline-danger` — used for "Anulează rezervarea") render as a red-outlined button on a white fill, which is Bootstrap's stock outline-danger, not the canvas's soft-red-fill treatment above. Cosmetic drift, not a bug — flagging for a deliberate decision, not fixing silently.
| Status badge (`_StatusBadge.cshtml`) | Component | Donation status pill | See §2 status table — always go through `ToBadgeClass()`/`ToLabel()`, never inline |
| `.card` / `.donation-card` | Component | Feed/list item card with image, title, status | `.donation-card-img`, hover elevation (shadow deepens on hover) |
| `.donation-detail-img`, `.donation-thumb` (+`-wide`) | Component | Donation photo display at different sizes | |
| `.image-placeholder` | Component | Empty/no-photo state for a donation | |
| `.empty-state` | Pattern | "Nothing here" state for lists | Per `.claude/rules/ui.md`, every empty state needs a next action — audit these when touched |
| `.form-card`, `.auth-card` | Pattern | Card wrapper around a form (auth pages, donation form) | |
| `.form-label`, `.form-select`, `.form-text`, validation summary/`input-validation-error` | Primitive/Component | Form controls — 16px+ font size enforced (no iOS zoom), custom green/red focus rings (`box-shadow: 0 0 0 4px rgba(...)`) | |
| `.segmented` | Component | Segmented toggle control | Pill-shaped, 999px radius |
| `.location-bar` | Component | City/neighborhood filter bar | |
| `.bottom-nav` | Template zone | Fixed bottom tab bar, mobile nav | Respects `env(safe-area-inset-bottom)` — required per `.claude/rules/ui.md`; body gets `.has-bottom-nav` when authenticated |
| `.hero`, `.hero-icon`, `.step-number` | Component | Landing/onboarding sections | Responsive padding change at 768px |
| `.icon-circle` (+`-neutral`, `-brand`) | Primitive | Circular icon badge | |
| Wizard (`.wizard-shell`, `-header`, `-progress[-bar/-seg]`, `-step-label/-intro`, `-check`, `-actions`, `-review-*`, `-note`, `-close`) | Template | Multi-step donation-posting flow | Largest custom component family — treat as a template, not a one-off; reuse before inventing a new stepper pattern |
| Alerts | Primitive (Bootstrap) | Status messages via `_StatusMessages.cshtml` (`TempData`) | `alert-success`, `alert-danger`, `alert-secondary`, `alert-dark`, `alert-light`, `alert-dismissible` all in use — no custom override beyond `.alert-success` |

**Missing states worth checking when you touch these:** loading state for the donation-post wizard (no spinner pattern found in site.css), disabled/loading state on `.btn` beyond native `:disabled`.

## 5. Layout

**Grid:** Bootstrap's standard 12-column grid via `.container`/`.row`/`.col-*` — no custom grid system.

**Base unit:** Bootstrap's default spacer scale (0, 4px, 8px, 16px, 24px, 48px) accounts for most spacing (`.5rem`, `1rem`, `1.5rem`, `3rem` appear repeatedly). Component-level gaps use finer one-off values (`.35rem`–`.75rem`) for icon/label spacing — these are fine as component-scoped tweaks, just don't invent new page-level spacing values outside the Bootstrap scale.

**Radius:** two tokens cover everything —
- `--dmd-radius` (1.25rem / 20px): cards, wizard shell, hero, large surfaces
- `--dmd-radius-sm` (.875rem / 14px): default Bootstrap `--bs-border-radius` (inputs, small cards)
- Pills: 999px (buttons, badges, chips, segmented control) — also set as `--bs-border-radius-pill`

**Breakpoints:** Bootstrap's stock breakpoints (`sm` 576, `md` 768, `lg` 992, `xl` 1200) via utility classes in views. Only one custom breakpoint exists in `site.css` itself: `@media (min-width: 768px)`, used twice (hero padding, one other section) — the app is otherwise mobile-first by default rather than by extensive media-query overrides. **Per `.claude/rules/ui.md`: check 360px / 768px / 1280px on every UI change; no horizontal scroll at any width.**

## 6. Depth

| Level | Shadow value | Used for |
|-------|-------------|----------|
| Chip/segment | `0 2px 8px rgba(11,15,13,.10)` | Segmented control, small floating chips |
| Card (rest) | `0 1px 2px rgba(16,24,20,.04), 0 8px 24px rgba(16,24,20,.06)` | Default `.card` / `.donation-card` elevation |
| Card (hover) | `0 4px 10px rgba(16,24,20,.06), 0 12px 28px rgba(16,24,20,.10)` | Hover state — deepens on interaction, paired with a `transform`/`box-shadow` transition |
| Brand-colored | `0 8px 20px rgba(4,120,87,.35)` | Elevated brand-colored surface (e.g. a prominent CTA) |
| Focus ring — success | `0 0 0 4px rgba(4,120,87,.15)` | Default focus / valid input state |
| Focus ring — danger | `0 0 0 4px rgba(192,38,45,.12)` | Invalid input state |

No formal z-index scale is documented in `site.css` beyond the skip-link (`z-index: 2000`) and the bottom nav being fixed — if stacking bugs show up (dropdown under bottom-nav, etc.), that's the first place to look and a good candidate for a real scale.

## 7. Do's and Don'ts

### Do
- Reference tokens from `tokens.css` (`--dmd-*`) and Bootstrap's brand-overridden variables — never a raw hex value in a view or inline style.
- Reuse `.donation-card`, `.form-card`/`.auth-card`, the wizard component family, and `_StatusBadge.cshtml` before inventing new equivalents.
- Route every donation-status color/label through `ToBadgeClass()`/`ToLabel()` in `DisplayExtensions.cs` — never hardcode a status→class mapping in a view.
- Keep buttons at the 44px min-height / pill shape defined on `.btn`; don't override height down for density.
- Use Bootstrap's spacer scale (`.25rem`/`.5rem`/`1rem`/`1.5rem`/`3rem` family) for page-level spacing; fine one-off gaps are fine only at the component level (icon-to-label, etc.).
- Keep all user-facing copy in Romanian with correct diacritics (ă, â, î, ș, ț — comma-below, not cedilla ş/ţ); keep code/class/DB terms in English.

### Don't
- Don't use `--dmd-accent` as a text color or a solid button/alert fill — it fails WCAG AA contrast (2.80:1) against both white and dark text at normal sizes. Decorative/icon use only, or pair with a manual contrast check.
- Don't add a second UI framework or component library (Tailwind, React, Vue, Angular, PrimeVue, shadcn, jQuery plugins) — Bootstrap-only, per `.claude/rules/ui.md`.
- Don't load a second copy or a different version of Bootstrap than the one pinned in `_Layout.cshtml` (currently 5.3.3 / Bootstrap Icons 1.11.3).
- Don't rely on `--dmd-border` (1.16:1 against white) anywhere a 3:1 UI-boundary contrast is required — it's a hairline divider, not a functional border color.
- Don't let user-entered or long Romanian text overflow — wrap it (`text-break` where needed); no horizontal scroll at any width.
- Don't skip the empty/loading/error state when touching a page that has one — `.empty-state` needs a next action, not just a message.

## 8. Responsive

**Strategy:** Mobile-first, 360px baseline (per `.claude/rules/ui.md`), Bootstrap's grid/utilities for breakpoint changes, one custom `@media (min-width: 768px)` override for hero spacing. The signature mobile pattern is the fixed **bottom nav** (safe-area-aware) for authenticated users, replaced by the top navbar-only layout for anonymous visitors.

| Element | Mobile (< 768px) | Desktop (≥ 768px) |
|---------|-------------------|---------------------|
| Primary navigation | Fixed `.bottom-nav` tab bar (authenticated) | Top navbar; bottom nav still present unless explicitly hidden |
| Hero section | `padding: 3rem 1.5rem` | `padding: 4.5rem 3rem` |
| Donation feed | Single column cards | Bootstrap grid columns (via `.row`/`.col-*` in views) |

## 9. Agent Prompt Guide

**When generating or editing UI for this project, agents must:**
- Read `.claude/rules/ui.md` first — it is the binding rule set for any `.cshtml`, `wwwroot/css/**`, or `wwwroot/js/**` change; this file explains *why* those rules exist and what they currently produce.
- Reference tokens from `tokens.css` and Sections 2–6 above; never invent a new hex/px value inline.
- Prefer Bootstrap utility classes; add custom CSS in `site.css` only when Bootstrap genuinely can't do it, following existing component naming (`.donation-*`, `.wizard-*`, `.form-*`).
- Check 360px / 768px / 1280px before calling any UI change done, per `.claude/rules/ui.md`'s pre-finish checklist.
- No `dotnet build`/`dotnet ef` tooling is available when working through the cloud session on this repo — structural correctness (brace/tag balance) is verified by inspection, not compilation. Flag risk level explicitly in every commit that touches `.cshtml`/`.cs`.

**Invariants — don't change without updating this file:**
- Brand color is `--dmd-green` (`#047857`); it drives both `--bs-primary` and `--bs-success`.
- `--dmd-accent` is decorative-only until someone deliberately re-audits it for contrast in a specific new use.
- Status colors/labels come only from `DonationStatus.ToBadgeClass()`/`ToLabel()`.
- Buttons are always ≥44px tall and pill-shaped.

**Open questions / known gaps:**
- The "Ada-nCoa — PWA UI/UX" design canvas is now reconciled (2026-09-28) — see "Design canvas vs. shipped app — gaps" below for what still diverges. Re-run reconciliation if the canvas gets a significant update.
- No documented type scale, elevation z-index scale, or dark mode — define these here before the codebase grows enough for inconsistency to appear.
- No motion tokens — the only transition in the CSS today is the card hover shadow (`transition: transform .15s ease, box-shadow .15s ease`); formalize as a scale if more animation gets added.

## Design canvas vs. shipped app — gaps

The design canvas (48 artboards, read 2026-09-28) covers considerably more than what's built. None of this is "wrong" — it's the backlog the canvas represents. Listed for a decision, not actioned here.

**Token gaps** (see §2 for detail): `Emerald 500` `#10B981` unused/undefined, `Grey 50` `#F6F7F5` vs. the shipped `--dmd-bg` `#F7F9F8` drift, `Orange bg` `#FFF1E6` and `Red · eroare` `#C0262D` (+ its soft `#FEECEC`) have no code tokens — the app currently borrows Bootstrap's stock danger red instead of the canvas's own.

**Button gap** (see §4): destructive actions are styled as Bootstrap's outline-danger in code vs. a soft-red-fill pill in the canvas.

**Screens designed but not built:**
- `Filtre.dc.html` ("Acasă — Filtre deschise") — a filter bottom-sheet. Today the feed only has the plain search box (the search icon was deliberately removed earlier this project).
- `AnuntPublicat.dc.html` ("Anunț publicat") — a dedicated "donation published" success screen after the posting wizard. Today the wizard redirects straight back to the feed with a TempData banner.
- `DesktopAcasa.dc.html` / `DesktopDonatii.dc.html` / `DesktopRezervari.dc.html` (1440px layouts) — the canvas has purpose-built desktop compositions; the shipped app relies on Bootstrap's grid reflowing the mobile layout rather than a distinct desktop design.
- `StariSiErori.dc.html` — a documented library of empty states, error states, and toasts. The app has `.empty-state` and `_StatusMessages.cshtml`, but hasn't necessarily implemented every state the canvas designs (e.g. a loading/skeleton state — `Incarcare.dc.html` — wasn't found as a pattern in `site.css`).

**Richer status set:** the canvas's foundations board lists reservation-adjacent statuses beyond the current 3 (`Confirmată`, `Pregătit pentru ridicare`, `Finalizată`, `Anulată`, `Expirată`) plus what looks like a moderation flow (`În verificare`, `Respins`, `În așteptare`) that doesn't exist in the current `DonationStatus`/reservation model at all. This is a bigger product question (should donations be moderated before going live?) — not something to infer and build from a design file alone.

---

## Voice & Tone

**Voice:** plain, warm, direct — a neighbor explaining how this works, not a brand.

**Terminology** (code/DB stays English regardless of UI copy):
| Concept | Code/DB term | Romanian UI term |
|---------|-------------|-------------------|
| Person giving food | `Donator` | Donator / Donatoare |
| Person receiving food | `Receiver` | Primitor / Primitoare |
| A posted food item | `DonationItem` | Donație |
| A claim on a donation | `Reservation` | Rezervare |

**Microcopy:** sentence case for buttons and headings (see existing labels — "Anulează rezervarea", "Confirmă predarea"); diacritics are mandatory and must use the comma-below forms (ș, ț), never cedilla (ş, ţ) — this is enforced by `.claude/rules/ui.md` and should be checked whenever new copy is written or edited, including by hand in `UiText.cs`.
