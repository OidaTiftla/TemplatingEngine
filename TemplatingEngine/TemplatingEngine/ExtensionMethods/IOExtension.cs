using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ExtensionMethods {

    internal static class IOExtension {

        #region file info

        public static string NameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }

        public static string RelativeTo(this FileInfo dest, FileInfo file) {
            return dest.MakeRelativeTo(file.Directory);
        }

        public static string MakeRelativeTo(this FileInfo dest, DirectoryInfo dir) {
            // source: http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            // with my adjustments
            Uri pathUri = new Uri(dest.FullName);
            // Folders must end in a slash
            string folder = dir.FullName;
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            var rel = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString());
            if (string.IsNullOrWhiteSpace(rel))
                rel = "./";
            return rel.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion file info

        #region directory info

        public static string MakeRelativeTo(this DirectoryInfo dest, DirectoryInfo dir) {
            // source: http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            // with my adjustments
            // Folders must end in a slash
            string folder = dest.FullName;
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri pathUri = new Uri(folder);
            // Folders must end in a slash
            folder = dir.FullName;
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            var rel = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString());
            if (string.IsNullOrWhiteSpace(rel))
                rel = "./";
            return rel.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion directory info

        #region resolve relative

        public static string ResolveRelativeTo(this string relativePath, FileInfo file) {
            return relativePath.ResolveRelativeTo(file.Directory);
        }

        public static string ResolveRelativeTo(this string relativePath, DirectoryInfo dir) {
            // source: http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            // with my adjustments

            // Folders must end in a slash
            string folder = dir.FullName;
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri baseUri = new Uri(folder);

            Uri resolvedUri = new Uri(baseUri, relativePath);
            // substring with start at 8 because I want to skip 'file:///'
            var rel = Uri.UnescapeDataString(resolvedUri.ToString().Substring(8));
            if (string.IsNullOrWhiteSpace(rel))
                rel = "./";
            return rel.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion resolve relative

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

        #endregion temp file & directory

        #region uri

        public static string Name(this Uri uri) {
            if (uri.Segments.Length > 0)
                return uri.Segments[uri.Segments.Length - 1].Trim('/').Trim('\\');
            return null;
        }

        public static Uri AddPath(this Uri uri, string path) {
            var ub = new UriBuilder(uri);
            var p = ub.Path;

            if (p.EndsWith("/") == false)
                p += "/";

            p += Uri.EscapeDataString(path);

            ub.Path = p;
            return ub.Uri;
        }

        #endregion uri

        #region stream write to other stream

        public class ProgressChangedEventArgs {
            public long TotalBytes { get; private set; }
            public long TransferedBytes { get; private set; }
            public bool Cancel { get; set; }

            public ProgressChangedEventArgs(long totalBytes, long transferedBytes) {
                this.TotalBytes = totalBytes;
                this.TransferedBytes = transferedBytes;
            }
        }

        public static long CopyTo(this FileInfo source, FileInfo destination,
            Action<ProgressChangedEventArgs> progressChangedEventHandler = null,
            CancellationToken? token = null) {
            var copyTask = source.CopyToAsync(destination, progressChangedEventHandler, token);
            copyTask.Wait();
            return copyTask.Result;
        }

        public static async Task<long> CopyToAsync(this FileInfo source, FileInfo destination,
            Action<ProgressChangedEventArgs> progressChangedEventHandler = null,
            CancellationToken? token = null) {
            using (var inputStream = source.OpenRead())
            using (var outputStream = destination.OpenWrite()) {
                return await inputStream.WriteToAsync(outputStream, progressChangedEventHandler, token: token);
            }
        }

        public static long WriteTo(this Stream input, Stream output,
            Action<ProgressChangedEventArgs> progressChangedEventHandler = null,
            int bufferSize = 1024 * 1024 /* 1MB */,
            CancellationToken? token = null) {
            var writeTask = input.WriteToAsync(output, progressChangedEventHandler, bufferSize, token);
            writeTask.Wait();
            return writeTask.Result;
        }

        // source: https://stackoverflow.com/a/26556205
        // with my modifications
        public static async Task<long> WriteToAsync(this Stream input, Stream output,
            Action<ProgressChangedEventArgs> progressChangedEventHandler = null,
            int bufferSize = 1024 * 1024 /* 1MB */,
            CancellationToken? token = null) {
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];
            bool swap = false;
            int read = 0;
            long size = 0;
            long len = input.Length;
            Task writer = null;

            // check for cancellation
            token?.ThrowIfCancellationRequested();

            try {
                // report
                if (!notifyCaller(progressChangedEventHandler, len, size)) {
                    // cancel
                    throw new OperationCanceledException("The operation was canceled.");
                }

                // check for cancellation
                token?.ThrowIfCancellationRequested();

                output.SetLength(input.Length);
                for (size = 0; size < len; size += read) {
                    // check for cancellation
                    token?.ThrowIfCancellationRequested();

                    // read
                    read = input.Read(swap ? buffer1 : buffer2, 0, bufferSize);

                    // check for cancellation
                    token?.ThrowIfCancellationRequested();

                    // wait for write finished
                    if (writer != null)
                        await writer;
                    writer = null;

                    // check for cancellation
                    token?.ThrowIfCancellationRequested();

                    // report written bytes
                    if (!notifyCaller(progressChangedEventHandler, len, size)) {
                        // cancel
                        throw new OperationCanceledException("The operation was canceled.");
                    }

                    // check for cancellation
                    token?.ThrowIfCancellationRequested();

                    // write async
                    writer = output.WriteAsync(swap ? buffer1 : buffer2, 0, read);
                    swap = !swap;
                }
            } finally {
                // wait for write finished
                if (writer != null)
                    await writer; //Fixed - Thanks @sam-hocevar
                writer = null;
            }
            // write all contents to disk
            output.Flush();

            // check for cancellation
            token?.ThrowIfCancellationRequested();

            return size;
        }

        private static bool notifyCaller(Action<ProgressChangedEventArgs> progressChangedEventHandler, long length, long count) {
            try {
                if (progressChangedEventHandler != null) {
                    var eArgs = new ProgressChangedEventArgs(length, count);
                    progressChangedEventHandler(eArgs);
                    return !eArgs.Cancel;
                }
            } catch { }
            return true;
        }

        #region timestamps

        // source: https://stackoverflow.com/a/17213170
        // with my modifications
        public static void UpdateTimestamps(this FileInfo destination, FileInfo source) {
            source.Refresh();
            destination.Refresh();
            if (destination.IsReadOnly) {
                destination.IsReadOnly = false;
                destination.CreationTime = source.CreationTime;
                destination.LastWriteTime = source.LastWriteTime;
                destination.LastAccessTime = source.LastAccessTime;
                destination.IsReadOnly = true;
            } else {
                destination.CreationTime = source.CreationTime;
                destination.LastWriteTime = source.LastWriteTime;
                destination.LastAccessTime = source.LastAccessTime;
            }
        }

        public static void UpdateAttributes(this FileInfo destination, FileInfo source) {
            source.Refresh();
            destination.Refresh();
            if (destination.IsReadOnly) {
                destination.IsReadOnly = false;
                destination.Attributes = source.Attributes;
                destination.IsReadOnly = source.IsReadOnly;
            } else {
                destination.Attributes = source.Attributes;
            }
        }

        // source: https://stackoverflow.com/a/17213170
        // adjusted for directories
        // with my modifications
        public static void UpdateTimestamps(this DirectoryInfo destination, DirectoryInfo source) {
            source.Refresh();
            destination.Refresh();
            destination.CreationTime = source.CreationTime;
            destination.LastWriteTime = source.LastWriteTime;
            destination.LastAccessTime = source.LastAccessTime;
        }

        public static void UpdateAttributes(this DirectoryInfo destination, DirectoryInfo source) {
            source.Refresh();
            destination.Refresh();
            destination.Attributes = source.Attributes;
        }

        #endregion timestamps

        #endregion stream write to other stream

        #region download path

        public static DirectoryInfo GetDownloadsPath() {
            string path = null;
            if (Environment.OSVersion.Version.Major >= 6) {
                IntPtr pathPtr;
                int hr = SHGetKnownFolderPath(ref FolderDownloads, 0, IntPtr.Zero, out pathPtr);
                if (hr == 0) {
                    path = Marshal.PtrToStringUni(pathPtr);
                    Marshal.FreeCoTaskMem(pathPtr);
                    return new DirectoryInfo(path);
                }
            }
            path = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            path = Path.Combine(path, "Downloads");
            return new DirectoryInfo(path);
        }

        private static Guid FolderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

        #endregion download path

        #region search pattern

        /// <summary>
        /// Create Regex form search pattern
        /// It is the same pattern as in directory searches
        ///
        /// * => 0 or more chars at this position
        /// ? => 0 or one char at this position
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public static Regex MakeRegexFromSearchPattern(string searchPattern) {
            return new Regex("^" + Regex.Escape(searchPattern ?? "*").Replace(@"\*", ".*").Replace(@"\?", ".?") + "$");
        }

        #endregion search pattern

        #region executing assembly

        public static string GetFileNameOfExecutingAssembly() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            if (assembly == null)
                return null;
            string codebase = assembly.CodeBase;
            if (codebase == null)
                return null;
            Uri p = new Uri(codebase);
            return p.LocalPath;
        }

        public static string GetConfigFileNameForExecutingAssembly() {
            string localPath = IOExtension.GetFileNameOfExecutingAssembly();
            if (localPath == null)
                return null;
            return localPath + ".config";
        }

        #endregion executing assembly
    }
}