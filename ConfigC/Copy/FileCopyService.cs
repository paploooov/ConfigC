namespace ConfigC.Copy;

/// <summary>Recursively copies a Steam profile folder into another, reporting progress as it goes.</summary>
public static class FileCopyService
{
    public static IReadOnlyList<string> EnumerateFiles(string sourcePath) =>
        Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);

    /// <param name="onFileCopied">Invoked after each file with (index, total, relative path, bytes copied so far).</param>
    public static CopyResult CopyRecursively(
        string sourcePath,
        string targetPath,
        Action<int, int, string, long>? onFileCopied = null)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        var files = EnumerateFiles(sourcePath);
        var failures = new List<string>();
        var copiedCount = 0;
        long bytesCopied = 0;

        for (var i = 0; i < files.Count; i++)
        {
            var sourceFile = files[i];
            var destFile = sourceFile.Replace(sourcePath, targetPath);

            try
            {
                File.Copy(sourceFile, destFile, overwrite: true);
                bytesCopied += new FileInfo(destFile).Length;
                copiedCount++;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetRelativePath(sourcePath, sourceFile)}: {ex.Message}");
            }

            onFileCopied?.Invoke(i + 1, files.Count, Path.GetRelativePath(sourcePath, sourceFile), bytesCopied);
        }

        return new CopyResult(copiedCount, bytesCopied, failures);
    }
}
