// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

    public class ApplicationHost
    {
        private readonly ProcessDelegate _request;

        public ApplicationHost(IServiceProvider serviceProvider, ProcessDelegate request)
        {
            _request = request;
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public int Run(string sourcePath, string targetPath, string themesPath)
        {
            return RunAsync(sourcePath, targetPath, themesPath).GetAwaiter().GetResult();
        }

        public async Task<int> RunAsync(string sourcePath, string targetPath, string themesPath)
        {
            // TODO: Support Parameters
            var collection = CreateCollection(sourcePath, targetPath, themesPath);
            foreach (var phase in Enum<ProcessingPhase>.GetValues())
            {
                foreach (var item in collection.Items)
                    using (var scope = ServiceProvider.CreateScope())
                    {
                        var context = new JoernaalContext(item, scope);
                        context.Phase = phase;
                        await _request(context);
                    }
            }
            return 0;
        }

        private static Collection CreateCollection(string sourcePath, string targetPath, string themesPath)
        {
            var collection = new Collection(sourcePath, targetPath, themesPath);

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("**/*");
            var directory = new DirectoryInfoWrapper(new DirectoryInfo(collection.SourcePath));
            var result = matcher.Execute(directory);
            if (!result.HasMatches)
                return collection;

            foreach (var file in result.Files)
            {
                if (file.Path.Contains(".joernaal/"))
                    continue;
                collection.CreateItem(file.Path);
            }
            return collection;
        }
    }
}