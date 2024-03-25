using k8s;
using Moq;
using Snd.Sdk.Kubernetes;
using Snd.Sdk.Kubernetes.Base;
using Snd.Sdk.Kubernetes.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Snd.Sdk.Tests.Kubernetes
{
    public class KubeFleetTests
    {
        [Fact]
        public void AddMember()
        {
            var fleet = new KubeFleet();

            var mockMembers = Enumerable.Range(0, 10).Append(1).Select(ix =>
            {
                var mockMember = new Mock<IKubeCluster>();
                var mock8s = new Mock<IKubernetes>();
                mock8s.Setup(k8s => k8s.BaseUri).Returns(new Uri($"https://{ix}"));
                mockMember.Setup(mm => mm.KubeApi).Returns(mock8s.Object);

                return mockMember.Object;
            });

            foreach (var member in mockMembers)
            {
                fleet.AddMember(member);
            }

            Assert.Equal(10, fleet.GetAllMembers().Count);
        }

        [Theory]
        [InlineData("1", "https://0.0.0.1/")]
        [InlineData("11", null)]
        public void GetMemberByName(string memberId, string memberApiUri)
        {
            var fleet = new KubeFleet();

            var mockMembers = Enumerable.Range(0, 10).Select(ix =>
            {
                var mockMember = new Mock<IKubeCluster>();
                var mock8s = new Mock<IKubernetes>();
                mock8s.Setup(k8s => k8s.BaseUri).Returns(new Uri($"https://{ix}"));
                mockMember.Setup(mm => mm.KubeApi).Returns(mock8s.Object);
                mockMember.Setup(mm => mm.ClusterName).Returns(ix.ToString());

                return mockMember.Object;
            });

            foreach (var member in mockMembers)
            {
                fleet.AddMember(member);
            }

            Assert.Equal(memberApiUri, fleet.GetMemberByName(memberId)?.KubeApi.BaseUri.ToString());
        }
    }
}
