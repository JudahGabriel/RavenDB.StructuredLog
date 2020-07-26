using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;

namespace Raven.StructuredLog
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Raven.StructuredLogger to the logging capabilities.
        /// </summary>
        /// <param name="services">The DI services.</param>
        /// <param name="configAction">The configuration action.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services, Action<LogOptions> configAction)
        {
            return services.AddSingleton<ILoggerProvider>(provider =>
            {
                var logger = CreateLogProvider(provider, configAction);
                services.AddLogging(builder => builder.AddProvider(logger));
                return logger;
            });
        }

        /// <summary>
        /// Adds Raven.StructuredLogger to the logging capabilities.
        /// </summary>
        /// <param name="services">The DI services.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services)
        {
            return AddRavenStructuredLogger(services, (_) => { });
        }

        private static RavenStructuredLoggerProvider CreateLogProvider(IServiceProvider provider, Action<LogOptions> configAction)
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var db = provider.GetRequiredService<IDocumentStore>();
            var options = new LogOptions();

            if (int.TryParse(config["Logging:MaxOccurrences"], out var maxOccurrences))
            {
                options.MaxOccurrences = maxOccurrences;
            }

            // See if we're configured to use scopes.
            if (bool.TryParse(config["Logging:IncludeScopes"], out var includeScopes))
            {
                options.IncludeScopes = includeScopes;
            }

            // See if we're configured to expire logs.
            if (int.TryParse(config["Logging:ExpirationInDays"], out var expirationInDays))
            {
                options.ExpirationInDays = expirationInDays;
            }

            // See if we're configured to expire logs.
            if (float.TryParse(config["Logging:FuzzyLogSearchAccuracy"], out var fuzzyLogSearchAccuracy))
            {
                options.FuzzyLogSearchAccuracy = fuzzyLogSearchAccuracy;
            }

            configAction?.Invoke(options);
            return new RavenStructuredLoggerProvider(db, options);
        }
    }
}
