using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sample.Models;
using Microsoft.Extensions.Logging;
using RavenDB.StructuredLog;

namespace Sample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;

        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

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

            // Logging with scopes
            using (logger.BeginScope(42))
            {
                logger.LogInformation("This message will have forty-two stored with it");
            }

            // Logging with multiple scopes.
            var totalCount = 777;
            using (logger.BeginScope(42))
            using (logger.BeginScope("The current user is {user}", User.Identity.Name))
            using (logger.BeginKeyValueScope(nameof(totalCount), totalCount))
            {
                logger.LogInformation("This log will contain forty-two, the current signed in user name, and a key-value pair containing the name of the totalCount variable and its value.");
            }

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
