using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;

namespace Raven.StructuredLog
{
    /// <summary>
    /// Adds RavenStructuredLog-specific extensions to <see cref="ILogger"/> and <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Begins a logging operation scope with a key and value pair.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A logging operation scope.</returns>
        public static IDisposable BeginKeyValueScope(this ILogger logger, string key, object value)
        {
            return logger.BeginScope(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>
        /// Configures logging services to use RavenDB structured logging.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ravenDb">The RavenDB database singleton.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddRavenStructuredLogger(this ILoggingBuilder builder, IDocumentStore ravenDb)
        {
            var provider = new RavenStructuredLoggerProvider(ravenDb);
            builder.AddProvider(provider);

            // Install our log index that we use for grouping log messages together.


            // See if we're configured to specify the max occurrences.
            var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            if (config != null && int.TryParse(config["Logging:StructuredLogMaxOccurrences"], out var maxOccurrences))
            {
                RavenStructuredLoggerProvider.MaxStructuredLogOccurrences = maxOccurrences;
            }

            // See if we're configured to use scopes.
            if (config != null && bool.TryParse(config["Logging:IncludeScopes"], out var includeScopes))
            {
                RavenStructuredLoggerProvider.IncludeScopes = includeScopes;
            }

            // Expiration configuration.
            if (config != null && int.TryParse(config["Logging:ExpirationInDays"], out var expirationInDays))
            {
                RavenStructuredLoggerProvider.ExpirationInDays = expirationInDays;
            }

            return builder;
        }

        /// <summary>
        /// Configures logging services to use RavenDB structured logging. The RavenDB document store must be added to the dependency injection services before calling this method.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Couldn't find the RavenDB document store singleton.</exception>
        public static ILoggingBuilder AddRavenStructuredLogger(this ILoggingBuilder builder)
        {
            IDocumentStore docStore;
            try
            {
                docStore = builder.Services.BuildServiceProvider().GetRequiredService<IDocumentStore>();
            }
            catch (InvalidOperationException noDocStoreError)
            {
                throw new InvalidOperationException($"Before calling {nameof(AddRavenStructuredLogger)}(), you must add a RavenDB DocumentStore to the dependencies in Startup.ConfigureServices: services.AddSingleton<IDocumentStore>(db). Alternately, call {nameof(AddRavenStructuredLogger)}(ravenStore) to pass in your Raven IDocumentStore.", noDocStoreError);
            }

            return AddRavenStructuredLogger(builder, docStore);
        }
    }
}
