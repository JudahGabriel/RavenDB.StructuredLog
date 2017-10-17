using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenDB.StructuredLog
{
    /// <summary>
    /// A log event. These are typically stored within a <see cref="StructuredLog"/>.
    /// </summary>
    public class Log
    {
        /// <summary>
        /// The formatted output message generated from the <see cref="Template"/>.
        /// If the <see cref="Template"/> is "The user {email} logged in", this Message will be "The user foo@bar.com logged in".
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The template for the message. For example, "The user {email} logged in. May be null in the case where no template string was used.
        /// </summary>
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        public string Template { get; set; }

        /// <summary>
        /// The log level.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The date when the log was created.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// The exception as a detailed string, if any exception occurred.
        /// This is a string, rather than the actual exception, because the exception may (and often does) contain self-referencing objects, resulting in exceptions during serialization.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// The log category. This is typically the type of the logger that created this log instance.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The EventId that created the log.
        /// </summary>
        public EventId? EventId { get; set; }

        /// <summary>
        /// The key and value pairs extracted from the <see cref="Template"/>.
        /// For example, if the <see cref="Template"/> is "The user {email} logged in", this will contain { "email", "foo@bar.com" }
        /// </summary>
        public IDictionary<string, object> TemplateValues { get; set; }

        /// <summary>
        /// Gets a list of scope values for the log. These will be created via logger.BeginScope(...).
        /// </summary>
        public IDictionary<string, object> Scope { get; set; }

        /// <summary>
        /// The deterministic hash code generated from the template.
        /// </summary>
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        public int TemplateHash { get; set; }
    }
}