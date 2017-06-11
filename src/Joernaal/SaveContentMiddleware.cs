using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Joernaal
{
    public class SaveContentMiddleware
    {
        private readonly ProcessDelegate _next;
        private readonly ILogger _logger;

        public SaveContentMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(nameof(Joernaal));
        }

        public async Task Invoke(HttpContext context)
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

            await _next(context);
        }
    }
}