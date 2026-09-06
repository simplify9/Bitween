import { useEffect, useRef } from "react";

const IDLE_TIMEOUT_MS = 30 * 60 * 1000; // 30 minutes
const CHECK_INTERVAL_MS = 30 * 1000; // re-check every 30s
const ACTIVITY_EVENTS = ["mousemove", "mousedown", "keydown", "scroll", "touchstart"];
/** Shared so activity in any tab keeps every tab alive (see the interval below). */
const LAST_ACTIVITY_KEY = "last_activity";

const readSharedActivity = (): number => {
  try {
    return Number(localStorage.getItem(LAST_ACTIVITY_KEY)) || 0;
  } catch {
    return 0; // storage unavailable: fall back to this tab's own timer
  }
};

const writeSharedActivity = (ts: number) => {
  try {
    localStorage.setItem(LAST_ACTIVITY_KEY, String(ts));
  } catch {
    // ignore: the in-tab ref still tracks activity
  }
};

/**
 * Signs the user out after a period with no activity in any tab. Runs the normal
 * sign-out path, so an idle session ends exactly like clicking Sign out.
 */
export function useIdleLogout(
  enabled: boolean,
  signOut: () => Promise<void>,
  timeoutMs: number = IDLE_TIMEOUT_MS,
) {
  const lastActivity = useRef<number>(Date.now());
  // Held in a ref so a new signOut identity doesn't restart the idle timer.
  const signOutRef = useRef(signOut);
  useEffect(() => {
    signOutRef.current = signOut;
  }, [signOut]);

  useEffect(() => {
    if (!enabled) return;

    // Start counting from the moment we become enabled. The ref is created when the
    // provider first mounts, which is while the login page is showing and no activity
    // listeners are attached, so without this a login page left open longer than
    // timeoutMs would sign the user out immediately after signing in.
    const enabledAt = Date.now();
    lastActivity.current = enabledAt;
    writeSharedActivity(enabledAt);

    const markActivity = () => {
      const ts = Date.now();
      lastActivity.current = ts;
      writeSharedActivity(ts);
    };
    ACTIVITY_EVENTS.forEach((e) => window.addEventListener(e, markActivity, { passive: true }));

    const interval = window.setInterval(async () => {
      // Activity events only fire in the focused tab, so take the most recent activity
      // across tabs. Otherwise a background tab would sign out a user who is actively
      // working in another one.
      const last = Math.max(lastActivity.current, readSharedActivity());
      if (Date.now() - last < timeoutMs) return;

      window.clearInterval(interval);
      ACTIVITY_EVENTS.forEach((e) => window.removeEventListener(e, markActivity));
      // No retry, and nothing to reload around: `signOut` ends the session before it
      // calls the server and swallows a failed call, so this cannot leave the user
      // looking at the app. It used to need both.
      await signOutRef.current();
    }, CHECK_INTERVAL_MS);

    return () => {
      window.clearInterval(interval);
      ACTIVITY_EVENTS.forEach((e) => window.removeEventListener(e, markActivity));
    };
  }, [enabled, timeoutMs]);
}
