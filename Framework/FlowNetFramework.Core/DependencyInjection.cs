﻿using FlowNetFramework.Data;
using FlowNetFramework.Logging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System.Reflection;
using System.Runtime.Loader;


namespace FlowNetFramework.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFlowNetFramework<T>(this IServiceCollection services, IConfiguration configuration, Microsoft.AspNetCore.Builder.ConfigureHostBuilder host, Action<DbContextOptionsBuilder> dbContextOptions) where T : DbContext
        {

            services.AddInfrastructureServices<T>(configuration, dbContextOptions);

            //services.AddFrameworkLogging(configuration, host);

            #region Assembly Works
            var assemblies = new List<Assembly>();
            assemblies.Add(Assembly.GetCallingAssembly());
            var assemblyNames = Assembly.GetCallingAssembly().GetReferencedAssemblies().Where(x => x.Name.StartsWith("FlowaDigital."));
            var loadContext = new AssemblyLoadContext("FlowNetFrameworkAssemblyLoadContext");
            foreach (var assemblyName in assemblyNames)
            {
                assemblies.Add(loadContext.LoadFromAssemblyName(assemblyName));
            }
            #endregion

            #region Scrutor
            services.Scan(selector => selector.FromAssemblies(assemblies.ToArray())
                                  .AddClasses(true)
                                  .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                                  .AsMatchingInterface()
                                  .WithScopedLifetime());
            #endregion

            #region MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies.ToArray()));
            #endregion

            #region AutoMapper
            services.AddAutoMapper(assemblies);
            #endregion

            #region FluentValidation
            services.AddValidatorsFromAssemblies(assemblies.ToArray());
            #endregion

            var isCORSEnabled = Convert.ToBoolean(configuration["Cors:Enabled"]);

            Console.WriteLine($"[INFO] FlowNet Framework || CORS Status: -> {(isCORSEnabled ? "Enabled" : "Disabled")}");

            if (isCORSEnabled)
            {
                var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
                string[] allowedOrigins = configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"[INFO] FlowNet Framework || CORS Allowed Origins: -> {(string.Join(",", allowedOrigins))}");
                services.AddCors(options =>
                {
                    options.AddPolicy(name: MyAllowSpecificOrigins,
                                      policy =>
                                      {
                                          policy.WithOrigins(allowedOrigins ?? new string[0])
                                          .AllowAnyMethod()
                                          .AllowAnyHeader()
                                          .AllowCredentials()
                                          .WithExposedHeaders("Authorization", "Set-Cookie", "Access-Control-Allow-Origin", "Access-Control-Allow-Headers");
                                      });
                });
            }

            services.AddHttpContextAccessor();

            services.AddAuthorization();

            return services;
        }
    }
}
