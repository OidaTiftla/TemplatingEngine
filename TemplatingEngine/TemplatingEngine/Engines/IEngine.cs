using System.IO;
using System.Threading.Tasks;

namespace TemplatingEngine.Engines {

    public interface IEngine {

        string Generate<T>(string template, T context);

        void Generate<T>(StreamReader template, T context, StreamWriter output);

        Task<string> GenerateAsync<T>(string template, T context);

        void GenerateAsync<T>(StreamReader template, T context, StreamWriter output);

        string GenerateDynamic(string template, dynamic context);

        void GenerateDynamic(StreamReader template, dynamic context, StreamWriter output);

        Task<string> GenerateDynamicAsync(string template, dynamic context);

        void GenerateDynamicAsync(StreamReader template, dynamic context, StreamWriter output);
    }
}