using System.IO;
using System.Text;

namespace miktexdummy {

    public class Program {

        private static void Main(string[] args) {
            File.WriteAllText(Path.ChangeExtension(args[0], ".pdf"), @"Ich bin ein PDF :D
" + File.ReadAllText(args[0]), Encoding.UTF8);
            File.WriteAllText(Path.ChangeExtension(args[0], ".log"), "Ich bin ein log :D", Encoding.UTF8);
        }
    }
}