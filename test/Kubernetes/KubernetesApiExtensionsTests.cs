using System.Collections.Generic;
using k8s.Models;
using Snd.Sdk.Kubernetes;
using Xunit;

namespace Snd.Sdk.Tests.Kubernetes;

public class KubernetesApiExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithAnnotations(bool doReplace)
    {
        var job = new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Annotations = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            },
            Spec = new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Annotations = new Dictionary<string, string>()
                    }
                }
            }
        };

        var replacement = new Dictionary<string, string>
        {
            { "key1", "value2" },
            { "key3", "value3" }
        };

        var newJob = job.WithAnnotations(replacement, replace: doReplace);

        var expected = doReplace
            ? replacement
            : new Dictionary<string, string>
            {
                { "key1", "value2" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

        Assert.Equal(expected, newJob.Metadata.Annotations);
        Assert.Equal(expected, newJob.Spec.Template.Metadata.Annotations);
    }
}
