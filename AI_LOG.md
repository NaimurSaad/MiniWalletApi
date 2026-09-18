# AI Log

## AI Assistance Used During Development

AI assistance was used during the development and debugging of the Mini Wallet & Ledger System.

The main areas where AI assistance was used were:

- Optimizing the transaction history endpoint.
- Validating transfer requests.
- Protecting wallet balance updates from concurrent requests.
- Fixing the audit notification service lifetime and asynchronous behavior.
- Implementing centralized exception handling.
- Adding API-key based authorization to prevent unauthorized wallet access.
- Preparing QA test cases and architecture documentation.

## Main Prompts Used

### Task 1 - Transaction History Optimization

"Optimize the GET /api/wallets/{id}/transactions endpoint by eliminating N+1 database round trips and using EF Core projections and non-tracking queries."

### Task 2 - Transfer Validation and Concurrency

"Audit the wallet transfer endpoint. Reject negative and invalid transfer amounts and protect balance updates from concurrent race conditions so that a wallet balance never becomes negative under parallel requests."

### Task 3 - Audit Service

"Fix the dependency injection lifetime issue in the background notification service and replace the blocking synchronous implementation with true non-blocking async I/O."

### Task 4 - Exception Handling

"Centralize exception handling in the ASP.NET Core application and return RFC 7807 ProblemDetails responses."

### Task 5 - Authorization

"Prevent unauthorized wallet and transaction-history access using header-based API-key authentication and prevent users from transferring money from another user's wallet."

### Documentation

"Create architecture documentation covering duplicate transfer submissions using UI locks, debouncing and Idempotency Keys, and compare normalization and denormalization approaches in SQL and MongoDB."

"Create QA test cases covering successful transfers, boundary conditions, validation failures and concurrent transfers."

## Example of an AI Suggestion That Was Changed

During the implementation of Task 2, an initial approach involved using an asynchronous delay inside the transfer flow to simulate processing time.

The original code contained:

Task.Delay(100)

This was not appropriate for the final implementation because it unnecessarily delayed the transfer operation and did not provide protection against concurrent balance updates.

The implementation was changed by removing the artificial delay and placing the balance check, balance updates and transaction creation inside an application-level lock.

The final approach ensures that concurrent requests cannot simultaneously pass the balance check using the same wallet balance.

## Human Review and Testing

The generated suggestions were reviewed and adapted during implementation.

The application was manually tested using Postman, including:

- Successful transfers.
- Negative transfer amounts.
- Zero transfer amounts.
- Insufficient funds.
- Same-wallet transfers.
- Concurrent transfer requests.
- Missing API keys.
- Unauthorized wallet access.
- Unauthorized transfer attempts.

The final implementation was verified by building and running the ASP.NET Core application and testing the API endpoints.