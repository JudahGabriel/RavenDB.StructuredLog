using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raven.StructuredLogger
{
    /// <summary>
    /// A IStartupFilter that configures the RavenDbStructuredLoggerProvider at startup.
    /// </summary>
    internal class RavenStructuredLoggerStartupFilter : IStartupFilter
    {
        /// <inheritdoc />
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                // Getting this service will force initialization of the provider.
                // See ServiceCollectionExtensions.AddLogger for more info.
                builder.ApplicationServices.GetRequiredService<RavenStructuredLoggerProvider>();
                next(builder);
            };
        }
    }
}
