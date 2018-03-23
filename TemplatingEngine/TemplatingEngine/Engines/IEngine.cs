using System.IO;
using System.Threading.Tasks;

namespace TemplatingEngine.Engines {

    public interface IEngine {

        string Generate<T>(string template, T context);

        void Generate<T>(Stream template, T context, Stream output);

        Task<string> GenerateAsync<T>(string template, T context);

        void GenerateAsync<T>(Stream template, T context, Stream output);

        string GenerateDynamic(string template, dynamic context);

        void GenerateDynamic(Stream template, dynamic context, Stream output);

        Task<string> GenerateDynamicAsync(string template, dynamic context);

        void GenerateDynamicAsync(Stream template, dynamic context, Stream output);
    }
}