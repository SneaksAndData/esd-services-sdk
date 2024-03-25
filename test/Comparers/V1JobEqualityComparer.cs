using k8s.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Snd.Sdk.Tests.Comparers
{
    public class V1JobEqualityComparer : IEqualityComparer<V1Job>
    {
        public bool Equals(V1Job x, V1Job y)
        {
            return x.Metadata.Name == y.Metadata.Name
                && x.Metadata.Labels.All((lbl) => y.Metadata.Labels[lbl.Key] == lbl.Value)
                && x.Spec.ActiveDeadlineSeconds == y.Spec.ActiveDeadlineSeconds
                && x.Spec.BackoffLimit == y.Spec.BackoffLimit
                && x.Spec.Template.Spec.ServiceAccountName == y.Spec.Template.Spec.ServiceAccountName
                && x.Spec.Template.Spec.Containers[0].Image == y.Spec.Template.Spec.Containers[0].Image
                && x.Spec.Template.Spec.Containers[0].Command.All(c => y.Spec.Template.Spec.Containers[0].Command.Contains(c))
                && x.Spec.Template.Spec.Containers[0].Args.All(arg => x.Spec.Template.Spec.Containers[0].Args.Contains(arg))
                && x.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].Key == y.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].Key
                && x.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].OperatorProperty == y.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].OperatorProperty
                && x.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].Values[0] == y.Spec.Template.Spec.Affinity.NodeAffinity.RequiredDuringSchedulingIgnoredDuringExecution.NodeSelectorTerms[0].MatchExpressions[0].Values[0]
                && x.Spec.Template.Spec.Tolerations[0].Key == y.Spec.Template.Spec.Tolerations[0].Key
                && x.Spec.Template.Spec.Tolerations[0].OperatorProperty == y.Spec.Template.Spec.Tolerations[0].OperatorProperty
                && x.Spec.Template.Spec.Tolerations[0].Effect == y.Spec.Template.Spec.Tolerations[0].Effect
                && x.Spec.Template.Spec.Tolerations[0].Value == y.Spec.Template.Spec.Tolerations[0].Value;
        }

        public int GetHashCode([DisallowNull] V1Job obj)
        {
            throw new NotImplementedException();
        }
    }
}
