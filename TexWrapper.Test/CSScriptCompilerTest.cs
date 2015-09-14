using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace TexWrapper.Test
{
    [TestClass]
    public class CSScriptCompilerTest
    {
        public class MyObject
        {
            public bool x { get; set; }
            public bool y { get; set; }
            public bool z { get; set; }
            public string Value { get { return "Value123"; } }
            public string Value2 { get { return "Value234"; } }
        }
        [TestMethod]
        public void CSScriptCompilerTestTex()
        {
            string result;
            var sw = new Stopwatch();
            var o = new MyObject();
            var cmplr = new CSScriptCompiler();
            sw.Start();
            var script = cmplr.Compile(@"{##
                print o.x;
		        ##}");
            sw.Stop();
            Trace.WriteLine("Compile T: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
            Trace.WriteLine(result = script.Run(o));
            sw.Stop();
            Trace.WriteLine("Run T: " + sw.ElapsedMilliseconds + "ms");
            Assert.AreEqual(o.x.ToString(), result);

            sw.Restart();
            cmplr.AddEscapeSequenze("%##", "##%");
            script = cmplr.Compile(@"%####%
%##  ##%
%####%
\documentclass{article}

\usepackage{bchart}

\begin{document}
    \begin{bchart}[step=2,max=10]
		{####}
		{##
		##}
		{## print o.Value; ##}
		{## print o.Value2; ##}
		{## if (o.x) { ##}
			\bcbar{3.4}
				\smallskip
		{## } else if (o.y) { ##}
			\bcbar{5.6}
				\medskip
		{## } else if (o.z) { ##}
			\bcbar{7.2}
				\bigskip
		{## } else { ##}
			\bcbar{9.9}
		{## } ##}
    \end{bchart}
\end{document}");
            sw.Stop();
            Trace.WriteLine("Compile T: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();
            Trace.WriteLine(result = script.Run(o));
            sw.Stop();
            Trace.WriteLine("Run T: " + sw.ElapsedMilliseconds + "ms");
            Assert.AreEqual(@"


\documentclass{article}

\usepackage{bchart}

\begin{document}
    \begin{bchart}[step=2,max=10]
		
		
		" + o.Value.ToString() + @"
		" + o.Value2.ToString() + @"
		
			\bcbar{9.9}
		
    \end{bchart}
\end{document}", result);
        }
    }
}
