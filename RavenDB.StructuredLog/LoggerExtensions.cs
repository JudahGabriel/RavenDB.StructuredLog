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
    }
}
