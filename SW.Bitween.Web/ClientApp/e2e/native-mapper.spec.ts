import { test, expect, type Page } from "@playwright/test";
import { pickOption, signInAsAdmin } from "./helpers";

/**
 * The new mapping editor, in a real browser against the real backend.
 *
 * The test that matters most is the round trip: build a mapping, save, reload, and
 * find exactly what was built. The old editor could not do that — it saved a
 * generated Scriban template and reverse-engineered the rules back out of it on
 * load, so subtle detail came back changed and nothing said so.
 */

test.beforeEach(async ({ page }) => {
  await signInAsAdmin(page);
});

const SAMPLE = JSON.stringify(
  {
    order: {
      customer: "Ali",
      net: 100,
      line: [
        { sku: "A1", qty: 2 },
        { sku: "B7", qty: 0 },
      ],
    },
  },
  null,
  2,
);

/** Creates a subscription with no mapper, which opens in the new editor. */
async function createSubscription(page: Page): Promise<string> {
  const name = `PW Mapper ${Date.now()}`;

  await page.goto("scheduled-jobs/new");
  await page.fill("#nj-name", name);
  await pickOption(page, "Information type", /Shipment order/);

  await pickOption(page, "receiver adapter", "NativeHttpReceiver");
  await page.locator("#prop-Url").fill("https://example.com/feed");
  await page.getByRole("button", { name: "Close this step" }).click();

  await page.getByRole("button", { name: /^Delivery/ }).click();
  await pickOption(page, "handler adapter", "NativeHttpHandler");
  await page.locator("#prop-Url").fill("https://example.com/post");

  await page.getByRole("button", { name: /^(Create|Save)/ }).last().click();
  await page.waitForURL(/\/subscriptions\/\d+/, { timeout: 15000 });

  return page.url().match(/\/subscriptions\/(\d+)/)![1];
}

async function openMapper(page: Page, subscriptionId: string) {
  await page.goto(`subscriptions/${subscriptionId}/mapper`);
  await expect(page.getByRole("button", { name: "Save" })).toBeVisible({ timeout: 15000 });
}

/** Fills the nth field rule's name and reads its value from a document path. */
async function addPathRule(page: Page, target: string, path: string) {
  await page.getByRole("button", { name: "Field", exact: true }).first().click();

  const nameBox = page.getByRole("textbox", { name: "Output field name" }).last();
  await nameBox.fill(target);

  await page.getByRole("combobox", { name: "Where the value comes from" }).last()
    .selectOption("path");
  await page.getByRole("textbox", { name: "Field path" }).last().fill(path);
}

