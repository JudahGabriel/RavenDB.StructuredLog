using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace Raven.StructuredLogger
{
    /// <summary>
    /// Extensions for IDocumentStore that aid in Raven.StructuredLogger.
    /// </summary>
    public static class DocumentStoreExtensions
    {
        /// <summary>
        /// Instructs Raven to ignore self-referencing loops. This must be called *BEFORE* docStore.Initialize().
        /// </summary>
        /// <param name="db">The database.</param>
        public static void IgnoreSelfReferencingLoops(this IDocumentStore db)
        {
            if (db.Conventions.Serialization is Raven.Client.Json.Serialization.NewtonsoftJson.NewtonsoftJsonSerializationConventions ravenSerializationConv)
            {
                ravenSerializationConv.CustomizeJsonSerializer = newtonSoft => newtonSoft.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            }
        }
    }
}
