using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Joernaal
{
    public class LayoutMiddleware
    {
        private readonly ProcessDelegate _next;
        private ILogger _logger;

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
                    writer.Write($@"<!DOCTYPE html>
<html lang=""en-us"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{item.Metadata.Title}</title>
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