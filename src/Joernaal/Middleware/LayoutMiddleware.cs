// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal.Middleware
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class LayoutMiddleware
    {
        private readonly ILogger _logger;
        private readonly ProcessDelegate _next;

        public LayoutMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger("Template");
        }

        public async Task Invoke(JoernaalContext context)
        {
            var item = context.Item;
            var extension = Path.GetExtension(item.TargetFullPath);
            if ((extension == ".html" || extension == ".htm") && context.Phase == ProcessingPhase.Conversion)
            {
                var source = Encoding.UTF8.GetString(item.Contents);
                var trimmedSource = source.Trim();

                if (!trimmedSource.StartsWith("<DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
                    !trimmedSource.StartsWith("<HTML", StringComparison.OrdinalIgnoreCase))
                {
                    var writer = new StringWriter();
                    var value = item.Properties.Site?.Value;
                    string title = string.Join(" &sect; ", item.Properties.Title?.Value, value).Trim();
                    writer.Write($@"<!DOCTYPE html>
<html lang=""en-us"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{title}</title>
</head>
<body>
    <header>
        <h1>Werner Strydom</h1>
    </header>
    <main>{source}</main>
    <footer>
        <p>Copyright 2017 Werner Strydom</p>
    </footer>
</body>
</html>");

                    item.Contents = Encoding.UTF8.GetBytes(writer.ToString());

                    _logger.LogInformation("Wrapping HTML doucment in layout", item.TargetPath);
                }
            }

            await _next(context);
        }
    }
}