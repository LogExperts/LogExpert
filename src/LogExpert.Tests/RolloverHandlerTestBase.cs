using LogExpert.Core.Classes.Log;

namespace LogExpert.Tests;

internal class RolloverHandlerTestBase
{
    #region Fields

    public const string TEST_DIR_NAME = "test";

    #endregion

    public DirectoryInfo TestDirectory { get; set; }

    protected LinkedList<string> CreateTestFilesWithDate ()
    {
        LinkedList<string> createdFiles = new();
        var dInfo = Directory.CreateDirectory(TEST_DIR_NAME);
        TestDirectory = dInfo;
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-08_1.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-08_0.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-10_0.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-11_1.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-11_0.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-12_2.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-12_1.log"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine_2010-06-12_0.log"));
        return createdFiles;
    }

    protected LinkedList<string> CreateTestFilesWithoutDate ()
    {
        LinkedList<string> createdFiles = new();
        var dInfo = Directory.CreateDirectory(TEST_DIR_NAME);
        TestDirectory = dInfo;
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.6"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.5"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.4"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.3"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.2"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log.1"));
        _ = createdFiles.AddLast(CreateFile(dInfo, "engine.log"));
        return createdFiles;
    }

    protected static LinkedList<string> RolloverSimulation (LinkedList<string> files, string formatPattern,
        bool deleteLatestFile)
    {
        var fileList = files;
        RolloverFilenameBuilder fnb = new(formatPattern);
        fnb.SetFileName(fileList.Last.Value);
        fnb.Index += fileList.Count;
        var newFileName = fnb.BuildFileName();
        _ = fileList.AddFirst(newFileName);
        var enumerator = fileList.GetEnumerator();
        var nextEnumerator = fileList.GetEnumerator();
        _ = nextEnumerator.MoveNext(); // move on 2nd entry
        _ = enumerator.MoveNext();

        while (nextEnumerator.MoveNext())
        {
            File.Move(nextEnumerator.Current, enumerator.Current);
            _ = enumerator.MoveNext();
        }

        _ = CreateFile(null, enumerator.Current);

        if (deleteLatestFile)
        {
            File.Delete(fileList.First.Value);
            fileList.RemoveFirst();
        }

        return fileList;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    protected static void Cleanup ()
    {
        try
        {
            Directory.Delete(TEST_DIR_NAME, true);
        }
        catch (Exception)
        {
        }
    }

    protected static string CreateFile (DirectoryInfo dInfo, string fileName)
    {
        var lineCount = 10;
        var fullName = dInfo == null ? fileName : dInfo.FullName + Path.DirectorySeparatorChar + fileName;

        using (StreamWriter writer = new(File.Create(fullName)))
        {
            for (var i = 1; i <= lineCount; ++i)
            {
                writer.WriteLine($"Line number {i:D3} of File {fullName}");
            }

            writer.Flush();
        }

        return fullName;
    }
}