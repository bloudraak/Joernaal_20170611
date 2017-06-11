using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using CommonMark;
using Microsoft.AspNetCore.Razor;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyModel;
using System.Collections.Immutable;
using NUglify.Html;

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

                var sourcePath = args[0];
                if (!Directory.Exists(sourcePath))
                {
                    Console.Error.WriteLine("The directory '{0}' does not exist.", sourcePath);
                }

                var targetPath = Path.Combine(sourcePath, "..", "dist");
                var intemediatePath = Path.Combine(sourcePath, "..", "obj");

                // TODO: We don't want to delete everything; this isn't good for cache busting
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }

                if (Directory.Exists(intemediatePath))
                {
                    Directory.Delete(intemediatePath, true);
                }

                var program = new Program();
                return program.RunAsync(sourcePath, intemediatePath, targetPath)
                    .GetAwaiter()
                    .GetResult();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

        private async Task<int> RunAsync(string sourcePath, string intemediatePath, string targetPath)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("**/*");


            var directory = new DirectoryInfoWrapper(new DirectoryInfo(sourcePath));
            var result = matcher.Execute(directory);
            if (!result.HasMatches)
            {
                await Console.Out.WriteLineAsync("No files found");
                return 1;
            }

            var templateType = CreateTemplate();

            foreach (var file in result.Files)
            {
                string outputPath = null;
                Directory.CreateDirectory(Path.Combine(targetPath, Path.GetDirectoryName(file.Path)));
                switch (Path.GetExtension(file.Path))
                {
                    case ".md":
                    case ".markdown":
                        outputPath = Path.ChangeExtension(Path.Combine(targetPath, file.Path), "html");
                        using (Stream sourceStream = File.OpenRead(Path.Combine(sourcePath, file.Path)))
                        using (Stream targetStream = File.Create(outputPath))
                        using (var reader = new StreamReader(sourceStream, Encoding.UTF8))
                        using (var writer = new StreamWriter(targetStream))
                        {
                            var w = new StringWriter();
                            CommonMarkConverter.Convert(reader, w);

                            var template = (TemplateBase)Activator.CreateInstance(templateType);
                            HtmlSettings htmlSettings = new HtmlSettings()
                                {
                                    PrettyPrint = false,
                                    IsFragmentOnly = false,
                                    
                                };
                            var r = NUglify.Uglify.Html(w.ToString(), htmlSettings);
                            var s = r.Code ?? w.ToString();
                            template.Body = w.ToString();
                            await template.ExecuteAsync();

                            await writer.WriteAsync(template.Source);
                        }

                        break;

                    default:
                        outputPath = Path.Combine(targetPath, file.Path);
                        File.Copy(file.Path, outputPath, true);
                        break;
                }

                var creationTime = File.GetCreationTime(Path.Combine(sourcePath, file.Path));
                var lastWriteTime = File.GetLastWriteTime(Path.Combine(sourcePath, file.Path));
                File.SetCreationTime(outputPath, creationTime);
                File.SetLastWriteTime(outputPath, lastWriteTime);
            }

            return 0;
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

            //
            //
            // 
            if (generatedAssembly != null)
            {
                var type = generatedAssembly.GetType("Joernaal.Generated.Layout");
                return type;
            }
            return null;
        }

        private IEnumerable<string> GetSourceFiles(RazorTemplateEngine engine, Assembly assembly)
        {
            EmbeddedFileProvider fp = new EmbeddedFileProvider(assembly, typeof(Program).Namespace + ".Themes");
            var c = fp.GetDirectoryContents("");
            foreach (var resource in c)
            {
                using (var sourceStream = assembly.GetManifestResourceStream("Joernaal.Themes." + resource.Name))
                using (var reader = new StreamReader(sourceStream))
                {
                    var razorResult = engine.GenerateCode(reader);
                    string source = razorResult.GeneratedCode;

                    //string path = Path.Combine(Path.GetTempPath(), nameof(Joernaal), "Source", resource.Name + ".cs");
                    //Directory.CreateDirectory(Path.GetDirectoryName(path));
                    //File.WriteAllText(path, source );
                    yield return source;
                }
            }
        }

        private Assembly CompileTemplate(IEnumerable<SyntaxTree> trees)
        {
            Assembly generatedAssembly = null;

            string path = Path.Combine(Path.GetTempPath(), nameof(Joernaal), nameof(Joernaal) + ".Themes.Generated.dll" );
            var compilation = CreateCompilation(trees, path);
            var compilationResult = compilation.Emit(path);
            if (compilationResult.Success)
            {
                generatedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            else
            {
                foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
                {
                    string issue = $"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: { codeIssue.Location.GetLineSpan()}, Severity: { codeIssue.Severity}";
                    Console.WriteLine(issue);
                }
            }

            return generatedAssembly;
        }

        private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> trees, string path)
        {
            var assembly = Assembly.GetEntryAssembly(); ;
            var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);

            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(Path.GetFileName(path))
                .WithOptions(options)
                .AddReferences(references)
                .AddSyntaxTrees(trees);

            return compilation;
        }
    }

    public abstract class TemplateBase
    {
        StringBuilder builder = new StringBuilder();
        string _body;

        public string Body { get => _body; set => _body = value; }

        public virtual void Write(object value)
        {
            // Escape HTML?
            builder.Append(value);
        }

        public virtual void WriteLiteral(object value)
        {
            builder.Append(value);
        }

        public virtual Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }

        public string RenderBody()
        {
            return Body;
        }

        public string Source { get { return builder.ToString(); } }
    }
}