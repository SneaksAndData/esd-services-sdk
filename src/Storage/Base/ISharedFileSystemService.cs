using Akka;
using Akka.IO;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base
{
    /// <summary>
    /// Shared filesystem implementation. (NFS, Samba etc.).
    /// </summary>
    public interface ISharedFileSystemService
    {
        /// <summary>
        /// Lists files under path on a SMB/NFS share.
        /// </summary>
        /// <param name="fileSystemName">Name of an object serving as a shared file system: SMB/NFS share name.</param>
        /// <param name="path">Path to ls.</param>
        /// <returns></returns>
        Source<ShareFile, NotUsed> ListFiles(string fileSystemName, string path);

        /// <summary>
        /// Reads a text file from a SMB/NFS share.
        /// </summary>
        /// <param name="fileSystemName">Name of an object serving as a shared file system: SMB/NFS share name.</param>
        /// <param name="path">Path to file.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="bufferSize">Read buffer size in bytes.</param>
        /// <returns></returns>
        Source<ByteString, Task<IOResult>> ReadTextFile(string fileSystemName, string path, string fileName, int bufferSize = 4194304);

        /// <summary>
        /// Deletes a file from a SMB/NFS share.
        /// </summary>
        /// <param name="fileSystemName">Name of an object serving as a shared file system: SMB/NFS share name.</param>
        /// <param name="path">Path to file.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        Task<bool> RemoveFile(string fileSystemName, string path, string fileName);
    }
}
