#if !UNITY_2020_1_OR_NEWER

using Friflo.Json.Fliox.DB.AspNetCore;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Tests.Main
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            var database            = new MemoryDatabase();
            var hub                 = new FlioxHub(database);
            var hostHub             = new HttpHostHub (hub);
            hub.EventBroker         = new EventBroker(true);                    // optional. eventBroker enables Pub-Sub
            hostHub.requestHandler  = new RequestHandler("./Json.Tests/www");   // optional. Used to serve static web content

            app.UseRouting();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("hello/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                
                endpoints.Map("/{*path}", async context => {
                    await context.HandleFlioxHostRequest(hostHub);
                });
            });
        }
    }
}

#endif