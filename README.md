# PollyCircuitBreakerTest

Created this solution to test using Polly's Circuit Breaker and Fallback Policies to point to a backup api gateway until service is restored on the primary api gateway.

The solution consists of 3 .NET Core projects.  One is an ASP.NET Core MVC site project. Two are ASP.NET Core WebAPI projects.

Both WebAPI sites have a simple method that returns a string, identifying which project generated the string.  One WebAPI project is designed to throw Bad Requests (400) for a number of times before returning successful.  This is to test the circuit breaker.  

The MVC Site is the Microsoft template.  The About page has been updated to pull the header from the API.  Polly is implemented here.  If the Primary API opens the breaker, the site fails over to the backup API.  
