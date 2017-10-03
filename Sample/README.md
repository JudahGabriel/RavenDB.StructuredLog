# RavenDB.StructuredLog Sample

This sample project shows how to configure Raven.StructuredLog and how to use it.

The interesting parts are:
 1. Startup.cs - initialization 
 2. appsettings.Development.json - configuration
 3. HomeController.cs - usage

Startup.cs initialization:
 ```csharp
 // This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{     
    // Create our Raven Structured Logger.
    var raven = this.CreateRavenDocStore();
    services.AddLogging(builder => builder.AddRavenStructuredLogger(raven));
	
	...
}
```

appsettings.Development.json configuration:
```json
"Logging": {
	"IncludeScopes": true, // Include scopes in the logs.
	"StructuredLogMaxOccurrences": 20, // Max number of occurrences that will be stored in a single StructuredLog. See StructuredLog.Occurrences for more info.
	"LogLevel": {
		"Default": "Debug", // Log everything from your own stuff.
		"System": "Warning", // Recommended, unless you like seeing hundreds of irrelevant logs.
		"Microsoft": "Warning" // Ditto to above.
	}
}
```

HomeController.cs usage:
```csharp
public IActionResult Index()
{
    // Simple logging
    logger.LogInformation("Hi there!");

    // Logging with templates
    logger.LogInformation("The time on the server is {time}.", DateTime.UtcNow);

    // Logging exceptions
    var exception = new InvalidOperationException("Foobar zanz");
    logger.LogError(exception, "Woops, an error occurred");

    // Logging exceptions with templates
    logger.LogError(exception, "Woops, an error occurred executing {action} at {date}", this.ControllerContext.ActionDescriptor.ActionName, DateTime.UtcNow);

    // Logging with scopes.
    // Note: Scopes will be logged only if appsettings.json has "IncludeScopes: true" inside the Logging section.
    using (logger.BeginScope(42))
    {
        logger.LogInformation("This message will have forty-two stored with it");
    }

    // Logging with multiple scopes.
    var totalCount = 777;
    using (logger.BeginScope(42)) // Plain value scopes.
    using (logger.BeginScope("The current user is {user}", User.Identity.Name)) // Template scopes
    using (logger.BeginKeyValueScope(nameof(totalCount), totalCount)) // Key-value pair scopes
    {
        logger.LogInformation("This log will contain forty-two, the current signed in user name, and a key-value pair containing the name of the totalCount variable and its value.");
    }

    return View();
}
```