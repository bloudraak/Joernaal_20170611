// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Middleware;

    public class Startup : IStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<JoernaalContext>();
        }

        public void Configure(IApplicationBuilder builder, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            builder.UseMiddleware<MarkdownMiddleware>();
            builder.UseMiddleware<ExtractTitleMiddleware>();
            builder.UseMiddleware<LayoutMiddleware>();
            builder.UseMiddleware<SaveContentMiddleware>();
            builder.UseMiddleware<SynchronizeTimestampMiddleware>();
            builder.UseMiddleware<UpdateReferencesMiddleware>();
        }
    }
}