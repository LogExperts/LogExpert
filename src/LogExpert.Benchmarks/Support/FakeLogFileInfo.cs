using ColumnizerLib;

namespace LogExpert.Benchmarks.Support;

/// <summary>
/// Minimal ILogFileInfo stub for benchmarks. No filesystem access.
/// Wraps an in-memory byte array as the file content.
/// </summary>
internal sealed class FakeLogFileInfo : ILogFileInfo
{
    private readonly byte[] _content;

    public FakeLogFileInfo (string name = "fake.log", byte[]? content = null, long length = 1_000_000)
    {
        FullName = name;
        _content = content ?? [];
        Length = content?.Length ?? length;
        OriginalLength = Length;
    }

    public string FullName { get; }
    public string FileName => Path.GetFileName(FullName);
    public string DirectoryName => Path.GetDirectoryName(FullName) ?? "";
    public char DirectorySeparatorChar => Path.DirectorySeparatorChar;
    public Uri Uri => new($"file:///{FullName}");
    public long Length { get; set; }
    public long OriginalLength { get; }
    public bool FileExists => true;
    public int PollInterval => 250;

    public bool FileHasChanged () => false;
    public Stream OpenStream () => new MemoryStream(_content, writable: false);
    public ILogFileInfo GetRolloverInfo (string fileName) => new FakeLogFileInfo(fileName);
}