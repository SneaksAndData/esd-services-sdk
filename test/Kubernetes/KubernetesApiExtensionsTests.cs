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


    [Theory]
    [InlineData("RetryJob", 1, "RetryJob", 2, "RetryJob", 3)]
    [InlineData("Ignore", 127, "FailJob", 255, "FailJob", 254)]
    public void WithPolicyFailureExitCodes(
        string action1, int exitCode1, string action2, int exitCode2, string action3, int exitCode3)
    {
        // Arrange
        var job = new V1Job();
        var actionExitCodeId = new List<(string action, int exitCode)>
        {
            (action1, exitCode1),
            (action2, exitCode2),
            (action3, exitCode3)
        };

        // Act
        var result = job.WithPolicyFailureExitCodes(actionExitCodeId);

        // Assert
        Assert.Equal(2, result.Spec.PodFailurePolicy.Rules.Count);
        Assert.Equal(action1, result.Spec.PodFailurePolicy.Rules[0].Action);
        Assert.Equal(new List<int> { exitCode1, exitCode2 }, result.Spec.PodFailurePolicy.Rules[0].OnExitCodes.Values);
        Assert.Equal(new List<int> { exitCode3 }, result.Spec.PodFailurePolicy.Rules[1].OnExitCodes.Values);
        Assert.Equal(action3, result.Spec.PodFailurePolicy.Rules[1].Action);
        Assert.Equal(new List<int> { exitCode3 }, result.Spec.PodFailurePolicy.Rules[1].OnExitCodes.Values);
    }
}