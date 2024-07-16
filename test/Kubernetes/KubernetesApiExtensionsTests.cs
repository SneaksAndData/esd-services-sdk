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
    [InlineData("Restart", "Restart", new int[] { 1, 2 }, new int[] { 2, 3 }, 3)]
    [InlineData("Restart", "Fail", null, null, null)] // Expecting an exception
    public void MergePodFailurePolicyRules_Test(string actionA, string actionB, int[] exitCodesA, int[] exitCodesB, int? expectedExitCodesCount)
    {
        var ruleA = new V1PodFailurePolicyRule(actionA,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesA?.ToList()));

        var ruleB = new V1PodFailurePolicyRule(actionB,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesB?.ToList()));

        if (expectedExitCodesCount.HasValue)
        {
            var result = KubernetesApiExtensions.MergePodFailurePolicyRules(ruleA, ruleB);

            Assert.Equal(actionA, result.Action);
            Assert.Equal(expectedExitCodesCount.Value, result.OnExitCodes.Values.Count);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => KubernetesApiExtensions.MergePodFailurePolicyRules(ruleA, ruleB));
        }
    }

    [Theory]
    [InlineData("Restart", new int[] { 1, 2, 3 })]
    public void ConvertToFailurePolicyRule_ValidConversion(string action, int[] exitCodes)
    {
        var actionPair = new KeyValuePair<string, List<int>>(action, new List<int>(exitCodes));

        var result = KubernetesApiExtensions.ConvertToFailurePolicyRule(actionPair);

        Assert.Equal(action, result.Action);
        Assert.Equal(exitCodes.Length, result.OnExitCodes.Values.Count);
        for (int i = 0; i < exitCodes.Length; i++)
        {
            Assert.Contains(exitCodes[i], result.OnExitCodes.Values);
        }
    }

    [Theory]
    [InlineData("Ignore")]
    public void ConvertToDisruptionTargetPolicyRule_ValidConversion(string action)
    {
        var result = KubernetesApiExtensions.ConvertToDisruptionTargetPolicyRule(action);

        Assert.Equal(action, result.Action);
        Assert.Single(result.OnPodConditions);
        Assert.Equal("True", result.OnPodConditions[0].Status);
        Assert.Equal("DisruptionTarget", result.OnPodConditions[0].Type);
    }

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

    [Fact]
    public void WithPodPolicyFailureDisruptionTarget()
    {
        var job = new V1Job();

        var result = job.WithPodPolicyFailureDisruptionTarget();

        var actions = result.Spec.PodFailurePolicy.Rules.Select(rule => rule.Action).ToList();
        Assert.Contains("Ignore", actions);
    }
}