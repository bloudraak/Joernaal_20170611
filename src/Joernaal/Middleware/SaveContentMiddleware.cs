// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal.Middleware
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class SaveContentMiddleware
    {
        private readonly ILogger _logger;
        private readonly ProcessDelegate _next;

        public SaveContentMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(nameof(Joernaal));
        }

        public async Task Invoke(JoernaalContext context)
        {
            if (context.Phase == ProcessingPhase.Saving)
            {
                var item = context.Item;
                var path = item.TargetFullPath;
                var directoryName = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryName))
                {
                    _logger.LogInformation("Creating directory '{path}'", directoryName);
                    Directory.CreateDirectory(directoryName);
                }

                _logger.LogInformation("Saving '{path}'", path);
                File.WriteAllBytes(path, item.Contents);
            }
            await _next(context);
        }
    }
}