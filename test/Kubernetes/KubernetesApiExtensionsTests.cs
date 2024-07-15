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
    [InlineData("Restart", "Restart", new int[] { 1, 2 }, new int[] { 2, 3 }, 3, new string[] { "DisruptionTarget", "AnotherCondition" }, new string[] { "AnotherCondition", "OneMoreCondition" }, 3)]
    [InlineData("Restart", "Fail", null, null, null, null, null, null)] // Expecting an exception
    public void MergePodFailurePolicyRules_Test(string actionA, string actionB, int[] exitCodesA, int[] exitCodesB, int? expectedExitCodesCount, string[] onPodConditionsA, string[] onPodConditionsB, int? expectedPodConditionsCount)
    {
        var ruleA = new V1PodFailurePolicyRule(actionA,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesA?.ToList()),
            onPodConditionsA?.Select(ruleName => new V1PodFailurePolicyOnPodConditionsPattern("True", ruleName)).ToList());

        var ruleB = new V1PodFailurePolicyRule(actionB,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesB?.ToList()),
            onPodConditionsB?.Select(ruleName => new V1PodFailurePolicyOnPodConditionsPattern("True", ruleName)).ToList());

        if (expectedExitCodesCount.HasValue && expectedPodConditionsCount.HasValue)
        {
            var result = KubernetesApiExtensions.MergePodFailurePolicyRules(ruleA, ruleB);

            Assert.Equal(actionA, result.Action);
            Assert.Equal(expectedExitCodesCount.Value, result.OnExitCodes.Values.Count);
            Assert.Equal(expectedPodConditionsCount.Value, result.OnPodConditions.Count);
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

    [Theory]
    [InlineData("RetryJob", new[] { 1, 2, 3 }, "Ignore", new[] { 127 }, "FailJob", new[] { 255, 254 })]
    public void WithPodPolicyFailureExitCodes(
        string action1, int[] exitCodes1, string action2, int[] exitCodes2, string action3, int[] exitCodes3)
    {
        var job = new V1Job();
        var actions = new Dictionary<string, List<int>>
        {
            { action1, exitCodes1.ToList() },
            { action2, exitCodes2.ToList() },
            { action3, exitCodes3.ToList() }
        };

        var result = job.WithPodPolicyFailureExitCodes(actions);

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