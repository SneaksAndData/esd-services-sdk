using Snd.Sdk.Storage.Models.BlobPath;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class AdlsGen2PathTests
{

    private const string path = "abfss://container@folder1/folder2/file.txt";

    [Fact]
    public static void CanParsePath()
    {
        // Act
        var storagePath = new AdlsGen2Path(path);

        // Assert
        Assert.Equal("container", storagePath.Container);
        Assert.Equal("folder1/folder2/file.txt", storagePath.FullPath);
    }

    [Fact]
    public static void CanSerializePath()
    {
        // Act
        var storagePath = new AdlsGen2Path(path);

        // Assert
        Assert.Equal(path, storagePath.ToHdfsPath());
    }
}
