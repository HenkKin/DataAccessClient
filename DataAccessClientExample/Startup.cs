using DataAccessClient;
using DataAccessClient.EntityFrameworkCore.SqlServer;
using DataAccessClientExample.DataLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataAccessClientExample
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
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            services.AddSingleton<IUserIdentifierProvider<int>, ExampleUserIdentifierProvider>();
            services.AddSingleton<ITenantIdentifierProvider<int>, ExampleTenantIdentifierProvider>();
            services.AddSingleton<ISoftDeletableConfiguration, ExampleSoftDeletableConfiguration>();
            services.AddSingleton<IMultiTenancyConfiguration<int>, ExampleMultiTenancyConfiguration>();

            services.AddDataAccessClientPool<ExampleDbContext, int>(builder =>
                    builder
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ExampleDataAccessClient;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"),
                new[] {typeof(ExampleEntity)});

            services.AddDataAccessClientPool<ExampleSecondDbContext, int>(builder =>
                    builder
                        .EnableSensitiveDataLogging()
                        .EnableDetailedErrors()
                        .UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ExampleSecondDataAccessClient;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"),
                new[] {typeof(ExampleSecondEntity)});

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
