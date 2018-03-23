using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TemplatingEngine.Engines {

    public class LatexEngine : CSharpEngine, IEngine {

        #region types

        public class TexConfig {

            #region properties

            public string Command { get; set; }
            public List<string> Arguments { get; private set; }

            #endregion properties

            #region constructors and destructor

            public TexConfig() {
                this.Arguments = new List<string>();
            }

            public TexConfig(FileInfo file) : this() {
                this.Load(file);
            }

            #endregion constructors and destructor

            #region load

            public bool Load(FileInfo file) {
                this.Command = null;
                this.Arguments.Clear();
                try {
                    XmlDocument document = new XmlDocument();
                    document.Load(file.FullName);
                    XmlElement rootElement = document.DocumentElement;
                    XmlNodeList nodes = rootElement.ChildNodes;
                    foreach (XmlNode node in nodes) {
                        if (node.Name.Equals("command"))
                            this.Command = node.InnerText;
                        else if (node.Name.Equals("arg")
                            && node.Attributes.Count == 1
                            && node.Attributes.Item(0).Name.Equals("value"))
                            this.Arguments.Add(node.Attributes.Item(0).Value);
                    }
                    return true;
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }

            #endregion load

            #region create process

            public Process CreateProcess(FileInfo texFile) {
                Process process = new Process();
                process.StartInfo.FileName = this.Command;
                process.StartInfo.Arguments = this.Arguments.Concat(new string[] { "\"" + texFile.FullName + "\"" }).Implode(" ");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                return process;
            }

            #endregion create process

            #region ToString

            public override string ToString() {
                return this.Command + " " + this.Arguments.Implode(" ");
            }

            #endregion ToString
        }

        public class TexWrapper {

            #region properties

            public TexConfig Configuration { get; set; }

            #endregion properties

            #region constructors and destructor

            public TexWrapper() {
                this.Configuration = new TexConfig();
                var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var config = new FileInfo(Path.Combine(exeDir, "Configuration.xml"));
                if (config.Exists)
                    this.Configuration.Load(config);
                else {
                    config = new FileInfo(Path.Combine(exeDir, "Config", "Configuration.xml"));
                    if (config.Exists)
                        this.Configuration.Load(config);
                }
            }

            public TexWrapper(FileInfo config) : this() {
                this.Configuration.Load(config);
            }

            public TexWrapper(TexConfig config) : this() {
                this.Configuration = config;
            }

            #endregion constructors and destructor

            #region compile

            public void Compile(string tex, FileInfo destPdfFile) {
                try {
                    var tmpDir = IOExtension.CreateTempDirectory();
                    var texTmpFile = new FileInfo(Path.Combine(tmpDir.FullName, "temp.tex"));
                    using (var writer = new StreamWriter(texTmpFile.FullName)) {
                        writer.Write(tex);
                    }
                    this.compile(texTmpFile, destPdfFile);
                    tmpDir.Delete(true);
                } catch (Exception ex) {
                    if (ex is TexCompilationException)
                        throw;
                    Debug.WriteLine(ex.Message);
                    throw new TexCompilationException("An Error occured while processing" + Environment.NewLine
                        + "see inner exception for details", ex);
                }
            }

            public void Compile(FileInfo texFile, FileInfo destPdfFile) {
                this.compile(texFile, destPdfFile);
            }

            public void CompileInTemporaryDirectory(FileInfo texFile, FileInfo destPdfFile) {
                try {
                    var tmpDir = IOExtension.CreateTempDirectory();
                    var texTmpFile = new FileInfo(Path.Combine(tmpDir.FullName, texFile.Name));
                    texFile.CopyTo(texTmpFile.FullName);
                    this.compile(texTmpFile, destPdfFile);
                    tmpDir.Delete(true);
                } catch (Exception ex) {
                    if (ex is TexCompilationException)
                        throw;
                    Debug.WriteLine(ex.Message);
                    throw new TexCompilationException("An Error occured while processing file " + texFile.FullName + Environment.NewLine
                        + "see inner exception for details", ex);
                }
            }

            private void compile(FileInfo texFile, FileInfo destPdfFile) {
                string cmdLine = null, workingDir = null;
                var stdOutput = new StringBuilder();
                var pdfFile = new FileInfo(Path.Combine(texFile.Directory.FullName, texFile.NameWithoutExtension() + ".pdf"));
                var logFile = new FileInfo(Path.Combine(texFile.Directory.FullName, texFile.NameWithoutExtension() + ".log"));
                try {
                    if (File.Exists(pdfFile.FullName))
                        pdfFile.Delete();
                    var process = this.Configuration.CreateProcess(texFile);
                    workingDir = process.StartInfo.WorkingDirectory = texFile.Directory.FullName;
                    cmdLine = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
                    // set up startinfo to redirect outputs
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.EnableRaisingEvents = true;
                    process.OutputDataReceived += (sender, e) => {
                        lock (stdOutput) {
                            stdOutput.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) => {
                        lock (stdOutput) {
                            stdOutput.AppendLine("STDERR: " + e.Data);
                        }
                    };
                    // start it
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    process.CancelOutputRead();
                    process.CancelErrorRead();
                    // verify the result
                    if (process.ExitCode == 0 || File.Exists(pdfFile.FullName)) {
                        if (process.ExitCode == 0)
                            Debug.WriteLine("Tex: Command was successfully executed.");
                        else
                            Debug.WriteLine("Tex: Command exited with error: " + process.ExitCode);
                        //var pdfFile = texFile.Directory.EnumerateFiles(texFile.NameWithoutExtension() + ".pdf").FirstOrDefault();
                        if (!File.Exists(pdfFile.FullName))
                            throw new TexCompilationException("Tex-Command was successfully executed but there is no output-PDF file found");
                        if (destPdfFile != null && destPdfFile.FullName != pdfFile.FullName) {
                            if (File.Exists(destPdfFile.FullName))
                                destPdfFile.Delete();
                            pdfFile.MoveTo(destPdfFile.FullName);
                        }
                    } else {
                        // some thing went wrong ...

                        // get logging
                        var log = "";
                        log += "Standard output:\n" + stdOutput.ToString();
                        log += "\n\n\n\n";

                        if (File.Exists(logFile.FullName)) {
                            using (var reader = new StreamReader(logFile.FullName))
                                log += reader.ReadToEnd();
                        }

                        Debug.WriteLine(
                            "'" + cmdLine + "' with working directory '"
                            + workingDir + "' exited with error: "
                            + process.ExitCode + Environment.NewLine + log);
                        throw new TexCompilationException(
                            "'" + cmdLine + "' with working directory '"
                            + workingDir + "' exited with error: "
                            + process.ExitCode + Environment.NewLine + log);
                    }
                } catch (Exception ex) {
                    if (ex is TexCompilationException)
                        throw;
                    Debug.WriteLine("An Error occured while processing file " + texFile.FullName + Environment.NewLine
                        + "'" + cmdLine + "' with working directory '"
                        + workingDir + "'");
                    Debug.WriteLine(ex.Message);
                    throw new TexCompilationException("An Error occured while processing file " + texFile.FullName + Environment.NewLine
                        + "'" + cmdLine + "' with working directory '"
                        + workingDir + "'" + Environment.NewLine + "see inner exception for details", ex);
                }
            }

            #endregion compile

            #region check config

            public bool CheckConfiguration() {
                try {
                    this.Compile(@"\documentclass{article}
                \begin{document}
                Hello World!
                \end{document}", null);
                    return true;
                } catch { return false; }
            }

            #endregion check config
        }

        public class TexCompilationException : Exception {

            public TexCompilationException() : base() {
            }

            public TexCompilationException(string message) : base(message) {
            }

            public TexCompilationException(string message, Exception innerException) : base(message, innerException) {
            }
        }

        #endregion types

        #region static

        private static string texEscapeFunc = @"
        private class Tex {
            public static string Escape(object o) {
                return Escape(o.ToString());
            }

            public static string Escape(string text) {
                if (text == null)
                    return """";

                var sb = new StringBuilder();
                foreach (var c in text) {
                    switch (c) {
                        case '\\': sb.Append(@""{\texttt{\char`\\}""); break;
                        //                        case '\\': sb.Append(@""{\textbackslash}""); break;
                        case '{': sb.Append(@""\{""); break;
                        case '}': sb.Append(@""\}""); break;
                        case '<': sb.Append(@""{\textless}""); break;
                        case '>': sb.Append(@""{\textgreater}""); break;
                        case '~': sb.Append(@""{\textasciitilde}""); break;
                        case '€': sb.Append(@""{\texteuro}""); break;
                        case '_': sb.Append(@""\_""); break;
                        case '^': sb.Append(@""\hat{\text{\ }}""); break;
                        case '§': sb.Append(@""\S""); break;
                        case '$': sb.Append(@""\$""); break;
                        case '&': sb.Append(@""\&""); break;
                        case '#': sb.Append(@""\#""); break;
                        case '%': sb.Append(@""\%""); break;
                        case ' ': sb.Append(@""\ ""); break;
                        case '""': sb.Append(@""""""'""); break;
                        case ',': sb.Append(@""{,}""); break;
                        default: sb.Append(c); break;
                    }
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                        sb.Append(c);
                    else
                        sb.Append(@""\char"""""" + ((int)c).ToString(""X"") + @"" "");
                }
                return sb.ToString().Replace(""\n"", @""\\"");
            }
        }";

        #endregion static

        #region properties

        private TexWrapper texWrapper_ = null;

        #endregion properties

        #region constructors and destructor

        public LatexEngine() : this(null) {
        }

        public LatexEngine(FileInfo configFile) {
            if (configFile == null)
                this.texWrapper_ = new TexWrapper();
            else
                this.texWrapper_ = new TexWrapper(configFile);
            this.AddEscapeSequenze(@"%##", @"##%");
            this.AddEscapeSequenze(@"\verb|##", @"##|");
            this.AddEscapeSequenze(@"\verb$##", @"##$");
            // Regex does not work pretty good if you want to use groups in Regex. At the moment its not necessary
            //this.AddEscapeSequenzeRegex(@"\\verb.\#\#", @"\#\#.");
            //this.AddEscapeSequenzeRegex(@"\\verb(?<k84>.)\#\#", @"\#\#\k<k84>");
            this.AddEscapeSequenze(@"\begin{comment}##", @"##\end{comment}");
            this.FunctionCode = texEscapeFunc;
        }

        #endregion constructors and destructor

        #region public interface

        public void CreatePdf(string template, dynamic context, FileInfo destination) {
            // run in temporary directory
            var tempDir = IOExtension.CreateTempDirectory();
            try {
                var texFile = new FileInfo(Path.Combine(tempDir.FullName, "template.tex"));
                using (var writer = new StreamWriter(texFile.FullName))
                    writer.Write(template);
                this.CreatePdf(texFile, context, destination);
            } finally {
                // cleanup temporary directory
                Directory.Delete(tempDir.FullName, true);
            }
        }

        public void CreatePdf(FileInfo source, dynamic context, FileInfo destination) {
            // compile into script
            IScript script;
            using (var reader = new StreamReader(source.FullName))
                script = this.Compile(reader.ReadToEnd());

            // create new tex-file
            var texcs = new FileInfo(source.FullName + "cs");
            using (var writer = new StreamWriter(texcs.FullName))
                writer.Write(script.Run(context));

            // compile into PDF
            this.texWrapper_.Compile(texcs, destination);
            this.texWrapper_.Compile(texcs, destination); // BugFix: Always compile twice
        }

        #endregion public interface

        #region implement IEngine

        public override string Generate<T>(string template, T context) {
            return this.GenerateDynamic(template, context);
        }

        public override void Generate<T>(Stream template, T context, Stream output) {
            this.GenerateDynamic(template, context, output);
        }

        public override async Task<string> GenerateAsync<T>(string template, T context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.Generate(template, context);
            });
            return output;
        }

        public override async void GenerateAsync<T>(Stream template, T context, Stream output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.Generate(template, context, output);
            });
        }

        public override string GenerateDynamic(string template, dynamic context) {
            throw new Exception("Cannot generate PDF into string. Please use the function overloads with StreamWriter.");
        }

        public override void GenerateDynamic(Stream template, dynamic context, Stream output) {
            // run in temporary directory
            var tempDir = IOExtension.CreateTempDirectory();
            try {
                var pdfFile = new FileInfo(Path.Combine(tempDir.FullName, "output.pdf"));
                using (var stream = new StreamReader(template))
                    this.CreatePdf(stream.ReadToEnd(), context, pdfFile);
                using (var stream = File.OpenRead(pdfFile.FullName))
                    stream.WriteTo(output);
            } finally {
                // cleanup temporary directory
                Directory.Delete(tempDir.FullName, true);
            }
        }

        public override async Task<string> GenerateDynamicAsync(string template, dynamic context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.GenerateDynamic(template, context);
            });
            return output;
        }

        public override async void GenerateDynamicAsync(Stream template, dynamic context, Stream output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.GenerateDynamic(template, context, output);
            });
        }

        #endregion implement IEngine
    }
}