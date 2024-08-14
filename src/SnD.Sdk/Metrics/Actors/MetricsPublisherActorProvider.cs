using System;
using Akka.Actor;

namespace SnD.Sdk.Metrics.Actors;

/// <summary>
/// Factory for creating metrics publisher actor instances.
/// </summary>
public static class MetricsPublisherActorProvider
{
    /// <summary>
    /// Start a new instance of the metrics publisher actor.
    /// </summary>
    /// <param name="actorSystem">Actor system used to control the actor.</param>
    /// <param name="factory">Factory method for creating the actor instance.</param>
    /// <param name="name">Optional Actor name (can be omitted if application requires the singleton actor).
    /// In this case the name of the class <see cref="MetricsPublisherActor"/> will be used.</param>
    /// <typeparam name="TActorType">The concrete class name for the actor.</typeparam>
    /// <returns>Actor reference.</returns>
    public static IActorRef StartMetricsPublisher<TActorType>(this IActorRefFactory actorSystem, Func<TActorType> factory, string name = null)
        where TActorType: MetricsPublisherActor
    {
        return actorSystem.ActorOf(Props.Create(() => factory()), name ?? nameof(MetricsPublisherActor));
    }
}
