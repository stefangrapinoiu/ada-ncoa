---
paths:
  - "**/*.cshtml"
  - "wwwroot/css/**"
  - "wwwroot/js/**"
---

# UI rules — Ada-ncoa

These rules apply to every change in Razor views, CSS, or front-end JS.

## Stack (do not change)
- Server-rendered ASP.NET Core Razor Pages, styled with Bootstrap only.
- Do not add Tailwind, React, Vue, Angular, PrimeVue, shadcn, or any other UI/component library.
- Use the Bootstrap version already referenced in the layout; never load a second copy or a different version.
- Use plain JavaScript only where a feature truly needs it. No jQuery plugins or npm packages without asking first.

## Design system
- The Figma design and the approved screen canvas are the visual source of truth.
- Colors, spacing, radii, and fonts come from CSS custom properties in the project's tokens file. If none exists, create `wwwroot/css/tokens.css` and add values there.
- Never hardcode hex colors or pixel values in views or inline styles. Prefer Bootstrap utility classes; add custom CSS only when Bootstrap can't do it.
- Reuse existing partials, view components, and button/badge/card styles before creating new ones. Keep new components visually consistent with the existing ones.

## Mobile-first (the app is a PWA used mainly on phones)
- Build for a 360px-wide screen first, then adjust at Bootstrap's `sm`, `md`, and `lg` breakpoints.
- No horizontal scrolling at any width. Images and media use `img-fluid` or `max-width: 100%`.
- Tap targets are at least 44×44px. Form inputs use a font size of at least 16px so iOS doesn't zoom in.
- Respect the device safe areas (`env(safe-area-inset-*)`) for fixed headers and bottom navigation.
- Long Romanian words and user-entered text must wrap, not overflow (`text-break` where needed).

## Language
- All user-facing text is in Romanian with correct diacritics: ă, â, î, ș, ț (comma-below ș/ț, not cedilla ş/ţ).
- Code, class names, variables, and database terms stay in English: Donator, Receiver, Donation, Reservation.

## Accessibility
- Use semantic HTML (`button` for actions, `a` for navigation, proper headings in order).
- Every input has a `<label>`; every meaningful image has Romanian `alt` text; decorative images use `alt=""`.
- Keep visible focus styles and sufficient color contrast (WCAG AA).

## Before finishing any UI change
1. Check the changed pages at 360px, 768px, and 1280px widths. If a browser tool is available, take screenshots at each width; if not, say the visual check was not done.
2. Confirm no horizontal overflow, no clipped text, and no overlapping elements.
3. Check the empty, loading, and error states of anything you touched.
4. In your summary, list which pages and widths you checked and anything that still looks off.
