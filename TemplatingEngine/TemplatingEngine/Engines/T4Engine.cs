using ExtensionMethods;
using Microsoft.VisualStudio.TextTemplating;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TemplatingEngine.Engines {

    public class T4Engine : IEngine {

        #region types

        public class TemplateHost : ITextTemplatingEngineHost, ITextTemplatingSessionHost {
            private List<CompilerError> compilerErrors_ = new List<CompilerError>();
            public IReadOnlyCollection<CompilerError> CompilerErrors { get { return this.compilerErrors_; } }

            public IDictionary<string, string> Parameters { get; private set; } = new Dictionary<string, string>();

            public string FileExtension { get; private set; }

            public Encoding OutputEncoding { get; private set; }

            #region events

            public delegate bool LoadIncludeTextDelegate(string requestFileName, out string content, out string location);

            public event LoadIncludeTextDelegate LoadIncludeTextCalled;

            #endregion events

            #region constructors and destructor

            public TemplateHost(string filename) {
                this.TemplateFile = filename;
            }

            #endregion constructors and destructor

            #region implement ITextTemplatingEngineHost

            public IList<string> StandardAssemblyReferences { get; private set; } = new List<string>();

            public IList<string> StandardImports { get; private set; } = new List<string>();

            public string TemplateFile { get; private set; }

            public object GetHostOption(string optionName) {
                return null;
            }

            public bool LoadIncludeText(string requestFileName, out string content, out string location) {
                content = null;
                location = null;
                var ret = this.LoadIncludeTextCalled?.Invoke(requestFileName, out content, out location);
                if (ret is null)
                    throw new Exception($"{nameof(this.LoadIncludeTextCalled)} was not handled. Please subscribe to the event.");
                return ret.Value;
            }

            public void LogErrors(CompilerErrorCollection errors) {
                foreach (CompilerError error in errors)
                    this.compilerErrors_.Add(error);
            }

            public void ClearErrors() {
                this.compilerErrors_.Clear();
            }

            public AppDomain ProvideTemplatingAppDomain(string content) {
                return AppDomain.CurrentDomain;
            }

            private static Dictionary<string, string> cache_ = new Dictionary<string, string>();
            private static List<string> recentFolders_ = new List<string>();

            public string ResolveAssemblyReference(string assemblyReference) {
                if (File.Exists(assemblyReference))
                    return assemblyReference;

                var name = Path.GetFileNameWithoutExtension(assemblyReference);
                var ext = Path.GetExtension(assemblyReference);

                if (name.EndsWith(".resources"))
                    return null;
                if (cache_.TryGetValue(name, out string asmPath))
                    return asmPath;

                foreach (var folder in recentFolders_) {
                    var assemblyResolveFileInfo = new FileInfo(Path.Combine(folder, name + ext));
                    if (assemblyResolveFileInfo.Exists) {
                        asmPath = assemblyResolveFileInfo.FullName;
                        cache_[name] = asmPath;
                        return asmPath;
                    }
                }

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    var asmFileInfo = new FileInfo(asm.Location);
                    if (asmFileInfo.Directory.Exists) {
                        var assemblyResolveFileInfo = new FileInfo(Path.Combine(asmFileInfo.Directory.FullName, name + ext));
                        if (assemblyResolveFileInfo.Exists) {
                            asmPath = assemblyResolveFileInfo.FullName;
                            cache_[name] = asmPath;
                            if (!recentFolders_.Contains(asmFileInfo.Directory.FullName))
                                recentFolders_.Add(asmFileInfo.Directory.FullName);
                            return asmPath;
                        }
                    }
                }

                return null;
            }

            public Type ResolveDirectiveProcessor(string processorName) {
                return null;
            }

            public string ResolveParameterValue(string directiveId, string processorName, string parameterName) {
                if (this.Parameters.TryGetValue(parameterName, out string value))
                    return value;
                throw new Exception($"Parameter not found: {parameterName}");
            }

            public string ResolvePath(string path) {
                if (Directory.Exists(path) || File.Exists(path))
                    return path;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    var asmFileInfo = new FileInfo(asm.Location);
                    if (asmFileInfo.Directory.Exists) {
                        var assemblyResolveFileInfo = new FileInfo(Path.Combine(asmFileInfo.Directory.FullName, path));
                        if (assemblyResolveFileInfo.Exists) {
                            return assemblyResolveFileInfo.FullName;
                        }
                    }
                }

                return null;
            }

            public void SetFileExtension(string extension) {
                this.FileExtension = extension;
            }

            public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective) {
                this.OutputEncoding = encoding;
            }

            #endregion implement ITextTemplatingEngineHost

            #region implement ITextTemplatingSessionHost

            public ITextTemplatingSession Session { get; set; } = new TextTemplatingSession();

            public ITextTemplatingSession CreateSession() {
                return this.Session;
            }

            #endregion implement ITextTemplatingSessionHost
        }

        #endregion types

        #region implement IEngine

        public string Generate<T>(string template, T context)
            where T : class {
            return GenerateT4(template, GetParametersFromContext(context));
        }

        public void Generate<T>(Stream template, T context, Stream output)
            where T : class {
            GenerateT4(template, GetParametersFromContext(context), output);
        }

        public async Task<string> GenerateAsync<T>(string template, T context)
            where T : class {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.Generate(template, context);
            });
            return output;
        }

        public async void GenerateAsync<T>(Stream template, T context, Stream output)
            where T : class {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.Generate(template, context, output);
            });
        }

        public string GenerateDynamic(string template, dynamic context) {
            return GenerateT4(template, GetParametersFromContextDynamic(context));
        }

        public void GenerateDynamic(Stream template, dynamic context, Stream output) {
            GenerateT4(template, GetParametersFromContextDynamic(context), output);
        }

        public async Task<string> GenerateDynamicAsync(string template, dynamic context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.GenerateDynamic(template, context);
            });
            return output;
        }

        public async void GenerateDynamicAsync(Stream template, dynamic context, Stream output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.GenerateDynamic(template, context, output);
            });
        }

        #endregion implement IEngine

        #region helper

        private static string GenerateT4(string template, IDictionary<string, string> parameters) {
            var engine = new Engine();
            var host = new TemplateHost(Guid.NewGuid().ToString());
            foreach (var param in parameters) {
                host.Parameters[param.Key] = param.Value;
            }
            var result = engine.ProcessTemplate(template, host);
            if (host.CompilerErrors.Any(x => !x.IsWarning)) {
                throw new Exception($"Errors occured during template generation: {host.CompilerErrors.Select(x => CompilerErrorToMessage(x)).Implode(Environment.NewLine, Environment.NewLine)}");
            }
            return result;
        }

        private void GenerateT4(Stream template, IDictionary<string, string> parameters, Stream output) {
            string input = null;
            using (var stream = new StreamReader(template, Encoding.UTF8, true, 1024, leaveOpen: true))
                input = stream.ReadToEnd();
            var text = GenerateT4(input, parameters);
            using (var stream = new StreamWriter(output, Encoding.UTF8, 1024, leaveOpen: true))
                stream.Write(text);
        }

        private static string CompilerErrorToMessage(CompilerError error) {
            return $"{error.ErrorText} (line {error.Line}, column {error.Column}, file {error.FileName})";
        }

        private static IDictionary<string, string> GetParametersFromContext<T>(T context)
            where T : class {
            if (context is null)
                return new Dictionary<string, string>();
            var type = typeof(T);
            return GetParametersFromContext(context, type);
        }

        private static IDictionary<string, string> GetParametersFromContextDynamic(dynamic context) {
            if (context is null)
                return new Dictionary<string, string>();
            var type = context.GetType();
            return GetParametersFromContext(context, type);
        }

        private static IDictionary<string, string> GetParametersFromContext(object context, Type type) {
            if (context is null)
                return new Dictionary<string, string>();
            var flags = BindingFlags.Public | BindingFlags.Instance;

            return type.GetProperties(flags)
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(context)?.ToString());
        }

        #endregion helper
    }
}