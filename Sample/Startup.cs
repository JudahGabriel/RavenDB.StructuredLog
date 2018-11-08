using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.StructuredLog;
using Raven.Client;
using Raven.Client.Documents;
using Microsoft.AspNetCore.Mvc;
using Sample.Common;

namespace Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create our Raven Structured Logger.
            var raven = this.CreateRavenDocStore();
            services.AddLogging(builder => builder.AddRavenStructuredLogger(raven));

            // Alternately, call .AddRavenStructuredLogger() without parameters to
            // instruct the logger to find your IDocumentStore via dependency injection services.
            //services.AddSingleton(raven);
            //services.AddLogging(builder => builder.AddRavenStructuredLogger());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private IDocumentStore CreateRavenDocStore()
        {
            var docStore = new DocumentStore
            {
                Urls = new[] { "http://live-test.ravendb.net" },
                Database = "Raven.StructuredLog.Sample"
            };
            docStore.Initialize();
            docStore.EnsureExists();
            return docStore;
        }
    }
}
