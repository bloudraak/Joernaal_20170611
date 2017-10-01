// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class ApplicationHostBuilder
    {
        private Type _startupType;

        public ApplicationHostBuilder UseStartup<T>()
        {
            _startupType = typeof(T);
            return this;
        }

        public ApplicationHost Build()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IApplicationBuilder, ApplicationBuilder>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var startup = (IStartup) ActivatorUtilities.CreateInstance(serviceProvider, _startupType);
            startup.ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var builder = serviceProvider.GetService<IApplicationBuilder>();

            startup.Configure(builder, loggerFactory);

            var request = builder.Build();

            return ActivatorUtilities.CreateInstance<ApplicationHost>(serviceProvider, request);
        }
    }
}