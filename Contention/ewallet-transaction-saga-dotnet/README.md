## Practical System Design Pattern: Contention & Multi‑Step Workflow

> Native AOT .NET Serverless E‑Wallet Transaction Processing Saga (AWS Step Functions + DynamoDB + Postgres (row locking) + Idempotent Inbox)

This project demonstrates a production‑style approach to handling multi‑step financial transactions under high contention. A single logical "transfer" spans multiple independently deployed Lambda functions orchestrated by an AWS Step Functions state machine (Saga pattern). The implementation balances simplicity with strong correctness guarantees:

- Idempotent transaction creation (DynamoDB TransactWrite + conditional puts)
- Pessimistic row‑level locking for balance updates (Postgres `SELECT ... FOR UPDATE`)
- Inbox table to suppress duplicate credit/debit effects on retries
- Explicit compensation paths (reverse credit + refund debit) on failure
- Native AOT compiled .NET Lambdas for cold start minimization

If this helps, please ⭐ the repository.

---

## Architecture Overview

![Architecture Diagram](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*YcloQc7YsOUX812GdeK6Cg.png)

The diagram illustrates: client request → CreateTransaction Lambda (DynamoDB TransactWrite) → DynamoDB Stream → EventBridge Pipe (filter) → Step Functions Saga orchestrating debit / credit / success or compensation paths with Postgres row locks and DynamoDB idempotency.

### Components
| Component | Purpose |
|-----------|---------|
| DynamoDB Single Table | Stores transaction records + idempotency inbox entries (partition key prefixes: `INBOX#`, `TRANSACTION#`) |
| Postgres (schema `wallet`) | Durable wallet balances with strict row‑level locking |
| EventBridge Pipe | Declarative filtering + direct handoff from DDB Stream to Step Functions |
| Step Functions Saga | Orchestrates steps + compensation + retries |
| Lambda Functions (AOT) | Single responsibility units for each workflow step |

---

## Guarantees & Patterns

1. Atomic + Idempotent Transaction Creation

- Two conditional `Put` operations executed inside one `TransactWriteItems` call.
- Duplicate client submission → `ConditionCheckFailedException` → HTTP 409.

1. Idempotent Side‑Effects (Debit/Credit)

- Inbox table in Postgres: unique constraint on `(transaction_id)` prevents duplicate writes when a Lambda is retried.

1. Contention Safety

- Pessimistic lock (`SELECT ... FOR UPDATE`) prevents concurrent decrements from racing on the same row.

1. Compensating Actions

- Failure after debit but before credit → refund sender.
- Failure after credit → reverse receiver credit.

1. Fast Cold Starts

- Native AOT trimming reduces package size and startup latency across multiple small Lambdas.

---

## Saga State Machine Steps

| Step | Lambda | Action | Compensation Trigger |
|------|--------|--------|----------------------|
| ProcessTransaction | `Ewallet.ProcessTransactionFunction` | Mark transaction PROCESSING | n/a |
| DebitSenderWalletBalance | `Ewallet.DebitSenderWalletBalanceFunction` | Row‑lock sender, subtract balance, insert inbox | RefundSender on downstream failure |
| CreditReceiverWalletBalance | `Ewallet.CreditReceiverWalletBalanceFunction` | Row‑lock receiver, add balance, insert inbox | ReverseReceiverCredit on downstream failure |
| SuccessTransaction | `Ewallet.SuccessTransactionFunction` | Mark transaction SUCCESS | n/a |
| ReverseReceiverCredit (compensation) | `Ewallet.ReverseReceiverCreditFunction` | Undo receiver credit | n/a |
| RefundSenderWalletBalance (compensation) | `Ewallet.RefundSenderWalletBalanceFunction` | Refund sender debit | n/a |
| FailTransaction | `Ewallet.FailTransactionFunction` | Mark transaction FAIL + reason | n/a |

Retries are safe because every balance mutation is idempotent via inbox uniqueness.

---

## Key Code Snippets

### DynamoDB Transaction Creation

```csharp
var transactRequest = new TransactWriteItemsRequest
{
    TransactItems = new List<TransactWriteItem>
    {
        new() { Put = new Put { TableName = tableName, Item = idempotentItem, ConditionExpression = "attribute_not_exists(PK)" } },
        new() { Put = new Put { TableName = tableName, Item = transactionItem, ConditionExpression = "attribute_not_exists(PK)" } },
    },
};
await _dynamoDbClient.TransactWriteItemsAsync(transactRequest);
```

