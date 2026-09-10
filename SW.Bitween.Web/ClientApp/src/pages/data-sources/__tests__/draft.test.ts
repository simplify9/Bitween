import { describe, expect, it } from "vitest";

import type { DataSourceDetail } from "../../../api/types";
import { draftOf, editableFingerprint } from "../draft";

const detail = (over: Partial<DataSourceDetail> = {}): DataSourceDetail =>
  ({
    id: 1,
    name: "Acme RabbitMQ",
    adapterId: "bitween.bus.rabbitmq",
    kind: "Broker",
    inactive: false,
    deduplicationWindowDays: 30,
    softMemoryLimitMb: 0,
    hardMemoryLimitMb: 0,
    cpuPercentLimit: 0,
    cpuLimitSamples: 0,
    gatewayCount: 3,
    properties: { Host: "localhost", Port: "5672" },
    secretProperties: ["Password"],
    lastKnownState: "Connected",
    lastHeartbeatOn: "2026-09-07T12:00:00Z",
    lastException: null,
    consecutiveFailures: 0,
    ownedByNode: "node-a (term 7)",
    ...over,
  }) as DataSourceDetail;

describe("editableFingerprint", () => {
  /**
   * The bug this exists for: the detail query is polled so the connection panel stays live, and
   * the form used to re-seed from every response. A setting added and not yet saved disappeared
   * within ten seconds, with no error and nothing to say the form had done it.
   */
  it("ignores the live figures that change on every poll", () => {
    const before = detail();
    const heartbeat = detail({
      lastHeartbeatOn: "2026-09-07T12:00:15Z",
      lastKnownState: "Connected",
      consecutiveFailures: 2,
      ownedByNode: "node-b (term 8)",
      gatewayCount: 4,
    });

    expect(editableFingerprint(heartbeat)).toBe(editableFingerprint(before));
  });

  it("changes when a setting really changes, so a save still re-masks the secrets", () => {
    // Saving sends the typed password and gets the sentinel back; the form has to go back to
    // showing "stored" rather than a value the server will never return again.
    const saved = detail({ properties: { Host: "localhost", Port: "5672", Password: "__private__" } });

    expect(editableFingerprint(saved)).not.toBe(editableFingerprint(detail()));
  });

  it("changes when any other editable field changes", () => {
    expect(editableFingerprint(detail({ name: "Renamed" }))).not.toBe(editableFingerprint(detail()));
    expect(editableFingerprint(detail({ inactive: true }))).not.toBe(editableFingerprint(detail()));
    expect(editableFingerprint(detail({ hardMemoryLimitMb: 512 }))).not.toBe(
      editableFingerprint(detail()),
    );
  });
});

describe("draftOf", () => {
  it("copies the properties rather than aliasing them, so editing does not mutate the cache", () => {
    const d = detail();
    const draft = draftOf(d);
    draft.properties.QueueType = "quorum";

    expect(d.properties).not.toHaveProperty("QueueType");
  });
});
