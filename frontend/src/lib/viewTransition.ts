/**
 * Native browser View Transitions API only — no React `<ViewTransition>`, no
 * `experimental.viewTransition` Next config flag. Both are still explicitly experimental/unstable
 * as of Next.js 16; a broken or flickering transition on an unstable API is worse than none on an
 * operational tool used for long sessions. Every entry point here degrades to a plain, instant
 * state change with zero visual difference when the API is unsupported, the user has
 * `prefers-reduced-motion` set, or the document is hidden (backgrounded tab, HMR reload — the
 * browser throws `InvalidStateError` if `startViewTransition` is called while
 * `document.visibilityState !== "visible"`) — same fallback code path for all three.
 */

export function prefersReducedMotion(): boolean {
  if (typeof window === "undefined") return false;
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function canUseViewTransitions(): boolean {
  return (
    typeof document !== "undefined" &&
    "startViewTransition" in document &&
    document.visibilityState === "visible" &&
    !prefersReducedMotion()
  );
}

/**
 * `startViewTransition`'s return value carries `ready`/`finished`/`updateCallbackDone` promises
 * that reject on failure (an aborted transition, a duplicate `view-transition-name` elsewhere on
 * the page, the tab backgrounding mid-transition, ...). Per the "silent fallback is mandatory"
 * rule, a rejection here must never surface as an unhandled promise rejection or break the
 * underlying update, which has already applied by the time any of these settle.
 */
function suppressTransitionRejections(transition: { ready: Promise<void>; finished: Promise<void> }): void {
  transition.ready.catch(() => {});
  transition.finished.catch(() => {});
}

/**
 * Runs `update` (a synchronous DOM/state mutation) inside a view transition when supported,
 * or immediately otherwise. Use this for a transition that doesn't need to wait on React's async
 * commit — e.g. a callback that itself calls `flushSync` (see `MorphingStat`).
 */
export function withViewTransition(update: () => void): void {
  if (!canUseViewTransitions()) {
    update();
    return;
  }
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  suppressTransitionRejections((document as any).startViewTransition(update));
}

/**
 * Wraps a Next.js App Router navigation in a view transition. `router.push` triggers React's
 * async re-render rather than a synchronous DOM mutation, so there is no single synchronous
 * callback we can hand the browser API the way `withViewTransition` does — instead we start the
 * transition, call `push`, and resolve after two animation frames (giving React's commit + paint
 * a chance to land) so the browser captures the "new" state before cross-fading to it. This is a
 * pragmatic, widely-used approximation for React Router integrations, not a guarantee React has
 * fully settled — acceptable here since worst case is a transition that starts a frame late, not
 * a broken one (the underlying navigation itself is unaffected either way).
 */
export function navigateWithViewTransition(push: (href: string) => void, href: string): void {
  if (!canUseViewTransitions()) {
    push(href);
    return;
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const transition = (document as any).startViewTransition(() => {
    push(href);
    return new Promise<void>((resolve) => {
      requestAnimationFrame(() => requestAnimationFrame(() => resolve()));
    });
  });
  suppressTransitionRejections(transition);
}
