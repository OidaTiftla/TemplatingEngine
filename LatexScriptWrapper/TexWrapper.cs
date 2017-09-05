using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace LatexScriptWrapper {
    public class TexConfig {
        #region properties
        public string Command { get; set; }
        public List<string> Arguments { get; private set; }
        #endregion

        #region constructor
        public TexConfig() { this.Arguments = new List<string>(); }
        public TexConfig(FileInfo file) : this() { this.Load(file); }
        #endregion

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
        #endregion

        #region create process
        public Process CreateProcess(FileInfo texFile) {
            Process process = new Process();
            process.StartInfo.FileName = this.Command;
            process.StartInfo.Arguments = this.Arguments.Concat(new string[] { "\"" + texFile.FullName + "\"" }).Implode(" ");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }
        #endregion

        #region ToString
        public override string ToString() {
            return this.Command + " " + this.Arguments.Implode(" ");
        }
        #endregion
    }

    public class TexWrapper {
        #region properties
        public TexConfig Configuration { get; set; }
        #endregion

        #region constructor
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
        public TexWrapper(FileInfo config) : this() { this.Configuration.Load(config); }
        public TexWrapper(TexConfig config) : this() { this.Configuration = config; }
        #endregion

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
        #endregion

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
        #endregion
    }

    public class TexCompilationException : Exception {
        public TexCompilationException() : base() { }
        public TexCompilationException(string message) : base(message) { }
        public TexCompilationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
