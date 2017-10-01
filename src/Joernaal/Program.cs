// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;
    using System.IO;
    using System.Net.Mime;
    using Microsoft.Extensions.CommandLineUtils;

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {

                CommandLineApplication application = new CommandLineApplication();
                application.OnExecute(() =>
                {
                    application.ShowHelp();
                    return 1;
                });

                application.Command("build", app =>
                {
                    var sourcePathOption = app.Option("--source|-s", "Source Path", CommandOptionType.SingleValue);
                    var targetPathOption = app.Option("--output|-o", "Output Path", CommandOptionType.SingleValue);
                    var themePathOption = app.Option("--theme|-t", "Theme Path", CommandOptionType.SingleValue);

                    app.OnExecute(() =>
                    {
                        var host = new ApplicationHostBuilder()
                                .UseStartup<Startup>()
                                .Build();

                        var sourcePath = sourcePathOption.Value();
                        var targetPath = targetPathOption.Value();
                        var themesPath = themePathOption.Value();
                        return host.Run(sourcePath, targetPath, themesPath);
                    });
                });

                return application.Execute(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }
    }
}