using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class ProcessGitClient : IGitClient
    {
        public async Task<IReadOnlyList<GitRemoteRef>> ListRemoteRefsAsync(string repositoryUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(repositoryUrl))
                return new List<GitRemoteRef>();

            var output = await RunGitAsync($"ls-remote --heads --tags {Quote(repositoryUrl)}", null, cancellationToken);
            var refs = new Dictionary<string, GitRemoteRef>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t');
                if (parts.Length != 2)
                    continue;

                var refPath = parts[1];
                if (refPath.EndsWith("^{}", StringComparison.Ordinal))
                    continue;

                if (refPath.StartsWith("refs/heads/", StringComparison.Ordinal))
                {
                    var name = refPath.Substring("refs/heads/".Length);
                    refs[name] = new GitRemoteRef(name, name, false);
                }
                else if (refPath.StartsWith("refs/tags/", StringComparison.Ordinal))
                {
                    var name = refPath.Substring("refs/tags/".Length);
                    refs[name] = new GitRemoteRef(name, name, true);
                }
            }

            return refs.Values.ToList();
        }

        public async Task<string> PrepareRepositoryAsync(string repositoryUrl, string revision, string cacheRoot, CancellationToken cancellationToken)
        {
            var cleanRepositoryUrl = GitUrlUtility.ExtractRepositoryUrl(repositoryUrl);
            if (string.IsNullOrWhiteSpace(cleanRepositoryUrl))
                throw new ArgumentException("Repository URL is required.", nameof(repositoryUrl));

            Directory.CreateDirectory(cacheRoot);
            var repositoryPath = Path.Combine(cacheRoot, StableHash.Sha1(cleanRepositoryUrl));
            var gitDirectory = Path.Combine(repositoryPath, ".git");

            if (!Directory.Exists(gitDirectory))
            {
                await RunGitAsync($"clone --depth 1 {Quote(cleanRepositoryUrl)} {Quote(repositoryPath)}", null, cancellationToken);
            }
            else
            {
                await RunGitAsync("fetch --all --tags --prune", repositoryPath, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(revision))
                await RunGitAsync($"checkout {Quote(revision)}", repositoryPath, cancellationToken);
            else
                await RunGitAsync("checkout --detach origin/HEAD", repositoryPath, cancellationToken);

            return repositoryPath;
        }

        public Task<IReadOnlyList<string>> FindUnityPackageFilesAsync(string repositoryPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath) || !Directory.Exists(repositoryPath))
                return Task.FromResult<IReadOnlyList<string>>(new List<string>());

            var files = Directory.GetFiles(repositoryPath, "*.unitypackage", SearchOption.AllDirectories)
                .Select(path => ToRelativePath(repositoryPath, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(files);
        }

        private static async Task<string> RunGitAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            using (var process = new Process { StartInfo = startInfo })
            using (cancellationToken.Register(() => TryKill(process)))
            {
                process.Start();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.Run(() => process.WaitForExit(), cancellationToken);

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"git {arguments} failed with exit code {process.ExitCode}: {error}");

                return output;
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch
            {
            }
        }

        private static string ToRelativePath(string root, string path)
        {
            var rootUri = new Uri(AppendDirectorySeparator(root));
            var pathUri = new Uri(path);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string Quote(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }
    }
}
