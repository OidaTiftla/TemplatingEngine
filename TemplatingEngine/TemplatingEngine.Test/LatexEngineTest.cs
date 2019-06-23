using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using TemplatingEngine.Engines;

namespace TemplatingEngine.Test {

    [TestClass]
    public class LatexEngineTest {

        public class MyObject {
            public bool x { get; set; }
            public bool y { get; set; }
            public bool z { get; set; }
            public string Value { get { return "Value123"; } }
            public string Value2 { get { return "Value234"; } }
        }

        [TestMethod]
        public void LatexEngineTestTex() {
            string result;
            var o = new MyObject();

            // locate miktexdummy.exe
            var miktexdummyFile = new FileInfo(typeof(miktexdummy.Program).Assembly.Location);
            if (!miktexdummyFile.Exists)
                throw new System.Exception("Cannot find 'miktexdummy.exe'");

            // setup engine to use miktexdummy.exe
            var engine = new LatexEngine(new LatexEngine.TexConfig() {
                Command = miktexdummyFile.FullName,
            });

            var input = @"%####%
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
\end{document}";

            // testing
            using (var mem = new MemoryStream()) {
                using (var writer = new StreamWriter(mem, Encoding.UTF8, 1024, leaveOpen: true))
                    writer.Write(input);

                // reset position to start
                mem.Position = 0;

                // generate
                using (var memOut = new MemoryStream()) {
                    engine.Generate(mem, o, memOut);

                    // reset position to start
                    memOut.Position = 0;

                    // check output
                    string output = null;
                    using (var reader = new StreamReader(memOut, Encoding.UTF8, true, 1024, leaveOpen: true))
                        output = reader.ReadToEnd();
                    Assert.AreEqual(@"Ich bin ein PDF :D
" + @"
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
\end{document}", output);
                }
            }
        }
    }
}