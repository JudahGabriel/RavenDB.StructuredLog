using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;

namespace Raven.StructuredLogger
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
        /// <param name="docStore">The Raven <see cref="DocumentStore"/> to store the logs in.</param>
        /// <param name="configAction">The configuration action.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services, IDocumentStore docStore, Action<LogOptions> configAction)
        {
            return AddLogger(services, docStore, configAction);
        }

        /// <summary>
        /// Adds Raven.StructuredLogger to the logging capabilities.
        /// </summary>
        /// <param name="services">The DI services.</param>
        /// <param name="configAction">The configuration action.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services, Action<LogOptions> configAction)
        {
            return AddLogger(services, null, configAction);
        }

        /// <summary>
        /// Adds Raven.StructuredLogger to the logging capabilities.
        /// </summary>
        /// <param name="services">The DI services.</param>
        /// <param name="docStore">The Raven <see cref="DocumentStore"/> to store the logs in.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services, IDocumentStore docStore)
        {
            return AddLogger(services, docStore, null);
        }

        /// <summary>
        /// Adds Raven.StructuredLogger to the logging capabilities.
        /// </summary>
        /// <param name="services">The DI services.</param>
        /// <returns></returns>
        public static IServiceCollection AddRavenStructuredLogger(this IServiceCollection services)
        {
            return AddLogger(services, null, null);
        }

        private static IServiceCollection AddLogger(IServiceCollection services, IDocumentStore? docStore, Action<LogOptions>? configAction)
        {
            return services.AddSingleton<ILoggerProvider>(provider =>
            {
                var logger = CreateLogProvider(provider, docStore, configAction);
                services.AddLogging(builder => builder.AddProvider(logger));
                return logger;
            });
        }

        private static RavenStructuredLoggerProvider CreateLogProvider(IServiceProvider provider, IDocumentStore? docStore, Action<LogOptions>? configAction)
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var db = docStore ?? provider.GetRequiredService<IDocumentStore>();
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
