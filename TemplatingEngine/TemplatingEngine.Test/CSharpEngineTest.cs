using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using TemplatingEngine.Engines;

namespace TemplatingEngine.Test {

    [TestClass]
    public class CSharpEngineTest {

        public class MyObject {
            public bool x { get; set; }
            public bool y { get; set; }
            public bool z { get; set; }
            public string Value { get { return "Value123"; } }
            public string Value2 { get { return "Value234"; } }
        }

        [TestMethod]
        public void CSharpEngineTestTex() {
            string result;
            var o = new MyObject();
            var engine = new CSharpEngine();
            result = engine.Generate(@"{##
                print o.x;
    ##}", o);
            Trace.WriteLine(result);
            Assert.AreEqual(o.x.ToString(), result);

            engine.AddEscapeSequenze("%##", "##%");
            result = engine.Generate(@"%####%
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
\end{document}", o);
            Trace.WriteLine(result);
            Assert.AreEqual(@"
" + @"
" + @"
\documentclass{article}
" + @"
\usepackage{bchart}
" + @"
\begin{document}
    \begin{bchart}[step=2,max=10]
        " + @"
        " + @"
        " + o.Value.ToString() + @"
        " + o.Value2.ToString() + @"
        " + @"
            \bcbar{9.9}
        " + @"
    \end{bchart}
\end{document}", result);
        }
    }
}