using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.StructuredLogger;
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
            // Using Raven.StructuredLog is simple:
            // 1. Create your doc store and add it as a singleton
            // 2. Recommended: call docStore.IgnoreSelfReferencingLoops() 
            // 3. Call services.AddRavenStructuredLogger()

            // 1. and 2. Create the doc store, along with .IgnoreSelfReferencingLoops().
            var raven = this.CreateRavenDocStore();
            services.AddSingleton(raven);

            // 3. Tell ASP.NET to use Raven Structured Log for logging.
            services.AddRavenStructuredLogger();

            // Alternately, call .AddRavenStructuredLogger() without parameters to
            // instruct the logger to find your IDocumentStore via dependency injection services.
            //services.AddSingleton(raven);
            //services.AddLogging(builder => builder.AddRavenStructuredLogger());
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private IDocumentStore CreateRavenDocStore()
        {
            var docStore = new DocumentStore
            {
                Urls = new[] { "http://live-test.ravendb.net" },
                Database = "Raven.StructuredLog.Sample"
            };

            // Raven Structured Log-specific:
            // Ignore self-referencing loops, otherwise Raven will attempt and fail to serialize logs that have self-referencing objects.
            docStore.IgnoreSelfReferencingLoops();

            docStore.Initialize();
            docStore.EnsureExists();

            return docStore;
        }
    }
}
