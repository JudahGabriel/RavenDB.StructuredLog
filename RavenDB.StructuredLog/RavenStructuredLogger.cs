using Microsoft.Extensions.Logging;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace RavenDB.StructuredLog
{
    /// <summary>
    /// Log provider that sends messages to RavenDB asynchronously.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RavenStructuredLogger : ILogger
    {
        private readonly Subject<Log> logs = new Subject<Log>();
        private readonly string categoryName;
        private List<RavenStructuredLogScope> scopeOrNull;

        public RavenStructuredLogger(string categoryName, IDocumentStore db)
        {
            this.categoryName = categoryName;
            this.logs
                .GroupByUntil(logs => 1, _ => Observable.Timer(TimeSpan.FromSeconds(2))) // If we log a bunch in quick succession, batch them together and log in a single trip to the DB.
                .SelectMany(logs => logs.ToList())
                .SubscribeOn(System.Reactive.Concurrency.TaskPoolScheduler.Default)
                .Subscribe(logs => TryWriteLogBatch(logs, db));
        }

        /// <summary>
        /// Begins a logical scope to the logger, ass
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            if (this.scopeOrNull == null)
            {
                this.scopeOrNull = new List<RavenStructuredLogScope>(2);
            }

            var scope = new RavenStructuredLogScope(state);
            scope.Disposed.Subscribe(_ => this.scopeOrNull.Remove(scope));
            scopeOrNull.Add(scope);

            return scope;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
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
                var structuredLogHash = GenerateStructuredHash(message, exception, details);
                var log = new Log
                {
                    Created = DateTime.UtcNow,
                    Exception = exception,
                    Message = message,
                    TemplateValues = details,
                    Level = logLevel,
                    Category = categoryName,
                    EventId = eventId.Id == 0 && eventId.Name == null ? new Nullable<EventId>() : eventId,
                    Template = structuredLogHash.message != message ? structuredLogHash.message : null, // Fill in the template only if it differs from the message.
                    TemplateHash = structuredLogHash.hash,
                    Scope = this.ScopeToDictionary()
                };
                this.logs.OnNext(log);
            }
        }

        private (int hash, string message) GenerateStructuredHash(string message, Exception exception, Dictionary<string, object> details)
        {
            // Do we have an "{OriginalFormat}" in the details? If so, use that to generate the log structured hash.
            // {OriginalFormat} is the raw string template. It will look something like this: "Generated {length} new items".
            // We use that for hte structured log hash, so that it will group both of the following messages together in a single StructuredLog: 
            //  - "Generated 5 new items" 
            //  - "Generated 2 new items"
            var origFormatString = default(string);
            if (details != null && details.TryGetValue("{OriginalFormat}", out var origFormat))
            {
                origFormatString = origFormat as string;
            }

            var uniqueMessage = origFormatString ?? exception?.ToString() ?? message;
            return (CalculateHash(uniqueMessage), uniqueMessage);
        }

        private IDictionary<string, object> ScopeToDictionary()
        {
            if (this.scopeOrNull == null)
            {
                return null;
            }

            var dictionary = new Dictionary<string, object>(this.scopeOrNull.Count * 3);
            var unnamedValues = 0;

            string GetUniqueScopeName(string desired)
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
                        dictionary.Add(GetUniqueScopeName(key + "_" + pair.Key), pair.Value);
                    }
                }
                else if (scopeObj.Value is IEnumerable<KeyValuePair<string, object>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        dictionary.Add(GetUniqueScopeName(pair.Key), pair.Value);
                    }
                }
                else if (scopeObj.Value is KeyValuePair<string, object> pair)
                {
                    dictionary.Add(GetUniqueScopeName(pair.Key), pair.Value);
                }
                else
                {
                    dictionary.Add(GetNextNoNameKey(), scopeObj.Value);
                }
            }

            return dictionary;
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
        private static void WriteLogBatch(IList<Log> logs, IDocumentStore db)
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
                    // Not a remote call, not SELECT N+1: we loaded these outside the loop.
                    var existingStructuredLog = dbSession.Load<StructuredLog>(structuredLogInfo.id);
                    if (existingStructuredLog == null)
                    {
                        existingStructuredLog = new StructuredLog();
                        dbSession.Store(existingStructuredLog, structuredLogInfo.id);
                    }

                    existingStructuredLog.AddLog(structuredLogInfo.log);
                }

                dbSession.SaveChanges();
            }
        }

        private static string GetStructuredLogId(Log log)
        {
            return "StructuredLogs/" + log.TemplateHash;
        }
    }
}
