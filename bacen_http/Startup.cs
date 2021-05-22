using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace bacen_http
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
            services.AddRazorPages();
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
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/new_pix", async context =>
                {
                    DateTime now = DateTime.Now;
                    Int64 now_milis = now.Ticks;
                    var rand = new Random();
                    int operation_count = rand.Next(1, 11);
                    string bacen_message = "{\"message_id\":\""+now_milis+"\",\"operation_count\":\""+operation_count+"\",\"operations\":[";
                    for (int i=0; i<operation_count; i++){
                        int own_transaction = rand.Next(0, 2);
                        string operation = "{\"operation_id\":\""+DateTime.Now.Ticks.ToString()+"\",\"operation_type\":\""+own_transaction+"\",\"pix_ammount\":\""+rand.Next(1, 1000)+"\"}";
                        bacen_message += operation;
                        if (i!=operation_count-1) bacen_message += ",";
                    }
                    bacen_message += "]}";
                    
                    await context.Response.WriteAsync(bacen_message);
                });
            });
        }
    }
}
