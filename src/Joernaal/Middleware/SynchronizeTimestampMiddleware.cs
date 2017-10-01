namespace Joernaal
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class SynchronizeTimestampMiddleware
    {
        private readonly ProcessDelegate _next;
        private readonly ILogger _logger;

        public SynchronizeTimestampMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(nameof(Joernaal));
        }

        public async Task Invoke(JoernaalContext context)
        {
            if (context.Phase == ProcessingPhase.Saving)
            {
                var item = context.Item;

                var creationTimeUtc = File.GetCreationTimeUtc(item.SourceFullPath);
                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(item.SourceFullPath);
                File.SetCreationTimeUtc(item.TargetFullPath, creationTimeUtc);
                File.SetLastWriteTimeUtc(item.TargetFullPath, lastWriteTimeUtc);

                _logger.LogInformation("Synchronized timestamps between '{SourcePath}' and '{TargetPath}'",
                    item.SourceFullPath, item.TargetFullPath);
            }
            await _next(context);
        }
    }
}