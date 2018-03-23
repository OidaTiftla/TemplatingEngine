using Microsoft.CSharp;
using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionMethods;

namespace LatexScriptWrapper
{
    public interface IScript
    {
        /// <summary>
        /// Run script with parameter o
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        string Run(dynamic o);
    }

    public interface ICompiler
    {
        IScript Compile(string code);
    }

    public interface IContentsScript : IScript
    {
        IList<string> Contents { get; }
    }

    public class CSScriptCompiler : ICompiler
    {
        public interface IEscapeSequenze
        {
            string BeginRegex { get; }
            string EndRegex { get; }
        }

        public class EscapeSequenze : IEscapeSequenze
        {
            public string Begin { get; set; }
            public string End { get; set; }
            public string BeginRegex { get { return Regex.Escape(this.Begin); } }
            public string EndRegex { get { return Regex.Escape(this.End); } }
            public EscapeSequenze(string begin, string end)
            {
                this.Begin = begin;
                this.End = end;
            }
        }

        //public class EscapeSequenzeRegex : IEscapeSequenze
        //{
        //    public string BeginRegex { get; set; }
        //    public string EndRegex { get; set; }
        //    public EscapeSequenzeRegex(string begin, string end)
        //    {
        //        this.BeginRegex = begin;
        //        this.EndRegex = end;
        //    }
        //}

        private List<IEscapeSequenze> escape_sequenzes_;
        private List<string> usings_;
        public ICollection<IEscapeSequenze> EscapeSequenzes { get { return this.escape_sequenzes_; } }
        public IEnumerable<string> Usings { get { return this.usings_; } }
        public string FunctionCode { get; set; }

        public CSScriptCompiler()
        {
            this.escape_sequenzes_ = new List<IEscapeSequenze>();
            this.usings_ = new List<string>();
            this.AddEscapeSequenze(@"{##", @"##}");
            this.AddUsing("Microsoft.CSharp");
            this.AddUsing("Microsoft.CSharp.RuntimeBinder");
            this.AddUsing("System");
            this.AddUsing("System.CodeDom.Compiler");
            this.AddUsing("System.Collections.Generic");
            this.AddUsing("System.Linq");
            this.AddUsing("System.Reflection");
            this.AddUsing("System.Text");
            this.AddUsing("System.Text.RegularExpressions");
            this.AddUsing("ExtensionMethods");
        }

        public void AddEscapeSequenze(string begin, string end)
        {
            this.EscapeSequenzes.Add(new EscapeSequenze(begin, end));
        }

        //public void AddEscapeSequenzeRegex(string begin, string end)
        //{
        //    this.EscapeSequenzes.Add(new EscapeSequenzeRegex(begin, end));
        //}

        /// <summary>
        /// Add namespace for using directives
        /// </summary>
        /// <param name="ns">namespace</param>
        public void AddUsing(string ns)
        {
            if (!this.usings_.Contains(ns))
                this.usings_.Add(ns);
        }

        public IScript Compile(string source_script)
        {
            var contents = new List<string>();
            var cs = createCS(source_script, contents);
            var script = createScript(cs, this.FunctionCode);
            foreach (var c in contents)
                script.Contents.Add(c);
            return script;
        }

        private string createCS(string tex, List<string> contents)
        {
            var csPartsRegex = new Regex(this.EscapeSequenzes.Implode("|", "(", ")",
                x => @"(?<=" + x.BeginRegex + @").*?(?=" + x.EndRegex + @")"), RegexOptions.Singleline);
            var printRegex = new Regex(@"(?<=\s+)print\s+(?<retval>.*?)(?=;)", RegexOptions.Singleline);
            var contentPartsRegex = new Regex(@"(^|"
                + this.EscapeSequenzes.Implode(")|(", "(", ")", x => x.EndRegex)
                + @")(?<content>.*?)("
                + this.EscapeSequenzes.Implode(")|(", "(", ")", x => x.BeginRegex) + @"|$)", RegexOptions.Singleline);

            contents.Clear();
            tex = csPartsRegex.Replace(tex, m =>
                printRegex.Replace(m.ToString(), mm => "Print(" + mm.Groups["retval"].ToString() + ")"));
            return contentPartsRegex.Replace(tex, m =>
            {
                var content = m.Groups["content"].Value;
                contents.Add(content);
                return "\r\nPrint(this.Contents[" + (contents.Count - 1).ToString() + "]);\r\n";
            });
        }

