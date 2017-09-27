using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RavenDB.StructuredLog
{
    public class RavenStructuredLoggerProvider : ILoggerProvider
    {
        private readonly IDocumentStore db;

        public RavenStructuredLoggerProvider(IDocumentStore db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RavenStructuredLogger(categoryName, this.db);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Extends <see cref="ILoggingBuilder"/> with methods to install the Raven Structured logger.
    /// </summary>
    public static class RavenLoggerExtensions
    {
        /// <summary>
        /// Configures logging services to use RavenDB structured logging.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ravenDb">The RavenDB database singleton.</param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ILoggingBuilder AddRavenStructuredLogger(this ILoggingBuilder builder, IDocumentStore ravenDb)
        {
            var provider = new RavenStructuredLoggerProvider(ravenDb);
            builder.AddProvider(provider);
            return builder;
        }

        /// <summary>
        /// Configures logging services to use RavenDB structured logging. The RavenDB document store must be added to the dependency injection services.
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
                throw new InvalidOperationException("Before calling AddRavenStructuredLogger(), please add a RavenDB DocumentStore to the dependencies in Startup.ConfigureServices: services.AddSingleton<IDocumentStore>(db)", noDocStoreError);
            }

            return AddRavenStructuredLogger(builder, docStore);
        }
    }
}
