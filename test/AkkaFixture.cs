﻿using Akka.Actor;
using Akka.Streams;

namespace Snd.Sdk.Tests
{
    public class AkkaFixture
    {
        public ActorSystem ActorSystem { get; }
        public IMaterializer Materializer { get; }

        public AkkaFixture()
        {
            this.ActorSystem = ActorSystem.Create(nameof(AkkaFixture));
            this.Materializer = this.ActorSystem.Materializer();
        }
    }
}
