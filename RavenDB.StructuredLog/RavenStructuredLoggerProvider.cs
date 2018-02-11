using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;

namespace Raven.StructuredLog
{
    /// <summary>
    /// Logging provider that structures and groups logs and stores them in RavenDB.
    /// </summary>
    public class RavenStructuredLoggerProvider : ILoggerProvider
    {
        private readonly IDocumentStore db;
        private const int DefaultMaxStrucutedLogOccurrences = 20;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="db">The RavenDB <see cref="IDocumentStore"/> instance to store the logs with.</param>
        public RavenStructuredLoggerProvider(IDocumentStore db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Creates a new logger.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new RavenStructuredLogger(categoryName, this.db);
        }

        /// <summary>
        /// Disposes the log provider.
        /// </summary>
        public void Dispose()
        {
        }

        internal static bool IncludeScopes { get; set; } = true;
        internal static int MaxStructuredLogOccurrences { get; set; } = MaxStructuredLogOccurrences;
    }
}
