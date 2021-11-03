**How to deal with concurrent DB update in .NET 5 with Entity Framework**

1. Use raw SQL update (just ignore EF)  
   RDBMS always guarantee atomic update
      
        update wallet set balance = balance + 100 where userId = '0000-0000-0000-0000'

2. Implement optimistic concurrency check  
   When EF detect concurrency conflict we have some choices
    - Retry with some delay
    - Response the situation to client (Hey! did you spam the submit button?)
    - Ignore!

3. Implement pessimistic concurrency control (beware of deadlock!)
   - Use row-lock level (it's very DB specific SQL command, you have to use raw query for it)
   - Use C#'s lock statement (be careful when running on multiple processes you will need distributed lock)
   - Use BCL semaphore (same with C# lock statement with async support)

4. Use message queue (publish-subscribe)  
   Put all incoming request to some queues (maybe partition by hash of user ID)  
   then have some services to subscribe on each partition
   - RabbitMQ will be a good start here
   - Actor Model (Akka.NET, F# Mailbox Processor)

5. Implement eventual consistency (no EF, micro-service approach)
   - Event Sourcing (Use marten for this)
   - Event Streaming (Kafka)

   
**Test Result**

Test with SQL server 2019
by Apache JMeter with 10 threads, each thread loop 10 times  
to send multiple recharge requests to the same wallet at the same time.

This test is an extreme case and did not reflect any real world usage!  
How can one of users recharge their wallet at this rate (> 10/s)?

| Concurrency Control Type | DbUpdateException handler | Retry | Success rate | Throughput |
|--------------------------|---------------------------|-------|-------------:|----------:|
|Optimistic                | N                         | N/A   | 50%          | 20.2/s    |
|Optimistic                | Y                         | 0     | 70%          | 13.7/s    |
|Optimistic                | Y                         | 5     | 91%          | 12.2/s    |
|Row lock                  | N/A                       | N/A   | 100%         | 34.1/s    |