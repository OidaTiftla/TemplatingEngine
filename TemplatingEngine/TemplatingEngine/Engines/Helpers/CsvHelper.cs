namespace TemplatingEngine.Engines.Helpers {

    public class Csv {

        public static string Escape(object o) {
            return Escape(o.ToString());
        }

        public static string Escape(string text) {
            if (text?.Contains("\"") == true)
                return $"\"{text?.Replace("\"", "\"\"")?.Replace("\n", " ")}\"";
            else
                return $"{text?.Replace("\n", " ")}";
        }
    }
}