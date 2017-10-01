// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal.Middleware
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using CommonMark;
    using Microsoft.Extensions.Logging;

    public class MarkdownMiddleware
    {
        private readonly ILogger _logger;
        private readonly ProcessDelegate _next;

        public MarkdownMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger("Markdown");
        }

        public async Task Invoke(JoernaalContext context)
        {
            var item = context.Item;

            var extension = Path.GetExtension(item.TargetPath);
            if ((string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase))
                && context.Phase == ProcessingPhase.Conversion)
            {
                using (var sourceStream = new MemoryStream(item.Contents))
                {
                    using (var targetStream = new MemoryStream())
                    {
                        using (var reader = new StreamReader(sourceStream, Encoding.UTF8))
                        {
                            using (var writer = new StreamWriter(targetStream))
                            {
                                var settings = CommonMarkSettings.Default.Clone();
                                CommonMarkConverter.Convert(reader, writer, settings);
                            }
                        }
                        item.Contents = targetStream.ToArray();
                    }
                }
                item.TargetPath = Path.ChangeExtension(item.TargetPath, "html");
                _logger.LogInformation("Converted '{SourcePath}' to '{TargetPath}'", item.SourceFullPath,
                    item.TargetFullPath);
            }
            else
            {
                _logger.LogTrace("Skipping: {Path}", item.TargetPath);
            }

            await _next(context);
        }
    }
}