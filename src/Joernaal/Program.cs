using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using CommonMark;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;

namespace Joernaal
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length <= 0)
                {
                    Console.Error.WriteLine("Usage:");
                    Console.Error.WriteLine("    joernaal <path>");
                    return 1;
                }

                var program = new Program();
                return program.RunAsync(args)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

        private async Task<int> RunAsync(string[] args)
        {
            IServiceCollection services = new ServiceCollection();

            // Startup::Configure
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            IApplicationBuilder builder = new ApplicationBuilder(serviceProvider);

            // Startup::ConfigureServices
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            builder.UseMiddleware<MarkdownMiddleware>();
            builder.UseMiddleware<ExtractTitleMiddleware>();
            builder.UseMiddleware<LayoutMiddleware>();
            builder.UseMiddleware<SaveContentMiddleware>();
            builder.UseMiddleware<SynchronizeTimestampMiddleware>();

            var request = builder.Build();

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("**/*");

            var collection = new Collection(args[0]);

            var directory = new DirectoryInfoWrapper(new DirectoryInfo(collection.SourcePath));
            var result = matcher.Execute(directory);
            if (!result.HasMatches)
            {
                await Console.Out.WriteLineAsync("No files found");
                return 1;
            }

            //var templateType = CreateTemplate();

            
            foreach (var file in result.Files)
            {
                collection.CreateItem(file.Path);
            }

            foreach (var item in collection.Items)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = new HttpContext(item, scope);
                    await request(context);
                }
            }
            
            //foreach (var item in collection.Items)
            //    await Process(templateType, item);

            return 0;
        }

        private async Task Process(Type templateType, Item item)
        {
            string outputPath;

            Directory.CreateDirectory(Path.GetDirectoryName(item.TargetPath));
            switch (Path.GetExtension(item.TargetPath))
            {
                case ".md":
                case ".markdown":
                    outputPath = Path.ChangeExtension(item.TargetPath, "html");
                    using (Stream sourceStream = File.OpenRead(item.SourcePath))
                    using (Stream targetStream = File.Create(outputPath))
                    using (var reader = new StreamReader(sourceStream, Encoding.UTF8))
                    using (var writer = new StreamWriter(targetStream))
                    {
                        var w = new StringWriter();
                        CommonMarkConverter.Convert(reader, w);

                        var template = (TemplateBase) Activator.CreateInstance(templateType);
                        
                        template.Body = w.ToString();
                        await template.ExecuteAsync();
                        await writer.WriteAsync(template.Source);
                    }

                    break;

                default:
                    outputPath = item.TargetPath;
                    File.Copy(item.SourcePath, outputPath, true);
                    break;
            }

            var creationTime = File.GetCreationTime(item.SourcePath);
            var lastWriteTime = File.GetLastWriteTime(item.SourcePath);
            File.SetCreationTime(outputPath, creationTime);
            File.SetLastWriteTime(outputPath, lastWriteTime);
        }

        private Type CreateTemplate()
        {
            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language)
            {
                DefaultBaseClass = nameof(TemplateBase),
                DefaultClassName = "Layout",
                DefaultNamespace = "Joernaal.Generated"
            };
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.IO");
            var engine = new RazorTemplateEngine(host);
            var assembly = Assembly.GetEntryAssembly();
            var sourceFiles = GetSourceFiles(engine, assembly).ToImmutableList();
            var trees = sourceFiles.Select(source => CSharpSyntaxTree.ParseText(source)).ToImmutableList();
            var generatedAssembly = CompileTemplate(trees);

            return generatedAssembly?.GetType("Joernaal.Generated.Layout");
        }

        private IEnumerable<string> GetSourceFiles(RazorTemplateEngine engine, Assembly assembly)
        {
            var fp = new EmbeddedFileProvider(assembly, typeof(Program).Namespace + ".Themes");
            var c = fp.GetDirectoryContents("");
            foreach (var resource in c)
                using (var sourceStream = assembly.GetManifestResourceStream("Joernaal.Themes." + resource.Name))
                using (var reader = new StreamReader(sourceStream))
                {
                    var razorResult = engine.GenerateCode(reader);
                    var source = razorResult.GeneratedCode;

                    yield return source;
                }
        }

        private Assembly CompileTemplate(IEnumerable<SyntaxTree> trees)
        {
            Assembly generatedAssembly = null;

            var path = Path.Combine(Path.GetTempPath(), nameof(Joernaal), nameof(Joernaal) + ".Themes.Generated.dll");
            var compilation = CreateCompilation(trees, path);
            var compilationResult = compilation.Emit(path);
            if (compilationResult.Success)
                generatedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            else
                foreach (var codeIssue in compilationResult.Diagnostics)
                {
                    var issue =
                        $"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: {codeIssue.Location.GetLineSpan()}, Severity: {codeIssue.Severity}";
                    Console.WriteLine(issue);
                }

            return generatedAssembly;
        }

        private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> trees, string path)
        {
            var trustedAssembliesPaths =
                ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);

            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(Path.GetFileName(path))
                .WithOptions(options)
                .AddReferences(references)
                .AddSyntaxTrees(trees);

            return compilation;
        }
    }
}