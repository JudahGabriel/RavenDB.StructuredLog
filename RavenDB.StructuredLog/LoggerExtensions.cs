using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace RavenDB.StructuredLog
{
    /// <summary>
    /// Adds 
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
