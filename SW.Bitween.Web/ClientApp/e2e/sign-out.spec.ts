import { expect, test, type Page } from "@playwright/test";
import { ADMIN_EMAIL, ADMIN_PASSWORD } from "./helpers";

/**
 * Signing out, and the four ways it used to go wrong.
 *
 * The session lives in four places — a row in the database, the HttpOnly refresh
 * cookie, the Jwt in localStorage, and React's own copy. Only the last one decides
 * what you see, and it used to be the one thing a sign-out could fail to clear:
 * `signOut` awaited the server first, so any failure threw before the session was
 * ended and left the whole app on screen with the Jwt already deleted. Every test
 * here is a way that could happen.
 */

const submitCredentials = async (page: Page) => {
  await page.fill("#login-email", ADMIN_EMAIL);
  await page.fill("#login-password", ADMIN_PASSWORD);
  await page.getByRole("button", { name: /^Sign in$/ }).click();
};

async function signIn(page: Page) {
  await page.goto("login");
  await submitCredentials(page);
  await page.waitForURL((u) => !u.pathname.endsWith("/login"), { timeout: 15000 });
}

const signOut = async (page: Page) => {
  await page.getByRole("button", { name: "Account menu" }).click();
  await page.getByRole("button", { name: /Sign out/ }).click();
};

/** The signed-in shell: present only while React holds a session. */
const shell = (page: Page) => page.getByRole("button", { name: "Account menu" });

test("the login page appears without waiting for the server", async ({ page }) => {
  await signIn(page);
  await page.route("**/api/accounts/logout", async (r) => {
    await new Promise((res) => setTimeout(res, 3000));
    await r.continue();
  });
  const started = Date.now();
  await signOut(page);
  // Held for 3s on purpose: the session ends locally first, so nothing waits on it.
  await page.waitForURL(/\/login/, { timeout: 2500 });
  expect(Date.now() - started).toBeLessThan(2500);
});

test("a refused sign-out still signs you out", async ({ page }) => {
  await signIn(page);
  await page.route("**/api/accounts/logout", (r) => r.fulfill({ status: 500, body: "boom" }));
  await signOut(page);
  await page.waitForURL(/\/login/, { timeout: 5000 });
  expect(await page.evaluate(() => localStorage.getItem("access_token"))).toBeNull();
});

test("an unreachable backend still signs you out", async ({ page }) => {
  await signIn(page);
  await page.route("**/api/accounts/logout", (r) => r.abort("connectionrefused"));
  await signOut(page);
  await page.waitForURL(/\/login/, { timeout: 5000 });
});

test("signing out in one tab ends the session in the other", async ({ page, context }) => {
  await signIn(page);
  const other = await context.newPage();
  await other.goto("https://localhost:7155/partners");
  await other.waitForTimeout(2000);
  expect(await shell(other).count()).toBe(1);

  await signOut(page);
  await page.waitForURL(/\/login/, { timeout: 5000 });
  // Both credentials are shared by every tab, so the session really has ended here
  // too — this tab just never used to find out until someone pressed refresh.
  await other.waitForURL(/\/login/, { timeout: 8000 });
});

test("a sign-out that never answers does not block signing back in", async ({ page }) => {
  await signIn(page);
  await page.route("**/api/accounts/logout", () => {
    /* never fulfilled: the request hangs rather than failing */
  });
  await signOut(page);
  await page.waitForURL(/\/login/, { timeout: 5000 });

  // A sign-in cancels the pending sign-out rather than waiting for it. Waiting was
  // the obvious guard against the race in the next test, and would have deadlocked
  // here for as long as the request hung.
  await submitCredentials(page);
  await page.waitForURL((u) => !u.pathname.endsWith("/login"), { timeout: 8000 });
  expect(await shell(page).count()).toBe(1);
});

test("a slow sign-out response cannot wipe the session that replaced it", async ({ page }) => {
  await signIn(page);
  // The logout response carries `Clear-Site-Data: "cookies", "storage"`, which the
  // browser applies to the whole origin whenever it lands — including over a newer
  // sign-in. Cancelling the request means the response never arrives.
  await page.route("**/api/accounts/logout", async (r) => {
    await new Promise((res) => setTimeout(res, 4000));
    await r.continue();
  });
  await signOut(page);
  await page.waitForURL(/\/login/, { timeout: 5000 });
  await submitCredentials(page);
  await page.waitForURL((u) => !u.pathname.endsWith("/login"), { timeout: 10000 });

  await page.waitForTimeout(5000); // outlast the delayed response
  expect(page.url()).not.toContain("/login");
  expect(await page.evaluate(() => localStorage.getItem("access_token"))).not.toBeNull();
  await page.goto("partners");
  await page.waitForTimeout(1500);
  expect(await shell(page).count()).toBe(1);
});

test("a slow session read cannot flash the app back after a sign-out", async ({ page, context }) => {
  await signIn(page);

  // Held open so it lands after the sign-out below. It returns 200 — the Jwt went
  // out before the sign-out and is still valid — so its result must be discarded on
  // arrival rather than trusted, or the app appears again over a dead session.
  await page.route("**/api/accounts/profile", async (r) => {
    await new Promise((res) => setTimeout(res, 4000));
    await r.continue();
  });
  const reloading = page.reload();
  await page.waitForTimeout(800);

  const other = await context.newPage();
  await other.goto("https://localhost:7155/partners");
  await other.waitForTimeout(1500);
  await signOut(other);
  await other.waitForURL(/\/login/, { timeout: 8000 });

  let everSignedIn = false;
  for (let i = 0; i < 60; i++) {
    if (await shell(page).count()) everSignedIn = true;
    await page.waitForTimeout(100);
  }
  await reloading.catch(() => {});
  expect(everSignedIn).toBe(false);
  expect(page.url()).toContain("/login");
});

test("a session that ended server-side does not need a refresh", async ({ page }) => {
  await signIn(page);
  // Exactly what a sign-out elsewhere leaves behind: no cookie, a useless Jwt.
  await page.context().clearCookies();
  await page.evaluate(() => localStorage.setItem("access_token", "dead"));
  await page.getByRole("link", { name: /^Partners$/ }).click();
  await page.waitForURL(/\/login/, { timeout: 10000 });
});
