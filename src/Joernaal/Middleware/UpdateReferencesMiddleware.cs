// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal.Middleware
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AngleSharp;
    using AngleSharp.Dom;
    using AngleSharp.Parser.Html;
    using Microsoft.Extensions.Logging;

    public class UpdateReferencesMiddleware
    {
        private readonly ILogger _logger;
        private readonly ProcessDelegate _next;

        public UpdateReferencesMiddleware(ProcessDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(nameof(Joernaal));
        }

        public async Task Invoke(JoernaalContext context)
        {
            if (context.Phase == ProcessingPhase.UpdatingReferences)
            {
                var mapping =
                        context.Item.Parent.Items.ToDictionary(element => element.SourcePath,
                            element => element.TargetPath);

                var parser = new HtmlParser();
                var item = context.Item;
                var extension = Path.GetExtension(item.TargetPath);
                if (extension == ".html" || extension == ".htm")
                {
                    var source = Encoding.UTF8.GetString(item.Contents);
                    var document = parser.Parse(source);

                    var modified = false;
                    modified |= UpdateReferences(document, mapping, "a", "href");
                    modified |= UpdateReferences(document, mapping, "img", "source");
                    modified |= UpdateReferences(document, mapping, "link", "href");
                    modified |= UpdateReferences(document, mapping, "script", "src");
                    if (modified)
                    {
                        string sourceText;
                        using (var writer = new StringWriter())
                        {
                            var formatter = new AutoSelectedMarkupFormatter();
                            document.DocumentElement.ToHtml(writer, formatter);
                            sourceText = writer.ToString();
                        }
                        item.Contents = Encoding.UTF8.GetBytes(sourceText);
                        _logger.LogInformation("Updated references in '{Path}'", item.TargetPath);
                    }
                }
            }
            await _next(context);
        }

        private bool UpdateReferences(IParentNode document, IReadOnlyDictionary<string, string> mapping,
            string selector, string name)
        {
            var modified = false;
            var elements = document.QuerySelectorAll(selector);
            foreach (var element in elements)
            {
                var s = element.Attributes[name].Value;
                if (!mapping.TryGetValue(s, out string x))
                    continue;

                element.SetAttribute(name, x);
                modified = true;
            }
            return modified;
        }
    }
}