        private bool containsFunction(dynamic script, string func)
        {
            var t = script.GetType() as Type;
            if (t.GetMethod(func) != null)
                return true;
            else if (t.GetMethod("GetDynamicMemberNames") != null)
                return System.Linq.Enumerable.Contains(script.GetDynamicMemberNames(), func);
            return false;
        }

        private IContentsScript createScript(string code, string func = null)
        {
            string classcode = this.Usings.Implode("\n", "", "", x => "using " + x + ";") + @"

                namespace UserFunctions
                {                
                    public class UserFunction : " + typeof(IContentsScript).FullName + @"
                    {
                        public IList<string> Contents { get; private set; }
                        public IList<string> Outputs { get; private set; }

                        public UserFunction()
                        {
                            this.Contents = new List<string>();
                            this.Outputs = new List<string>();
                        }

                        __func__

                        public string Run(dynamic o)
                        {
                            this.Outputs.Clear();
                            var sb = new StringBuilder();
                            this.run(" + typeof(AnonymousTypeHelper).FullName + @".ToExpandoObjectIfNecessary(o));
                            foreach (var s in this.Outputs)
                                sb.Append(s);
                            return sb.ToString();
                        }

                        public void Print(object o)
                        {
                            this.Outputs.Add(o.ToString());
                        }

                        private void run(dynamic o)
                        {
                            //System.Diagnostics.Debugger.Break();
                            __code__
                        }
                    }
                }
            ";

            string finalCode = classcode.Replace("__code__", code).Replace("__func__", func);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            //parameters.GenerateInMemory = true;
            //parameters.GenerateExecutable = false;
            parameters.IncludeDebugInformation = true;
            parameters.TempFiles = new TempFileCollection(Environment.GetEnvironmentVariable("TEMP"), true);

            //Add the required assemblies
            AddIfNotExists(parameters, typeof(IContentsScript).Assembly.Location);
            AddIfNotExists(parameters, typeof(AnonymousTypeHelper).Assembly.Location);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.FullName.Contains("Anonymously Hosted DynamicMethods Assembly"))
                        continue;
                    //if (asm.Location.Contains("Microsoft.Xna") || asm.Location.Contains("Gibbo.Library")
                    //    || asm.Location.Contains("System"))
                    AddIfNotExists(parameters, asm.Location);
                }
                catch { }
            }
            var add = true;
            foreach (var asm in parameters.ReferencedAssemblies)
                if (asm.Contains("Microsoft.CSharp"))
                {
                    add = false;
                    break;
                }
            if (add)
                parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, finalCode);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    if (error.IsWarning)
                        sb.AppendLine(String.Format("Warning ({0}) Zeile: {1} Spalte: {2}\n{3}", error.ErrorNumber, error.Line, error.Column, error.ErrorText));
                    else
                        sb.AppendLine(String.Format("Error ({0}) Zeile: {1} Spalte: {2}\n{3}", error.ErrorNumber, error.Line, error.Column, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }

            Type binaryFunction = results.CompiledAssembly.GetType("UserFunctions.UserFunction");
            return binaryFunction.GetConstructor(new Type[0]).Invoke(new object[0]) as IContentsScript;
        }

        private static void AddIfNotExists(CompilerParameters parameters, string location)
        {
            if (!parameters.ReferencedAssemblies.Contains(location))
                parameters.ReferencedAssemblies.Add(location);

        }
    }
}
