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
        var actions = new Dictionary<string, List<int>>
        {
            { action1, exitCodes1.ToList() },
            { action2, exitCodes2.ToList() },
            { action3, exitCodes3.ToList() }
        };

        // Act
        var result = job.WithPodPolicyFailureExitCodes(actions);

        // Assert
        Assert.Equal(actions.Count, result.Spec.PodFailurePolicy.Rules.Count);

        foreach (var action in actions)
        {
            var rule = result.Spec.PodFailurePolicy.Rules.FirstOrDefault(ruleElement =>
                ruleElement.Action == action.Key);
            Assert.NotNull(rule);
            Assert.Equal(action.Value.Distinct().ToList(), rule.OnExitCodes.Values);
        }

        var additionalActions = new Dictionary<string, List<int>>
        {
            { "AdditionalAction1", new List<int> { 4, 5, 6 } },
            { "AdditionalAction2", new List<int> { 7, 8, 9 } }
        };

        result = result.WithPodPolicyFailureExitCodes(additionalActions);

        Assert.Equal(actions.Count + additionalActions.Count, result.Spec.PodFailurePolicy.Rules.Count);

        foreach (var action in additionalActions)
        {
            var rule = result.Spec.PodFailurePolicy.Rules.FirstOrDefault(ruleElement =>
                ruleElement.Action == action.Key);
            Assert.NotNull(rule);
            Assert.Equal(action.Value.Distinct().ToList(), rule.OnExitCodes.Values);
        }
    }
}