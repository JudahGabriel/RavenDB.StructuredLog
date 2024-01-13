using System;
using System.Collections.Generic;
using System.Text;

namespace Raven.StructuredLog
{
    /// <summary>
    /// Options for configuring Raven.StructuredLog.
    /// </summary>
    public class LogOptions
    {
        /// <summary>
        /// Whether to include scopes in logs. Defaults to true.
        /// </summary>
        public bool IncludeScopes { get; set; } = true;

        /// <summary>
        /// The maximum number of log occurrences to store within a single StructuredLog. Defaults to 20.
        /// </summary>
        public int MaxOccurrences { get; set; } = 20;

        /// <summary>
        /// The number of days after which to expire StructuredLog documents that haven't been updated. Defaults to 365.
        /// Each time a structured log has another log added to it, expiration will be pushed out this many days.
        /// </summary>
        public int ExpirationInDays { get; set; } = 365;

        /// <summary>
        /// The suggestion accuracy for grouping logs together, from 0 to 1. Defaults to 0.8f.
        /// 1 is the precise: only near-exact messages will be grouped together.
        /// 0 is the imprecise: slightly similar but different logs will be grouped together.
        /// </summary>
        public float FuzzyLogSearchAccuracy { get; set; } = 0.8f; // // 0.9 is too strict, won't match things that should be grouped. 0.8 seems about right.
    }
}
