using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Joernaal.Middleware;
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

        private async Task<int> RunAsync(IReadOnlyList<string> args)
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
            builder.UseMiddleware<UpdateReferencesMiddleware>();

            var request = builder.Build();

            var collection = CreateCollection(args[0]);

            foreach (var phase in Enum<ProcessingPhase>.GetValues())
            {
                foreach (var item in collection.Items)
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var context = new JoernaalContext(item, scope);
                        context.Phase = phase;
                        await request(context);
                    }
                }
            }

            return 0;
        }

        private static Collection CreateCollection(string basePath)
        {
            var collection = new Collection(basePath);

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("**/*");
            var directory = new DirectoryInfoWrapper(new DirectoryInfo(collection.SourcePath));
            var result = matcher.Execute(directory);
            if (!result.HasMatches)
                return collection;

            foreach (var file in result.Files)
            {
                if (file.Path.Contains(".joernaal/"))
                {
                    continue;
                }
                collection.CreateItem(file.Path);
            }
            return collection;
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

    public static class Enum<T> where T : struct, IComparable, IFormattable, IConvertible
    {
        public static IEnumerable<T> GetValues()
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static IEnumerable<string> GetNames()
        {
            return Enum.GetNames(typeof(T));
        }
    }
}