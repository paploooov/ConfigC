namespace ConfigC.Copy;

public sealed record CopyResult(int FilesCopied, long BytesCopied, IReadOnlyList<string> Failures);
