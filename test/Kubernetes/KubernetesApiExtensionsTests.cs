using System.Collections.Generic;
using k8s.Models;
using Snd.Sdk.Kubernetes;
using Xunit;
using System;
using System.Linq;

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
    [InlineData("RetryJob", new[] { 1, 2, 3 }, "Ignore", new[] { 127 }, "FailJob", new[] { 255, 254 })]
    public void WithPodPolicyFailureExitCodes(
        string action1, int[] exitCodes1, string action2, int[] exitCodes2, string action3, int[] exitCodes3)
    {
        // Arrange
        var job = new V1Job();
        var actions = new Dictionary<ValueTuple<string, List<int>>, ValueType>
        {
            { (action1, exitCodes1.ToList()), default(ValueType) },
            { (action2, exitCodes2.ToList()), default(ValueType) },
            { (action3, exitCodes3.ToList()), default(ValueType) }
        };

        // Act
        var result = job.WithPodPolicyFailureExitCodes(actions);

        // Assert
        Assert.Equal(actions.Count, result.Spec.PodFailurePolicy.Rules.Count);

        foreach (var action in actions)
        {
            var rule = result.Spec.PodFailurePolicy.Rules.FirstOrDefault(ruleElement =>
                ruleElement.Action == action.Key.Item1);
            Assert.NotNull(rule);
            Assert.Equal(action.Key.Item2.Distinct().ToList(), rule.OnExitCodes.Values);
        }
    }
}