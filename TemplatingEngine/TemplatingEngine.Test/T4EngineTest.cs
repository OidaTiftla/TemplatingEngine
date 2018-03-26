using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using TemplatingEngine.Engines;

namespace TemplatingEngine.Test {

    [TestClass]
    public class T4EngineTest {

        public class MyObject {
            public bool x { get; set; }
            public bool y { get; set; }
            public bool z { get; set; }
            public string Value { get { return "Value123"; } }
            public string Value2 { get { return "Value234"; } }
        }

        [TestMethod]
        public void T4EngineTestSimple() {
            string result;
            var o = new MyObject();
            var engine = new T4Engine();
            result = engine.Generate(@"<#@ template language=""C#"" debug=""true"" #>

<#@ output extension="".html"" #>
<#@ parameter name=""parameter1"" type=""System.String"" #>

<p>Hello world this is <#= this.x #></p>", o);
            Trace.WriteLine(result);
            Assert.AreEqual(@"<p>Hello world this is false</p>", result.Trim());
        }
    }
}