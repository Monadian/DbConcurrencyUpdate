using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CocurentTransaction.Db;
using MassTransit;
using ConcurrentTransaction.Consumers;
using ConcurrentTransaction.Models.Messages;
using GreenPipes;

namespace CocurentTransaction
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ConcurentTransaction", Version = "v1" });
            });

            var connectionString = "Server=localhost; Database=wallet; User Id = sa; Password = Abc123456;";
            //services.AddDbContext<WalletContext>(options => options.UseSqlServer(connectionString));

            // https://docs.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cwith-constant#dbcontext-pooling
            services.AddDbContextPool<WalletContext>(options => options.UseSqlServer(connectionString));

            services.AddMassTransit(x =>
            {
                x.AddConsumer<RechargeWalletConsumer>();

                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ReceiveEndpoint("recharge-wallet-listener", e =>
                    {
                        e.ConcurrentMessageLimit = 2;
                        e.UseMessageRetry(r => r.Interval(2, 100));
                        e.Consumer<RechargeWalletConsumer>(ctx);
                    });
                });

                // For testing without RabbitMQ
                //x.UsingInMemory((context, cfg) =>
                //{
                //    cfg.ConfigureEndpoints(context);
                //});

                x.AddRequestClient<RechargeWallet>();
            });

            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CocurentTransaction v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
