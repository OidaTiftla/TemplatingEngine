using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatexScriptWrapper.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the RepotGenerator
            // Here you can pass a Configuration, otherwise
            // CSTexReportGenerator searches Configuration.xml
            // in the current directory and if it is not there
            // he searches it in ./Config/
            var generator = new CSTexReportGenerator();

            // Create an object, that you can use in your LaTeX
            var o = new
            {
                Value = 25,
                x = false,
                y = true,
                z = false,
                Headers = new string[] { "First", "Second", "Third", "Fourth" },
                Entries = new[] {
                    new[] { "11", "12", "13", "14" },
                    new[] { "21", "22", "23", "24" },
                    new[] { "31", "32", "33", "34" },
                    new[] { "41", "42", "43", "44" },
                    new[] { "51", "52", "53", "54" },
                    new[] { "61", "62", "63", "64" },
                },
            };

            // Create the PDF
            var src = new FileInfo("TestFile.tex");
            var dest = new FileInfo("TestFile.pdf");
            generator.Create(src, o, dest);

            // Show the PDF
            Process.Start(dest.FullName);
        }
    }
}
