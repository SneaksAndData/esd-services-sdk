using Snd.Sdk.Storage.Models.BlobPath;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class AmazonS3BlobStoragePathTests
{
    
    [Fact]
    public static void CanParsePath()
    {
        // Arrange
        const string path = "s3a://bucket-name/folder1/folder2/file.txt";
        
        // Act
        var storagePath = new AmazonS3StoragePath(path);
        
        // Assert
        Assert.Equal("bucket-name", storagePath.Bucket);
        Assert.Equal("folder1/folder2/file.txt", storagePath.ObjectKey);
    }
    
    [Fact]
    public static void CanSerializePath()
    {
        // Arrange
        const string path = "s3a://bucket-name/folder1/folder2/file.txt";
        
        // Act
        var storagePath = new AmazonS3StoragePath(path);
        
        // Assert
        Assert.Equal(path, storagePath.ToHdfsPath());
    }
    
    [Fact]
    public static void CanJoinPath()
    {
        // Arrange
        const string path = "s3a://bucket-name/";
        
        // Act
        var storagePath = new AmazonS3StoragePath(path).Join("/folder1/folder2/file.txt");
        
        // Assert
        Assert.Equal("s3a://bucket-name/folder1/folder2/file.txt", storagePath.ToHdfsPath());
    }
    
    [Fact]
    public static void TrimsDuplicatedSlashes()
    {
        // Arrange
        const string path = "s3a://bucket-name/";
        
        // Act
        var storagePath = new AmazonS3StoragePath(path).Join("/folder1///folder2/file.txt");
        
        // Assert
        Assert.Equal("s3a://bucket-name/folder1/folder2/file.txt", storagePath.ToHdfsPath());
    }
}
