# RavenDB.StructuredLog
An ASP.NET Core logger that utilizes RavenDB to store structured logs.

<a href="https://andrewlock.net/creating-an-extension-method-for-attaching-key-value-pairs-to-scope-state-using-asp-net-core/">Structured log</a> that uses RavenDB as the log store.

Old, ugly way of logging makes for thousands of opaque logs:
- "User foo@bar.com signed in at 5:13 Oct 7"
- "User no@regerts.com signed in at 2:25 Nov 8"
- "User bar@zanz.com signed in at 3:18 Nov 21"
- "User me@me2.com signed in at 12:0 Dec 15"
- [and on and on for 1000+ entries - oiy!]

But with structured and grouped logging, you get a fewer logs that group similar logs together and makes them searchable:
```
{
    "MessageTemplate": "User {email} signed in at {date}",
    "Level": "Information",
	"OccurrencesCount": 1032
    "FirstOccurrence": "2017-09-27T17:29:46.6597966+00:00",
    "LastOccurrence": "2017-09-27T17:39:50.5554997+00:00",
    "Occurrences": [
        {
            "Message": "User foo@bar.com signed in at 5:13 Oct 7",
            "Level": "Information",
            "Created": "2017-09-27T17:39:48.4248681+00:00",
            "Exception": null,
            "Category": "Sample.Controllers.HomeController",
            "EventId": null,
            "TemplateValues": {
                "{OriginalFormat}": "User {email} signed in at {date}",
				"email": "foo@bar.com",
				"date": "5:13 Oct 7"
            },
            "Scope": {}
        },
		...all log occurrences, trimmed with a user-specified maximum
    ],
    
}

The end result is humans can easily understand what errors are occurring in your software and how often. Moreover, unlike old school logging where logs are giant opaque strings, structured logs are searchable as their template values are extracted and stored outside the log message.

## Instructions ##
1. In Startup.cs:

```csharp
public void ConfigureServices(IServiceCollection services)
{
	// Add RavenDB structured logging.
	services.AddLogging(builder => builder.AddRavenStructuredLogger(docStore)); // Where docStore is your RavenDB DocumentStore singleton.

	...
}
```

2. Use logging as you normally would inside your controllers and services:
```csharp
public class HomeController : Controller 
{
	private ILogger<HomeController> logger;

	public HomeController(ILogger<HomeController> logger)
	{
		this.logger = logger;
	}

	public string Get()
	{
		// Simple logging
		logger.LogInformation("Hi there!");

		// Logging with templates
		logger.LogInformation("The time on the server is {time}.", DateTime.UtcNow);

		// Logging exceptions
		logger.LogError(exception, "Woops, an error occurred");
		
		// Logging exceptions with templates
		logger.LogError(exception, "Woops, an error occurred executing {action} at {date}", this.ControllerContext.ActionDescriptor.ActionName, DateTime.UtcNow);

		// Logging with scopes
		using (logger.BeginScope(42))
		{
			logger.LogInformation("This message will have forty-two stored with it");
		}

		// Logging with multiple scopes and scope templates.
		using (logger.BeginScope(42))
		using (logger.BeginScope("The current user is {user}", User.Identity.Name))
		using (logger.BeginKeyValueScope(nameof(totalCount), totalCount))
		{
			logger.LogInformation("This log will contain forty-two, the current signed in user name, and a key-value pair containing the name of the totalCount variable and its value.");
		}
		
		...
	}
}
```

3. You're done! 

Need help? See the [sample app](https://github.com/JudahGabriel/RavenDB.StructuredLog/tree/master/Sample).