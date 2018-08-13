using Microsoft.Extensions.Logging;
using Raven.Client;
using Raven.Client.Documents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Raven.StructuredLog
{
    /// <summary>
    /// Log provider that sends messages to RavenDB asynchronously.
    /// </summary>
    public class RavenStructuredLogger : ILogger, IDisposable
    {
        private readonly Subject<Log> logs = new Subject<Log>();
        private readonly string categoryName;
        private ConcurrentBag<RavenStructuredLogScope> scopeOrNull; // Logger is not thread safe. However, some frameworks dispose on a background thread, causing this bag to be accessed on multipl threads.
        private IDisposable logsSubscriptionOrNull;
        private IDocumentStore db;

        private const string originalFormat = "{OriginalFormat}";

        /// <summary>
        /// Creates a new structured logger.
        /// </summary>
        /// <param name="categoryName">The name of the logger.</param>
        /// <param name="db">The Raven <see cref="IDocumentStore"/>.</param>
        public RavenStructuredLogger(string categoryName, IDocumentStore db)
        {
            this.categoryName = categoryName;
            this.db = db;
        }

        /// <summary>
        /// Begins a logical scope to the logger.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            // If we're not configured to use scopes, punt.
            if (!RavenStructuredLoggerProvider.IncludeScopes)
            {
                return null;
            }

            if (this.scopeOrNull == null)
            {
                this.scopeOrNull = new ConcurrentBag<RavenStructuredLogScope>();
            }

            var scope = new RavenStructuredLogScope(state);
            scope.Disposed.Subscribe(_ => this.scopeOrNull.TryTake(out var _));
            scopeOrNull.Add(scope);

            return scope;
        }

        /// <summary>
        /// Gets whether the logger is enabled.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Writes the log asynchronously to Raven.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                if (this.logsSubscriptionOrNull == null)
                {
                    this.logsSubscriptionOrNull = this.logs
                        .GroupByUntil(logs => 1, _ => this.CreateTimer()) // If we log a bunch in quick succession, batch them together and log in a single trip to the DB.
                        .SelectMany(logs => logs.ToList())
                        .Subscribe(logs => TryWriteLogBatch(logs, db));
                }

                // Convert the state to a dictionary of name/value pairs.
                var details = default(Dictionary<string, object>);
                if (state is IEnumerable<KeyValuePair<string, object>> list)
                {
                    details = new Dictionary<string, object>(list.Count());
                    foreach (var pair in list)
                    {
                        details.Add(pair.Key, pair.Value);
                    }
                }
                
                var message = formatter(state, exception);

                // If the message is the same as the {OriginalFormat}, don't store the original format; it's duplicate data.
                if (details != null && details.TryGetValue(originalFormat, out var origFormatValue) && (origFormatValue as string) == message)
                {
                    details.Remove(originalFormat);
                }
                var (function, file, line) = ExtractMyFunctionAndLine(exception);
                var structuredLogHash = GenerateStructuredHash(message, exception, details, function, file, line);
                var log = new Log
                {
                    Created = DateTime.UtcNow,
                    Exception = exception?.ToDetailedString(),
                    Message = message,
                    Function = function,
                    File = file,
                    LineNumber = line,
                    TemplateValues = details,
                    Level = logLevel,
                    Category = categoryName,
                    EventId = eventId.Id == 0 && eventId.Name == null ? new EventId?() : eventId,
                    Template = structuredLogHash.message != message ? structuredLogHash.message : null, // Fill in the template only if it differs from the message.
                    TemplateHash = structuredLogHash.hash,
                    Scope = this.ScopeToDictionary()
                };
                this.logs.OnNext(log);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (logsSubscriptionOrNull != null)
            {
                logsSubscriptionOrNull.Dispose();
            }
        }

        private IObservable<long> CreateTimer()
        {
            return Observable.Timer(TimeSpan.FromSeconds(2));
        }

        private (int hash, string message) GenerateStructuredHash(string message, Exception exception, Dictionary<string, object> details, string function, string file, string line)
        {
            // Do we have an "{OriginalFormat}" in the details? If so, use that to generate the log structured hash.
            // {OriginalFormat} is the raw string template. It will look something like this: "Generated {length} new items".
            // We use that for hte structured log hash, so that it will group both of the following messages together in a single StructuredLog: 
            //  - "Generated 5 new items" 
            //  - "Generated 2 new items"
            var origFormatString = default(string);
            if (details != null)
            {
                if (details.TryGetValue(originalFormat, out var origFormat))
                {
                    origFormatString = origFormat as string;
                }
                else if (details.Any())
                {
                    origFormatString = TryExtractFormatFromMessageWithDetails(message, details);
                }
            }

            // Calculate the hash on the unique message. 
            // This is either the original format string ("{user} created {number} new items")
            // or the message itself "Object reference not set to instance of an object".
            var uniqueMessage = (origFormatString ?? message).Trim(); // Trim because we've witnessed some messages in the wild starting with a new line.

            // Sometimes the message can contain new lines; usually when the message contains a stack trace. We don't want that. We just want the message.
            var messageNewLineIndex = uniqueMessage.IndexOf(Environment.NewLine);
            if (messageNewLineIndex != -1)
            {
                uniqueMessage = uniqueMessage.Substring(0, messageNewLineIndex);
            }

            // For exceptions, we want group messages where the error is the same.
            // Two ArgumentNullExceptions from the same place in code should be grouped together by having the same hash.
            // Two ArgumentNullExceptions from different places in code should not be grouped together, and should have different hashes.
            if (exception != null && !string.IsNullOrEmpty(function))
            {
                uniqueMessage += " at " + function;
                if (!string.IsNullOrEmpty(file))
                {
                    uniqueMessage += " in " + file;
                    if (!string.IsNullOrEmpty(line))
                    {
                        uniqueMessage += " line " + line;
                    }
                }
            }

            var hash = CalculateHash(uniqueMessage);
            return (hash, uniqueMessage);
        }

        private (string function, string file, string line) ExtractMyFunctionAndLine(Exception error)
        {             
            if (error == null || error.StackTrace == null)
            {
                return (string.Empty, string.Empty, string.Empty);
            }

            var atText = "at ";
            var lineText = ":line ";
            var inText = " in ";
            var omittedStackLines = new[]
            {
                "at System.",
                "at Microsoft.",
                "at lambda_method",
                "at Raven."
            };

            var stackLines = error.StackTrace
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
            var appStackLines = stackLines
                .Where(l => l.StartsWith(atText) && omittedStackLines.All(omitted => !l.StartsWith(omitted)));
            var myFunctionWithLineNumber = appStackLines
                .Where(l => l.Contains(lineText))
                .FirstOrDefault();
            var bestFunction = myFunctionWithLineNumber ?? // Use the top-most stack line that has an app function in it and a line number in it.
                appStackLines.FirstOrDefault() ?? // No line number? use the top-most stack line that has an app function in it.
                stackLines.LastOrDefault(l => l.Contains(lineText)) ?? // No app function? Use the top-most stack line that has a line number in it.
                stackLines.LastOrDefault(); // No line number? Use the top-most function.
            if (bestFunction != null)
            {
                // OK, we have a stack line that ideally looks something like:
                // "at MyCompany.FooBar.Blah() in c:\builds\foobar.cs:line 625"
                var indexOfIn = bestFunction.IndexOf(inText, atText.Length);
                if (indexOfIn == -1)
                {
                    // There's no space after the function. The stack line is likely "at MyCompany.Foobar.Blah()".
                    // This means there's no file or line number. Use only the function name.
                    var function = FunctionWithoutNamespaces(bestFunction.Substring(atText.Length));
                    return (function, string.Empty, string.Empty);
                }
                else
                {
                    // We have " in " after the function name, so we have a file.
                    var function = FunctionWithoutNamespaces(bestFunction.Substring(atText.Length, indexOfIn - atText.Length));
                    var lineIndex = bestFunction.IndexOf(lineText);
                    var fileIndex = indexOfIn + inText.Length;
                    if (lineIndex == -1)
                    {
                        // We don't have a "line: 625" bit of text.
                        // We instead have "at MyCompany.FooBar.Blah() in c:\builds\foobar.cs"
                        var file = FileWithoutPath(bestFunction.Substring(fileIndex));
                        return (function, file, string.Empty);
                    }
                    else
                    {
                        // We have a line too. So, we have the ideal "at MyCompany.FooBar.Blah() in c:\builds\foobar.cs:line 625".
                        var file = FileWithoutPath(bestFunction.Substring(fileIndex, lineIndex - fileIndex));
                        var line = bestFunction.Substring(lineIndex + lineText.Length);
                        return (function, file, line);
                    }
                }
            }

            return (string.Empty, string.Empty, string.Empty);
        }

        // This function takes a fully qualified function and turns it into just the class and function name:
        // MyApp.Sample.HomeController.Foo() -> HomeController.Foo()
        private static string FunctionWithoutNamespaces(string function)
        {
            if (string.IsNullOrEmpty(function))
            {
                return string.Empty;
            }
            
            var period = ".";
            var parts = function.Split(new[] { period }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 2)
            {
                return string.Join(period, parts.Skip(parts.Length - 2));
            }

            return function;
        }

        // Takes a file path and returns just the file name.
        // c:\foo\bar.cs -> bar.cs
        private static string FileWithoutPath(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return string.Empty;
            }

            return System.IO.Path.GetFileName(file);
        }

        private string TryExtractFormatFromMessageWithDetails(string message, Dictionary<string, object> details)
        {
            // There is no {OriginalFormat}. Do we have details? Some log messages include details without the original format.
            // For example, some messages will be "Request finished in 2.9422ms", and it won't contain an {OriginalFormat}.
            // However, these messages will indeed contain the details, e.g. { "Elapsed": 2.9422 }, etc.
            // If the message contains all the .ToString values of the details, we can recreate the message, e.g. "Request finished in {Elapsed}"
            var detailStrings = details
                .Select(p => new KeyValuePair<string, string>(p.Key, p.Value?.ToString()))
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .ToList();
            var messageContainsAllDetails = detailStrings.Count > 0 && detailStrings.All(d => message.Contains(d.Value));
            if (messageContainsAllDetails)
            {
                var originalFormatBuilder = new StringBuilder(message.Length * 2);
                var runningIndex = 0;
                foreach (var detailPair in detailStrings)
                {
                    var detailIndex = message.IndexOf(detailPair.Value, runningIndex, StringComparison.Ordinal);
                    if (detailIndex != -1)
                    {
                        // Append the text from the last position up until the detail value.
                        originalFormatBuilder.Append(message, runningIndex, detailIndex - runningIndex);

                        // Append the key name inside of braces, rather than the value.
                        // e.g. "{Elapsed}", rather than "2.9422"
                        originalFormatBuilder.Append('{');
                        originalFormatBuilder.Append(detailPair.Key);
                        originalFormatBuilder.Append('}');

                        // Move the running index to past the value.
                        runningIndex = detailIndex + detailPair.Value.Length;
                    }
                }

                var originalFormat = originalFormatBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(originalFormat))
                {
                    return originalFormat;
                }
            }

            // We can't recreate the original format. Return null to indicate this.
            return null;
        }

        private IDictionary<string, object> ScopeToDictionary()
        {
            if (this.scopeOrNull == null)
            {
                return null;
            }

            var dictionary = new Dictionary<string, object>(this.scopeOrNull.Count * 3);
            var unnamedValues = 0;

            string GetNextNoNameKey()
            {
                for (var i = unnamedValues; i < 50; i++)
                {
                    var key = i.ToString();
                    if (!dictionary.ContainsKey(key))
                    {
                        unnamedValues++;
                        return key;
                    }
                }

                return Guid.NewGuid().ToString();
            }

            
            foreach (var scopeObj in this.scopeOrNull)
            {
                if (scopeObj.Value is Microsoft.Extensions.Logging.Internal.FormattedLogValues logValues)
                {
                    var key = GetNextNoNameKey();
                    dictionary.Add(key, scopeObj.Value.ToString());
                    foreach (var pair in logValues)
                    {
                        dictionary.Add(GetUniqueDictionaryKey(dictionary, key + "_" + pair.Key), pair.Value);
                    }
                }
                else if (scopeObj.Value is IEnumerable<KeyValuePair<string, object>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        dictionary.Add(GetUniqueDictionaryKey(dictionary, pair.Key), pair.Value);
                    }
                }
                else if (scopeObj.Value is KeyValuePair<string, object> pair)
                {
                    dictionary.Add(GetUniqueDictionaryKey(dictionary, pair.Key), pair.Value);
                }
                else
                {
                    dictionary.Add(GetNextNoNameKey(), scopeObj.Value);
                }
            }

            return dictionary;
        }

        private static string GetUniqueDictionaryKey(IDictionary<string, object> dictionary, string desired)
        {
            if (dictionary.ContainsKey(desired))
            {
                for (var i = 2; i < 50; i++) // We should be able to find a unique name within 50 tries. If not, we'll just use a GUID.
                {
                    var newName = desired + "_" + i.ToString();
                    if (!dictionary.ContainsKey(newName))
                    {
                        return newName;
                    }
                }

                return Guid.NewGuid().ToString();
            }

            return desired;
        }

        private static int CalculateHash(string input)
        {
            // We can't use input.GetHashCode in .NET Core, as it can (and does!) return different values each time the app is run.
            // See https://github.com/dotnet/corefx/issues/19703
            // Instead, we've implemented the following deterministic string hash algorithm: https://stackoverflow.com/a/5155015/536
            unchecked
            {
                int hash = 23;
                foreach (char c in input)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        // NOTE: this method is called on a background thread. Don't touch class members.
        private static void TryWriteLogBatch(IList<Log> logs, IDocumentStore db)
        {
            if (logs.Count > 0)
            {
                // This occurs on a background thread. Eat any exception.
                try
                {
                    WriteLogBatch(logs, db);
                }
                catch (Exception error)
                {
                    Console.WriteLine("Unable to write logs to Raven. Possibly disconnected. {0}", error.ToString());
                }
            }
        }

        // NOTE: this method is called on a background thread. Don't touch class members. 
        private static void WriteLogBatch(IList<Log> logs, IDocumentStore db, bool hasRetried = false)
        {
            // .db is a class member, but it's OK to access because it is a thread-safe class.
            using (var dbSession = db.OpenSession())
            {
                var structuredLogIds = logs
                    .Select(l => (id: GetStructuredLogId(l), log: l))
                    .ToList();
                var structuredLogs = dbSession.Load<StructuredLog>(structuredLogIds.Select(l => l.id));
                foreach (var structuredLogInfo in structuredLogIds)
                {
                    var existingStructuredLog = structuredLogs[structuredLogInfo.id];
                    if (existingStructuredLog == null)
                    {
                        existingStructuredLog = new StructuredLog();
                        dbSession.Store(existingStructuredLog, structuredLogInfo.id);
                    }

                    existingStructuredLog.AddLog(structuredLogInfo.log);

                    // Update the expiration time for this structured log.
                    var meta = dbSession.Advanced.GetMetadataFor(existingStructuredLog);
                    var expireDateIsoString = DateTime.UtcNow.AddDays(RavenStructuredLoggerProvider.ExpirationInDays).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                    meta["@expires"] = expireDateIsoString;
                }

                try
                {
                    dbSession.SaveChanges();
                }
                catch (Client.Exceptions.Documents.Session.NonUniqueObjectException) when (hasRetried == false)
                {
                    // This exception can happen in a small race condition: 
                    // If we check for existing ID, it's not there, then another thread saves it with that ID, then we try to save with the same ID.
                    // When this race condition happens, retry if we haven't already.
                    WriteLogBatch(logs, db, true);
                }
            }
        }

        private static string GetStructuredLogId(Log log)
        {
            return "StructuredLogs/" + log.TemplateHash;
        }
    }
}
