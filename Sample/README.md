# RavenDB.StructuredLog Sample

This sample project shows how to configure Raven.StructuredLog and how to use it.

The interesting parts are:
 1. Startup.cs - initialization 
 2. appsettings.Development.json - configuration
 3. HomeController.cs - usage

## Initialization - Startup.cs
 ```csharp
 // This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{     
    // Create our Raven Structured Logger.
    var raven = this.CreateRavenDocStore();
    services.AddLogging(builder => builder.AddRavenStructuredLogger(raven));
	
	// ...
}
```

## Configuration - appsettings.Development.json
```json
"Logging": {
	"IncludeScopes": true, 
	"StructuredLogMaxOccurrences": 20,
	"LogLevel": {
		"Default": "Debug", 
		"System": "Warning",
		"Microsoft": "Warning"
	}
}
```

IncludeScopes is used if you want to persist data stored in log scopes. (See below for more info.)

StructuredLogMaxOccurrences is the max number of occurrences to store inside a StructuredLog. Default is 20.

As for LogLevel, we recommend Warning for both System and Microsoft to reduce the noise in your logs.

## Usage - HomeController.cs
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