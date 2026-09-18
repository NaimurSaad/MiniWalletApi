# Architecture

## 1. Duplicate Submission Prevention

Duplicate transfer submissions can happen when a user clicks the transfer button multiple times, the network retries a request, or a client resends the same request.

### UI Button Lock

The transfer button can be disabled immediately after the user clicks it and remain disabled until the request is completed.

This prevents accidental multiple submissions caused by double-clicking.

### Debouncing

Debouncing can prevent repeated transfer requests that happen within a short period of time.

The client can ignore additional clicks for a short period after the first submission.

However, debouncing only provides client-side protection and should not be the only protection mechanism.

### Idempotency Keys

For financial operations such as wallet transfers, the backend can use an Idempotency-Key HTTP header.

Example:

Idempotency-Key: unique-request-id

The server stores the idempotency key together with the result of the transfer.

If the same key is received again, the server can return the previous result instead of processing the transfer again.

This provides server-side protection against duplicate requests and network retries.

### Recommended Approach

A robust transfer system can use all three approaches:

- UI button locking to prevent accidental double-clicks.
- Debouncing to prevent rapid repeated submissions.
- Idempotency keys to prevent duplicate processing at the backend.


## 2. SQL vs MongoDB: Normalization and Denormalization

### SQL Database

A relational SQL database commonly uses normalization to reduce data duplication and maintain consistency.

For this wallet system, wallet and transaction data can be stored in separate tables:

Wallet
------
Id
OwnerName
Balance

Transaction
-----------
Id
WalletId
Amount
Type
CreatedAt

The Transaction table uses WalletId to identify the wallet associated with the transaction.

Advantages of normalization include:

- Reduced data duplication.
- Better data consistency.
- Clear relationships between entities.
- Easier maintenance of related data.

### MongoDB

MongoDB is a document-oriented database. Depending on the application's access patterns, related data can be embedded in the same document.

For example, a wallet document could contain its transaction history together with the wallet information.

Embedding transaction data can make it easier to retrieve a wallet and its transaction history together.

However, denormalization can result in duplicated data and may make updates more difficult when the same information exists in multiple documents.

### Comparison

| Aspect | SQL | MongoDB |
|---|---|---|
| Data model | Relational tables | Documents |
| Common approach | Normalization | Normalization or denormalization |
| Relationships | Foreign keys / relationships | References or embedded documents |
| Data duplication | Usually minimized | Can be intentionally used |
| Read optimization | Queries and joins | Embedded documents can reduce additional queries |
| Design focus | Consistency and relationships | Access patterns and document structure |

The appropriate design depends on the application's access patterns, consistency requirements, and scalability requirements.

For a wallet and ledger system, separating wallets and transactions provides a clear relational structure. A MongoDB implementation could embed transaction data when retrieving a wallet together with its history is a common access pattern.