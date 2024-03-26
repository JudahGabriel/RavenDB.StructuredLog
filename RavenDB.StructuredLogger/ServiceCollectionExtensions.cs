using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;
using Microsoft.AspNetCore.Hosting;

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
            var logProvider = new RavenStructuredLoggerProvider();
            services.AddLogging(builder => builder.AddProvider(logProvider));
            services.AddTransient<IStartupFilter, RavenStructuredLoggerStartupFilter>();
            return services.AddSingleton(services =>
            {
                logProvider.Initialize(services, docStore, configAction);
                return logProvider;
            });
        }
    }
}
