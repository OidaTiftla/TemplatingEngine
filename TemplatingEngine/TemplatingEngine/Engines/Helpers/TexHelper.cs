using System.Text;

namespace TemplatingEngine.Engines.Helpers {

    public class Tex {

        public static string Escape(object o) {
            return Escape(o.ToString());
        }

        public static string Escape(string text) {
            if (text == null)
                return "";

            var sb = new StringBuilder();
            foreach (var c in text) {
                switch (c) {
                    case '\\': sb.Append(@"{\texttt{\char`\\}"); break;
                    //case '\\': sb.Append(@"{\textbackslash}"); break;
                    case '{': sb.Append(@"\{"); break;
                    case '}': sb.Append(@"\}"); break;
                    case '<': sb.Append(@"{\textless}"); break;
                    case '>': sb.Append(@"{\textgreater}"); break;
                    case '~': sb.Append(@"{\textasciitilde}"); break;
                    case '€': sb.Append(@"{\texteuro}"); break;
                    case '_': sb.Append(@"\_"); break;
                    case '^': sb.Append(@"\hat{\text{\ }}"); break;
                    case '§': sb.Append(@"\S"); break;
                    case '$': sb.Append(@"\$"); break;
                    case '&': sb.Append(@"\&"); break;
                    case '#': sb.Append(@"\#"); break;
                    case '%': sb.Append(@"\%"); break;
                    case ' ': sb.Append(@"\ "); break;
                    case '"': sb.Append(@"""'"); break;
                    case ',': sb.Append(@"{,}"); break;
                    default: sb.Append(c); break;
                }
                //if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                //    sb.Append(c);
                //else
                //    sb.Append(@"\char""" + ((int)c).ToString("X") + @" ");
            }
            return sb.ToString().Replace("\n", @"\\");
        }
    }
}