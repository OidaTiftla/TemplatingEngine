using System;
using System.Diagnostics;
using System.IO;

namespace LatexScriptWrapper {
    public interface IReportGenerator {
        void Create(dynamic o, FileInfo destination);
        void Create(FileInfo source, dynamic o, FileInfo destination);
    }

    public class CSTexReportGenerator : IReportGenerator {
        private static string texEscapeFunc = @"
            class Tex
            {
                public static string Escape(object o) { return Escape(o.ToString()); }
                public static string Escape(string text)
                {
                    if (text == null)
                        return """";
                    
                    var sb = new StringBuilder();
                    foreach (var c in text)
                    {
                        switch(c)
                        {
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
//                        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
//                            sb.Append(c);
//                        else
//                            sb.Append(@""\char"""""" + ((int)c).ToString(""X"") + @"" "");
                    }
                    return sb.ToString().Replace(""\n"",@""\\"");
                }
            }";
        private IScript script_ = null;
        private FileInfo source_ = null;
        private CSScriptCompiler compiler_ = new CSScriptCompiler();
        public CSScriptCompiler Compiler { get { return this.compiler_; } }
        public FileInfo Source { get { return this.source_; } set { this.source_ = value; this.script_ = null; } }
        public TexWrapper TexWrapper { get; private set; }

        public CSTexReportGenerator() : this(null) { }
        public CSTexReportGenerator(FileInfo configFile) {
            if (configFile == null)
                this.TexWrapper = new TexWrapper();
            else
                this.TexWrapper = new TexWrapper(configFile);
            this.compiler_.AddEscapeSequenze(@"%##", @"##%");
            this.compiler_.AddEscapeSequenze(@"\verb|##", @"##|");
            this.compiler_.AddEscapeSequenze(@"\verb$##", @"##$");
            // Regex does not work pretty good if you want to use groups in Regex. At the moment its not necessary
            //this.compiler_.AddEscapeSequenzeRegex(@"\\verb.\#\#", @"\#\#.");
            //this.compiler_.AddEscapeSequenzeRegex(@"\\verb(?<k84>.)\#\#", @"\#\#\k<k84>");
            this.compiler_.AddEscapeSequenze(@"\begin{comment}##", @"##\end{comment}");
            this.compiler_.FunctionCode = texEscapeFunc;
        }

        public void Create(dynamic o, FileInfo destination) {
            if (this.Source == null)
                throw new InvalidOperationException("You must first specify a source.");
            if (this.script_ == null)
                using (var reader = new StreamReader(this.source_.FullName))
                    this.script_ = this.compiler_.Compile(reader.ReadToEnd());
            var texcs = new FileInfo(this.source_.FullName + "cs");
            using (var writer = new StreamWriter(texcs.FullName))
                writer.Write(this.script_.Run(o));
            this.TexWrapper.Compile(texcs, destination);
            this.TexWrapper.Compile(texcs, destination); // BugFix: Always compile twice
        }

        public void Create(FileInfo source, dynamic o, FileInfo destination) {
            IScript script;
#if DEBUG
            var swTotal = new Stopwatch();
            var sw = new Stopwatch();
            swTotal.Start();
            sw.Start();
#endif
            using (var reader = new StreamReader(source.FullName))
                script = this.compiler_.Compile(reader.ReadToEnd());
#if DEBUG
            sw.Stop();
            Trace.WriteLine("Compile Script T: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
#endif
            var texcs = new FileInfo(source.FullName + "cs");
            using (var writer = new StreamWriter(texcs.FullName))
                writer.Write(script.Run(o));
#if DEBUG
            sw.Stop();
            Trace.WriteLine("Run Script T: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
#endif
            this.TexWrapper.Compile(texcs, destination);
#if DEBUG
            sw.Stop();
            Trace.WriteLine("Compile Tex 1 T: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
#endif
            this.TexWrapper.Compile(texcs, destination); // BugFix: Always compile twice
#if DEBUG
            sw.Stop();
            swTotal.Stop();
            Trace.WriteLine("Compile Tex 2 T: " + sw.ElapsedMilliseconds + "ms");
            Trace.WriteLine("Total T: " + swTotal.ElapsedMilliseconds + "ms");
#endif
        }
    }
}
