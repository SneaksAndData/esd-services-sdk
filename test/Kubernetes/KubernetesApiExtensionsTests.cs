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
    [InlineData("Restart", "Restart", new int[] { 1, 2 }, new int[] { 2, 3 })]
    public void MergePodFailurePolicyRules(string actionA, string actionB, int[] exitCodesA,
        int[] exitCodesB)
    {
        var ruleA = new V1PodFailurePolicyRule(actionA,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesA?.ToList()));

        var ruleB = new V1PodFailurePolicyRule(actionB,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesB?.ToList()));

        var expectedRule = new V1PodFailurePolicyRule(actionA, // Assuming the action from ruleA is the expected action
            new V1PodFailurePolicyOnExitCodesRequirement("In",
                exitCodesA.Union(exitCodesB).ToList())); // Assuming the merge logic

        var result = KubernetesApiExtensions.MergePodFailurePolicyRules(ruleA, ruleB);

        Assert.Equal(expectedRule.OnExitCodes.Values.OrderBy(expectedValues => expectedValues).ToList(),
            result.OnExitCodes.Values.OrderBy(resultValues => resultValues).ToList());
    }

    [Theory]
    [InlineData("Restart", "Fail", null, null)]
    public void MergePodFailurePolicyRules_Exception_Test(string actionA, string actionB, int[] exitCodesA,
        int[] exitCodesB)
    {
        var ruleA = new V1PodFailurePolicyRule(actionA,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesA?.ToList()));

        var ruleB = new V1PodFailurePolicyRule(actionB,
            new V1PodFailurePolicyOnExitCodesRequirement("In", exitCodesB?.ToList()));

        Assert.Throws<InvalidOperationException>(() =>
            KubernetesApiExtensions.MergePodFailurePolicyRules(ruleA, ruleB));
    }


    [Theory]
    [InlineData("Restart", new int[] { 1, 2, 3 })]
    public void ConvertToFailurePolicyRule_ValidConversion(string action, int[] exitCodes)
    {
        var actionPair = new KeyValuePair<string, List<int>>(action, new List<int>(exitCodes));

        var result = KubernetesApiExtensions.ConvertToFailurePolicyRule(actionPair);

        Assert.Equal(exitCodes.OrderBy(expectedExitCodes => expectedExitCodes).ToList(),
            result.OnExitCodes.Values.OrderBy(resultExitCodes => resultExitCodes).ToList());
    }

    [Theory]
    [InlineData("Ignore")]
    public void ConvertToDisruptionTargetPolicyRule_ValidConversion(string action)
    {
        var result = KubernetesApiExtensions.ConvertToDisruptionTargetPolicyRule(action);

        Assert.True(action == result.Action && result.OnPodConditions.Count == 1 &&
                    result.OnPodConditions.First().Status == "True" &&
                    result.OnPodConditions.First().Type == "DisruptionTarget");
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

        Assert.Contains("True", result.Spec.PodFailurePolicy.Rules
            .Where(rule => rule.Action == "Ignore")
            .SelectMany(rule => rule.OnPodConditions)
            .Where(condition => condition.Type == "DisruptionTarget")
            .Select(condition => condition.Status)
            .ToList());
    }
}