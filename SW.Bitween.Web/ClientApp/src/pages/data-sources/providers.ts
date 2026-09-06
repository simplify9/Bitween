/**
 * The bus providers Bitween ships, and what each expects.
 *
 * Connection settings are untyped by design — a provider must not be limited to the subset of a
 * broker's model Bitween happens to have modelled — but an empty key/value grid is not a form
 * anyone can fill in. This is presentation only: it decides which fields a new data source starts
 * with, not which ones the adapter will accept.
 */
export interface BusProvider {
  id: string;
  label: string;
  description: string;
  /** Field name to a short explanation, shown under the input. */
  hints: Record<string, string>;
  defaults: Record<string, string>;
  secrets: string[];
}

export const BUS_PROVIDERS: BusProvider[] = [
  {
    id: "bitween.bus.rabbitmq",
    label: "RabbitMQ",
    description: "An AMQP broker the customer runs, separate from Bitween's own bus.",
    defaults: {
      Host: "",
      Port: "5672",
      UserName: "",
      Password: "",
      VirtualHost: "/",
      DeclareMode: "assert",
      Prefetch: "16",
    },
    secrets: ["Password"],
    hints: {
      DeclareMode:
        "none — assume everything exists. assert — check and fail loudly if not. create — declare the queues.",
      Prefetch: "How many messages the broker lets Bitween hold unacknowledged at once.",
      Tls: "Set to true for anything that is not localhost — AMQP authenticates in the clear otherwise.",
    },
  },
  {
    id: "bitween.bus.sqs",
    label: "Amazon SQS",
    description:
      "An SQS queue, including the one an Amazon Selling Partner notification subscription delivers to.",
    defaults: {
      Region: "eu-west-1",
      AccessKeyId: "",
      SecretAccessKey: "",
      WaitTimeSeconds: "20",
      VisibilityTimeoutSeconds: "60",
      UnwrapSellingPartnerNotification: "false",
    },
    secrets: ["AccessKeyId", "SecretAccessKey"],
    hints: {
      AccessKeyId: "Leave both keys blank on AWS to use the instance profile or IRSA instead.",
      VisibilityTimeoutSeconds:
        "Must exceed how long Bitween takes to persist a message, or SQS redelivers one already being handled.",
      UnwrapSellingPartnerNotification:
        "true unwraps the SP-API envelope so subscriptions see the notification payload itself.",
    },
  },
];

export const providerOf = (adapterId: string): BusProvider | undefined =>
  BUS_PROVIDERS.find((p) => p.id === adapterId);

export const providerLabel = (adapterId: string): string =>
  providerOf(adapterId)?.label ?? adapterId;

/**
 * Whether a setting holds a credential. The backend decides this for real — and masks
 * accordingly — but the form needs to know before a value has ever been saved.
 */
const CREDENTIAL = /password|secret|token|credential|apikey|accesskey|privatekey|connectionstring|sas|passphrase|certificate/i;

export const isSecretName = (name: string, declared: string[] = []): boolean =>
  declared.some((d) => d.toLowerCase() === name.toLowerCase()) || CREDENTIAL.test(name);
