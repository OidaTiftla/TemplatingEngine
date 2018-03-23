using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TemplatingEngine.Engines {

    public class RazorEngine : IEngine {

        #region implement IEngine

        public string Generate<T>(string template, T context) {
            throw new System.NotImplementedException();
        }

        public void Generate<T>(StreamReader template, T context, StreamWriter output) {
            throw new System.NotImplementedException();
        }

        public async Task<string> GenerateAsync<T>(string template, T context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.Generate(template, context);
            });
            return output;
        }

        public async void GenerateAsync<T>(StreamReader template, T context, StreamWriter output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.Generate(template, context, output);
            });
        }

        public string GenerateDynamic(string template, dynamic context) {
            throw new System.NotImplementedException();
        }

        public void GenerateDynamic(StreamReader template, dynamic context, StreamWriter output) {
            throw new System.NotImplementedException();
        }

        public async Task<string> GenerateDynamicAsync(string template, dynamic context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.GenerateDynamic(template, context);
            });
            return output;
        }

        public async void GenerateDynamicAsync(StreamReader template, dynamic context, StreamWriter output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.GenerateDynamic(template, context, output);
            });
        }

        #endregion implement IEngine
    }
}