using System.IO;
using Itinero.Intermodal.API.Staging;
using Itinero.Transit.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itinero.Intermodal.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public static Router Router { get; private set; }
        
        public static TransitDb TransitDb { get; private set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var transitDb = BuildTransitDb.BuildOrLoad();

            RouterDb routerDb = null;
            using (var routerDbStream = File.OpenRead(
                Configuration.GetValue<string>("routerdb")))
            {
                routerDb = RouterDb.Deserialize(routerDbStream);
            }
            
            Startup.Router = new Router(routerDb);
            Startup.TransitDb = transitDb;
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}