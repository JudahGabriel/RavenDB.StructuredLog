using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;

namespace Raven.StructuredLogger
{
    /// <summary>
    /// Logging provider that structures and groups logs and stores them in RavenDB.
    /// </summary>
    public class RavenStructuredLoggerProvider : ILoggerProvider
    {
        private readonly IDocumentStore db;
        private readonly LogOptions options;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="db">The Raven document store to send the logs to.</param>
        /// <param name="options">The log options.</param>
        public RavenStructuredLoggerProvider(IDocumentStore db, LogOptions options)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a new logger.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new RavenStructuredLogger(categoryName, this.db, this.options);
        }

        /// <summary>
        /// Disposes the log provider.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
