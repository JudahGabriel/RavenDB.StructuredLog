using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Raven.Client.Documents;
using System;

namespace Raven.StructuredLogger
{
    /// <summary>
    /// Logging provider that structures and groups logs and stores them in RavenDB.
    /// </summary>
    public class RavenStructuredLoggerProvider : ILoggerProvider
    {
        private IDocumentStore? db;
        private LogOptions? options;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public RavenStructuredLoggerProvider()
        {
        }

        /// <summary>
        /// Creates a new logger.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            // If we haven't been initialized yet, return a null logger.
            if (db == null || options == null)
            {
                return NullLogger.Instance;
            }

            return new RavenStructuredLogger(categoryName, this.db, this.options);
        }

        internal void Initialize(IServiceProvider provider, IDocumentStore? docStore, Action<LogOptions>? configAction)
        {
            this.options = new LogOptions();
            this.db = docStore ?? provider.GetRequiredService<IDocumentStore>();
            var config = provider.GetRequiredService<IConfiguration>();

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
        }

        /// <summary>
        /// Disposes the log provider.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
