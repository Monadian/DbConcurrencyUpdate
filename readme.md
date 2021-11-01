**How to deal with concurrency DB update in .NET 5 with Entity Framework**
1. Implment optimistic concurrency check first!
   When EF detect concurrency conflict we have some choices
    - Retry with some delay
    - Response to situation to client (Hey! are you spam the submit button?)
    - Ignore!
2. Implment pessimistic concurrency control (beware of deadlock!)
   - Use row-lock level (It's very DB specific SQL command, you have to use raw query for it)
   - Use C#'s lock statement (be careful when running on multiple processes you need distributed lock)
   - Use C#'s semaphore (It's like lock statement with async support)
3. Implment eventual consistency (micro-service)
   - Event Sourcing (Use marten for this)
   - Event Streaming (Kafka)
   
   
**Test Result**

Test with SQL server 2019
by Apache JMeter with 10 threads each threads loop for 10 times
to send multiple requests to recharge the same wallet at the same time.

| Concurrency Control Type | DbUpdateException handler | Retry | Success rate | Troughput |
|--------------------------|---------------------------|-------|-------------:|----------:|
|Optimistic                | N                         | N     | 38.0%        | 21.2/s    |
|Optimistic                | Y                         | N     | 75.5%        | 13.442/s  |
|Optimistic                | Y                         | Y     | 86.0%        | 12.5/s    |
|Row lock                  | N/A                       | N/A   | 100%         | 27.5/s    |