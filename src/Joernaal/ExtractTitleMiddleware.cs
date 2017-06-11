using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;

namespace Joernaal
{
    public class ExtractTitleMiddleware
    {
        private readonly ProcessDelegate _next;
        private ILogger _logger;

        public ExtractTitleMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(nameof(Joernaal));
        }

        public async Task Invoke(HttpContext context)
        {
            var parser = new HtmlParser();
            var item = context.Item;
            var source = Encoding.UTF8.GetString(item.Contents);
            var document = parser.Parse(source);

            string title = null;
            for (int header = 1; header < 7; header++)
            {
                var cellSelector = $"h{header}";
                var cells = document.QuerySelectorAll(cellSelector);
                title = cells.Select(m => m.TextContent).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                item.Metadata.Title = title;
                _logger.LogInformation("Extracting title '{Title}' from '{Path}'", title, item.TargetPath);
            }

            await _next(context);
        }
    }
}