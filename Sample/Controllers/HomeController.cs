﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.StructuredLog;
using Sample.Models;
using System;
using System.Diagnostics;

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

            // Logging exceptions.
            var exception = new InvalidOperationException("Foobar zanz");
            exception.Data.Add("life, universe, everything", 42);
            logger.LogError(exception, "Woops, an error occurred");

            // Logging exceptions with templates
            var otherException = new ArgumentNullException();
            logger.LogError(otherException, "Woops, an error occurred executing {action} at {date}", this.ControllerContext.ActionDescriptor.ActionName, DateTime.UtcNow);

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
            using (logger.BeginKeyValueScope("total count", totalCount)) // Key-value pair scopes
            {
                logger.LogInformation("This log will contain forty-two, the current signed in user name, and the total count name and value");
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
