import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api, TOKEN_KEY, onSessionEnded, type PermissionKey, type Session } from "../api";
import { useIdleLogout } from "./useIdleLogout";

interface SessionContextValue {
  session: Session | null;
  /** True until the stored session has been checked once at startup. */
  initializing: boolean;
  can: (permission: PermissionKey) => boolean;
  signIn: (email: string, password: string) => Promise<Session>;
  signInWithMicrosoft: () => Promise<Session>;
  /** Adopt a session produced elsewhere (invite acceptance, demo switch). */
  adoptSession: (session: Session) => void;
  /** Re-fetch the session after profile changes. */
  refresh: () => Promise<void>;
  signOut: () => Promise<void>;
}

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [initializing, setInitializing] = useState(true);
  const queryClient = useQueryClient();

  useEffect(() => {
    api
      .getSession()
      .then(setSession)
      .finally(() => setInitializing(false));
  }, []);

  const adoptSession = useCallback(
    (next: Session) => {
      // a different identity invalidates everything previously fetched
      queryClient.clear();
      setSession(next);
    },
    [queryClient],
  );

  const signIn = useCallback(
    async (email: string, password: string) => {
      const next = await api.login(email, password);
      adoptSession(next);
      return next;
    },
    [adoptSession],
  );

  const signInWithMicrosoft = useCallback(async () => {
    const next = await api.loginWithMicrosoft();
    adoptSession(next);
    return next;
  }, [adoptSession]);

  const refresh = useCallback(async () => {
    setSession(await api.getSession());
  }, []);

  /** Ends the session locally, then tells the server. Never the other way round. */
  const endSession = useCallback(() => {
    setSession(null);
    queryClient.clear();
  }, [queryClient]);

  const signOut = useCallback(async () => {
    // The session ends here, whatever the server says. `await api.logout()` used to
    // come first, so any failure — a backend restart, a blip, an unreachable host —
    // threw before the two lines below ran: signed out everywhere except the one
    // place that decides what you see, which then needed a manual refresh. Ending it
    // first also means the login page appears immediately rather than after the
    // round trip, which is what made signing out feel slow.
    endSession();
    await api.logout();
  }, [endSession]);

  /**
   * Ends the session here when it ends in another tab.
   *
   * Both credentials are shared by every tab of this origin — the Jwt in
   * localStorage and the refresh cookie — so a sign-out in one tab really has ended
   * the session in all of them; the others simply never found out and kept rendering
   * the whole app over reads that could only fail. `storage` fires only in the
   * *other* tabs, which is exactly the audience. A null key means the entire store
   * was cleared, which the logout response's `Clear-Site-Data` does, so that counts
   * too; a key with a new value is another tab signing *in*, which doesn't.
   */
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key !== null && e.key !== TOKEN_KEY) return;
      if (e.newValue) return;
      endSession();
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, [endSession]);

  /**
   * Ends the session here when a request proves it is already over.
   *
   * Covers what no `storage` event can: a Jwt and refresh cookie that both expired
   * while a tab sat open, or an administrator disabling the account. `request()`
   * already knew — it writes "Your session has ended" — but it could only throw that
   * at whichever page happened to be asking, and a page's error state cannot sign
   * anyone out.
   */
  useEffect(() => {
    onSessionEnded(endSession);
    return () => onSessionEnded(null);
  }, [endSession]);

  // Finding #4 in the 9USRCraft pen test: an authenticated session stayed usable
  // indefinitely. Signs out after 30 minutes with no activity in any tab.
  useIdleLogout(!!session, signOut);

  const value = useMemo<SessionContextValue>(
    () => ({
      session,
      initializing,
      can: (permission) => session?.permissions.includes(permission) ?? false,
      signIn,
      signInWithMicrosoft,
      adoptSession,
      refresh,
      signOut,
    }),
    [session, initializing, signIn, signInWithMicrosoft, adoptSession, refresh, signOut],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): SessionContextValue {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error("useSession must be used inside <SessionProvider>");
  return ctx;
}
