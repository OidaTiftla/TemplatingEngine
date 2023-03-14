using System.IO;

namespace ExtensionMethods {
    public static partial class IOExtension {
        #region resolve relative

        public static string ResolveRelativeTo(this string relativePath, FileInfo file) {
            return relativePath.ResolveRelativeTo(file.Directory ?? new DirectoryInfo(""));
        }

        public static string ResolveRelativeTo(this string relativePath, DirectoryInfo dir) {
            // source: http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            // with my adjustments

            // Folders must end in a slash
            var folder = dir.FullName;
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCulture)) {
                folder += Path.DirectorySeparatorChar;
            }
            var baseUri = new Uri(folder);

            var resolvedUri = new Uri(baseUri, relativePath);
            var rel = Uri.UnescapeDataString(resolvedUri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(rel)) {
                rel = "./";
            }

            return rel.Replace('/', Path.DirectorySeparatorChar);
        }

        public static string ResolveRelativeToExecutingAssembly(this string relativPath) {
            return Path.Combine(AppContext.BaseDirectory, relativPath);
        }

        #endregion resolve relative

        #region file info
        public static string NameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        #endregion

        #region temp file & directory
        public static FileInfo CreateTempFile() {
            return new FileInfo(Path.GetTempFileName());
        }

        public static DirectoryInfo CreateTempDirectory() {
            string tmp;
            while (Directory.Exists(tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())))
                ;
            return Directory.CreateDirectory(tmp);
        }
        #endregion
    }
}
