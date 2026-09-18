# QA Test Cases

## Transfer API Testing

The following test cases were used to verify the wallet transfer functionality, including successful transfers, boundary conditions, validation, authorization, and concurrent requests.

| Test Case | Scenario | Input | Expected Result |
|---|---|---|---|
| TC-01 | Successful transfer | Sender: 1, Receiver: 2, Amount: 50 | Transfer succeeds and sender balance decreases by 50 |
| TC-02 | Boundary: transfer entire balance | Sender: 1, Receiver: 2, Amount: 500 | Transfer succeeds and sender balance becomes 0 |
| TC-03 | Negative amount | Sender: 1, Receiver: 2, Amount: -50 | Request is rejected with a validation error |
| TC-04 | Zero amount | Sender: 1, Receiver: 2, Amount: 0 | Request is rejected with a validation error |
| TC-05 | Insufficient funds | Sender: 1, Receiver: 2, Amount: greater than sender balance | Request is rejected with an insufficient funds error |
| TC-06 | Same wallet transfer | Sender: 1, Receiver: 1, Amount: 50 | Request is rejected because sender and receiver must be different |
| TC-07 | Concurrent transfers | Multiple parallel requests transferring 100 from a wallet with 500 balance | Successful transfers must not cause the wallet balance to become negative |
| TC-08 | Unauthorized wallet transfer | Alice API key attempting to transfer from Bob's wallet | Request is rejected with 403 Forbidden |
| TC-09 | Unauthorized transaction history | Alice API key accessing Bob's transaction history | Request is rejected with 403 Forbidden |
| TC-10 | Missing API key | Request without X-API-Key header | Request is rejected with 401 Unauthorized |

## Concurrency Test

A concurrent transfer test was performed by sending multiple transfer requests in parallel.

The sender wallet initially had a balance of 500.

Each request attempted to transfer 100.

The transfer operation uses an application-level lock around the balance check and update. This ensures that concurrent requests do not all pass the balance check using the same old balance.

The expected behavior is:

- A maximum of five transfers of 100 can succeed.
- The sender balance must never become negative.
- Additional transfers are rejected with an insufficient funds response.

## Authorization Test

Header-based API keys were used to prevent unauthorized wallet and transaction-history access.

The configured keys are:

- alice-key → Wallet 1
- bob-key → Wallet 2

A request using Alice's key to access or transfer from Wallet 2 is rejected with HTTP 403 Forbidden.

A request without an API key is rejected with HTTP 401 Unauthorized.