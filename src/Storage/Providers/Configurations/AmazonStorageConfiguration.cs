using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using Amazon;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;

namespace Snd.Sdk.Storage.Providers.Configurations
{
    /// <summary>
    /// Configuration for AWS S3.
    /// </summary>
    public sealed class AmazonStorageConfiguration
    {
        /// <summary>
        /// AWS access key.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// AWS secret key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// AWS service URL.
        /// </summary>
        public Uri ServiceUrl { get; set; }

        /// <summary>
        /// Force HTTP protocol
        /// </summary>
        public bool UseHttp => ServiceUrl.Scheme == "http";


        /// <summary>
        /// Initialize from environment variables.
        /// </summary>
        /// <returns></returns>
        public static AmazonStorageConfiguration CreateFromEnv()
        {
            // TODO: should be GetDomainEnvironmentVariable
            return new AmazonStorageConfiguration
            {
                AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
                SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
                ServiceUrl = new Uri(Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL"))
            };
        }
    }
}
