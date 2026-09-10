import { describe, expect, it } from "vitest";

import { draftNameFor, draftStatementFor, groupBySchema, qualify, type DbObject } from "../schema";

const object = (over: Partial<DbObject> = {}): DbObject => ({
  schema: "sales",
  name: "orders",
  type: "table",
  rowCount: null,
  columns: [],
  parameters: [],
  comment: null,
  ...over,
});

describe("groupBySchema", () => {
  it("groups without reordering what the adapter returned", () => {
    // The catalog query orders by schema then name; re-sorting here would fight it and put a
    // page's rows in a different order from the page before it.
    const grouped = groupBySchema([
      object({ schema: "sales", name: "orders" }),
      object({ schema: "sales", name: "customers" }),
      object({ schema: "audit", name: "log" }),
    ]);

    expect(grouped.map((g) => g.schema)).toEqual(["sales", "audit"]);
    expect(grouped[0].objects.map((o) => o.name)).toEqual(["orders", "customers"]);
  });
});

describe("qualify", () => {
  it("leaves a plain identifier alone", () => {
    expect(qualify("sales", "orders")).toBe("sales.orders");
  });

  it("quotes anything that would not resolve unquoted", () => {
    // PostgreSQL folds an unquoted name to lower case, so an unquoted mixed-case name finds
    // nothing — generating SQL the user cannot run is worse than generating none.
    expect(qualify("sales", "Orders")).toBe('sales."Orders"');
    expect(qualify("my schema", "order-line")).toBe('"my schema"."order-line"');
  });
});

describe("draftStatementFor", () => {
  it("names the columns it knows about, and limits the rows", () => {
    // The row limit is not politeness: the first thing anyone does with a new statement is run it
    // against a table whose size they have not checked.
    const sql = draftStatementFor(
      object({
        columns: [
          { name: "id", dbType: "integer", clrType: "Int32", nullable: false, primaryKey: true, generated: true, ordinal: 1 },
          { name: "total", dbType: "numeric", clrType: "Decimal", nullable: true, primaryKey: false, generated: false, ordinal: 2 },
        ],
      }),
    );

    expect(sql).toContain("select id, total");
    expect(sql).toContain("from sales.orders");
    expect(sql).toContain("limit 100");
  });

  it("falls back to a star when the columns have not been read yet", () => {
    // The row can be used before it is expanded, and a statement that names no columns is still
    // a working starting point.
    expect(draftStatementFor(object())).toContain("select *");
  });

  it("calls a procedure rather than selecting from it", () => {
    const sql = draftStatementFor(
      object({
        type: "procedure",
        name: "release_order",
        parameters: [
          { name: "order_id", dbType: "integer", direction: "In", ordinal: 1 },
          { name: "released", dbType: "boolean", direction: "Out", ordinal: 2 },
        ],
      }),
    );

    // Only the inputs are bound: an out parameter is the procedure's answer, not something the
    // caller supplies. The adapter sends the direction as `In`/`Out`, so the match is
    // case-insensitive — reading it literally would bind everything or nothing.
    expect(sql).toBe("call sales.release_order(@order_id)");
  });

  it("selects from a set-returning function", () => {
    const sql = draftStatementFor(
      object({
        type: "function",
        name: "orders_for_customer",
        parameters: [{ name: "cid", dbType: "integer", direction: "In", ordinal: 1 }],
      }),
    );

    expect(sql).toBe("select * from sales.orders_for_customer(@cid)");
  });

  it("reads the next value of a sequence", () => {
    expect(draftStatementFor(object({ type: "sequence", name: "order_seq" }))).toBe(
      "select nextval('sales.order_seq')",
    );
  });
});

describe("draftNameFor", () => {
  it("makes a name that reads as what it does", () => {
    expect(draftNameFor(object({ name: "order_line" }))).toBe("readOrderLine");
    expect(draftNameFor(object({ type: "procedure", name: "release_order" }))).toBe(
      "callReleaseOrder",
    );
  });
});