### Inbox Pattern (Postgres)

```csharp
const string insertInboxSql = @"INSERT INTO wallet.inbox (transaction_id, type, amount, created_at, updated_at)
VALUES (@TransactionId, @Type, @Amount, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC');";
try
{
    await connection.ExecuteAsync(insertInboxSql, new { TransactionId = "CREDIT#" + request.TransactionId, Type = "RECEIVER", Amount = request.Amount }, transaction);
}
catch (PostgresException ex) when (ex.SqlState == "23505")
{
    throw new DuplicateTransactionException($"Duplicate transaction_id {request.TransactionId} detected. Step Function retry suppressed.");
}
```

### Pessimistic Lock & Update

```csharp
const string selectAccountSql = @"SELECT id, user_id, balance FROM wallet.account WHERE user_id = @SenderUserId FOR UPDATE;";
var account = await connection.QuerySingleOrDefaultAsync<Account>(selectAccountSql, new { SenderUserId = request.SenderUserId }, transaction);
const string updateBalanceSql = @"UPDATE wallet.account SET balance = balance - @Amount, updated_at = NOW() AT TIME ZONE 'UTC' WHERE id = @Id;";
await connection.ExecuteAsync(updateBalanceSql, new { Amount = amount, Id = account.Id }, transaction);
await transaction.CommitAsync();
```

---

## Data Model

### DynamoDB Single Table (Logical PK prefixes)

| PK Prefix | Entity | Notes |
|-----------|--------|-------|
| `INBOX#<uuid>` | Idempotency marker for creation | Prevents duplicate create |
| `TRANSACTION#<id>` | Transaction item | Drives stream event |

### Postgres Schema (Excerpt)

```sql
CREATE TABLE wallet.account (
  id uuid PRIMARY KEY,
  user_id text UNIQUE NOT NULL,
  balance numeric(18,2) NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT NOW(),
  updated_at timestamptz NOT NULL DEFAULT NOW()
);

CREATE TABLE wallet.inbox (
  transaction_id text PRIMARY KEY,
  type text NOT NULL,
  amount numeric(18,2) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT NOW(),
  updated_at timestamptz NOT NULL DEFAULT NOW()
);
```

---

## Deployment

You can deploy infrastructure in two ways:

1. AWS CDK (recommended for full stack including Pipe + State Machine)
2. AWS SAM (for Lambda packaging/build)

### Prerequisites

- .NET 8 SDK (Native AOT)
- Node.js (for CDK)
- AWS CLI configured (`aws configure`)
- Docker (required for SAM build Native AOT + Lambda packaging)

### CDK Deploy

```powershell
cd cdk
npm install
npx cdk synth
npx cdk deploy --require-approval never
```

---

## Local Development & Testing

### Build Solution

```powershell
dotnet build Ewallet.sln
```


### Invoke a Function Locally

```powershell
sam local invoke Ewallet.CreateTransactionFunction --event events/create-transaction-event.json
```

### Manual Saga Trigger

If you need to replay a DynamoDB stream event locally, craft an input matching the Pipe template:

```json
{
  "TransactionId": "TRANSACTION#123",
  "SenderUserId": "user-a",
  "ReceiverUserId": "user-b",
  "Amount": 50
}
 
```

Invoke the first processing Lambda or start an execution of the Step Function in AWS console.

---

## Error Handling & Retries

- Duplicate creation → HTTP 409 surfaced (safe to retry client side).
- Lambda retry (e.g., network transient) results in inbox uniqueness violation → suppress duplicate side‑effect; you can log and treat as success.
- Business failures (insufficient funds) raise custom exceptions; Saga transitions to compensation + `FailTransaction`.
- All DB mutations wrapped in explicit transactions; commit only after successful validation and inbox write.

---

## Extending

Ideas for enhancement:

- Api Gateway integration.
- Dead Letter Queue for Event Bridge Pipe
- Unit Tests and Integration Tests

---

## License

Distributed under the terms of the repository root LICENSE.

---

If this project is useful, please give it a ⭐ and share feedback or issues.

Happy building!
