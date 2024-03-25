using System;
using Snd.Sdk.Helpers;
using Snd.Sdk.Storage.Models.BlobPath;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class IBlobPathTests
{
    [Theory]
    [InlineData("abfss://container@key")]
    [InlineData("container@key")]
    public void AdlsGen2PathParseSuccessTest(string path)
    {
        var adlsPath = path.AsAdlsGen2Path();
        Assert.Equal("container", adlsPath.Container);
        Assert.Equal("key", adlsPath.ObjectKey);
    }

    [Theory]
    [InlineData("abfkkk://container@key")]
    [InlineData("s3://container@key")]
    [InlineData("s3://container/key")]
    [InlineData("")]
    [InlineData("//container/key")]
    public void AdlsGen2PathParseFailTest(string path)
    {
        Assert.Throws<ArgumentException>(path.AsAdlsGen2Path);
    }

    [Theory]
    [InlineData("s3a://bucket/key")]
    public void AmazonS3PathParseSuccessTest(string path)
    {
        var adlsPath = path.AsAmazonS3Path();
        Assert.Equal("bucket", adlsPath.Bucket);
        Assert.Equal("key", adlsPath.ObjectKey);
    }

    [Theory]
    [InlineData("abfkkk://container@key")]
    [InlineData("s3://container@key")]
    [InlineData("s3://container/key")]
    [InlineData("")]
    [InlineData("//container/key")]
    [InlineData("bucket/key")]
    [InlineData("bucket@key")]
    public void AmazonS3PathParseFailTest(string path)
    {
        Assert.Throws<ArgumentException>(path.AsAmazonS3Path);
    }

    [Theory]
    [InlineData("abfss://container@key", nameof(AdlsGen2Path))]
    [InlineData("s3a://bucket/key", nameof(AmazonS3StoragePath))]
    public void TestFromHdfsPath(string path, string className)
    {
        var parsedPath = path.FromHdfsPath();
        Assert.Equal(className, parsedPath.GetType().Name);
    }
}