test("builds a mapping, previews it, saves it, and reloads exactly what was built", async ({
  page,
}) => {
  const subscriptionId = await createSubscription(page);
  await openMapper(page, subscriptionId);

  // ── The source document drives the field list ──────────────────────────────
  await page.getByRole("textbox", { name: "Sample source document" }).fill(SAMPLE);

  // A value is offered; a field inside a list is not, because it is only reachable
  // from a loop over that list.
  await expect(page.getByRole("button", { name: /order\.customer/ })).toBeVisible();
  await expect(page.getByText("list · 2")).toBeVisible();
  await expect(page.getByRole("button", { name: /order\.line\.sku/ })).toHaveCount(0);

  // ── Rules ──────────────────────────────────────────────────────────────────
  await addPathRule(page, "customerName", "order.customer");

  await page.getByRole("button", { name: "Field", exact: true }).first().click();
  await page.getByRole("textbox", { name: "Output field name" }).last().fill("channel");
  await page.getByRole("combobox", { name: "Where the value comes from" }).last()
    .selectOption("fixed");
  await page.getByRole("textbox", { name: "Fixed value" }).last().fill("WEB");

  // A number target with a transform — the case that proves the arithmetic is done
  // on the server with the real value rather than baked into a template.
  await addPathRule(page, "total", "order.net");
  await page.getByRole("combobox", { name: "Transform" }).last().selectOption("multiply");
  await page.getByRole("textbox", { name: /Multiply.*By/ }).last().fill("1.16");
  await page.getByRole("combobox", { name: "Value type" }).last().selectOption("number");

  // ── A loop with a filter ───────────────────────────────────────────────────
  await page.getByRole("button", { name: "List", exact: true }).click();
  await page.getByRole("textbox", { name: "Output list name" }).fill("lines");
  await page.getByRole("combobox", { name: "Source list" }).selectOption("order.line");
  await page.getByRole("checkbox", { name: "Only some entries" }).check();
  await page.getByRole("textbox", { name: "Filter field" }).fill("qty");
  await page.getByRole("combobox", { name: "Filter comparison" }).selectOption("greaterThan");
  await page.getByRole("textbox", { name: "Filter value" }).fill("0");

  // Scoped to the loop's own group. Both the loop and the root have an "Add field"
  // button, and the root's is later in the document — so `.last()` would put the
  // rule at the top level instead of inside the list.
  const linesGroup = page.getByRole("group", { name: "List lines rules" });
  await linesGroup.getByRole("button", { name: "Field", exact: true }).click();
  await linesGroup.getByRole("textbox", { name: "Output field name" }).fill("code");
  await linesGroup.getByRole("textbox", { name: "Field path" }).fill("sku");

  // ── The preview comes from the server ──────────────────────────────────────
  const preview = page.locator("pre").first();
  await expect(preview).toContainText('"customerName": "Ali"', { timeout: 15000 });
  await expect(preview).toContainText('"channel": "WEB"');

  // 100 × 1.16. In binary floating point this is 115.99999999999999, which is why
  // the mapper works in decimal. It arrives as 116 rather than 116.00 because a
  // whole number is written as an integer — an order quantity must not pick up a
  // decimal point the source never had.
  await expect(preview).toContainText('"total": 116,');

  // The filter dropped the entry with qty 0.
  await expect(preview).toContainText('"code": "A1"');
  await expect(preview).not.toContainText('"code": "B7"');

  // ── Save, reload, and check nothing changed ────────────────────────────────
  await page.getByRole("button", { name: "Save" }).click();
  await expect(page.getByText("Saved")).toBeVisible({ timeout: 15000 });

  await page.reload();
  await expect(page.getByRole("button", { name: "Save" })).toBeVisible({ timeout: 15000 });

  await expect(page.getByRole("textbox", { name: "Output field name" }).nth(0)).toHaveValue(
    "customerName",
  );
  await expect(page.getByRole("textbox", { name: "Output field name" }).nth(1)).toHaveValue(
    "channel",
  );
  await expect(page.getByRole("textbox", { name: "Output field name" }).nth(2)).toHaveValue("total");
  await expect(page.getByRole("combobox", { name: "Transform" }).nth(2)).toHaveValue("multiply");
  await expect(page.getByRole("combobox", { name: "Value type" }).nth(2)).toHaveValue("number");

  await expect(page.getByRole("textbox", { name: "Output list name" })).toHaveValue("lines");
  await expect(page.getByRole("combobox", { name: "Source list" })).toHaveValue("order.line");
  await expect(page.getByRole("combobox", { name: "Filter comparison" })).toHaveValue("greaterThan");

  // And the preview still produces the same document after the round trip.
  await expect(page.locator("pre").first()).toContainText('"total": 116,', { timeout: 15000 });
});

test("a rule that cannot be applied is named rather than producing an empty field", async ({
  page,
}) => {
  const subscriptionId = await createSubscription(page);
  await openMapper(page, subscriptionId);

  await page.getByRole("textbox", { name: "Sample source document" }).fill(SAMPLE);

  await addPathRule(page, "total", "order.customer");
  await page.getByRole("combobox", { name: "Value type" }).last().selectOption("number");

  // "Ali" is not a number. The old mapper wrote null into the field and said nothing;
  // this fails the mapping and names the rule.
  await expect(page.getByText(/could not be applied/)).toBeVisible({ timeout: 15000 });

  // Reported twice on purpose — once on the rule row that is wrong, and once in the
  // preview panel's summary of everything that failed.
  await expect(page.getByText(/cannot convert 'Ali' to number/)).toHaveCount(2);
  await expect(page.getByRole("alert").filter({ hasText: /cannot convert 'Ali'/ })).toBeVisible();
});

test("stored rules survive a switch to a list-shaped output and back", async ({ page }) => {
  const subscriptionId = await createSubscription(page);
  await openMapper(page, subscriptionId);

  await page.getByRole("textbox", { name: "Sample source document" }).fill(SAMPLE);
  await addPathRule(page, "customerName", "order.customer");

  await page.getByRole("checkbox", { name: /The whole output is a list/ }).check();
  await expect(page.getByText("Root list")).toBeVisible();

  await page.getByRole("checkbox", { name: /The whole output is a list/ }).uncheck();

  // The field rules were put aside, not thrown away.
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveValue(
    "customerName",
  );
});
