// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors;
using Vlingo.Cluster.Model.Attribute;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;

namespace Vlingo.Directory.Model
{
    public interface IDirectoryService : IStartable, IStoppable
    {
        void AssignLeadership();
        
        void RelinquishLeadership();
        
        void Use(IAttributesProtocol client);
    }

    public static class DirectoryServiceFactory
    {
        public static IDirectoryService Instance(Stage stage, Node localNode)
        {
            var network =
                new Network(
                    new Group(Properties.Instance.DirectoryGroupAddress(), Properties.Instance.DirectoryGroupPort()),
                    Properties.Instance.DirectoryIncomingPort());
            
            var maxMessageSize = Properties.Instance.DirectoryMessageBufferSize();
            
            var timing =
                new Timing(
                    Properties.Instance.DirectoryMessageProcessingInterval(),
                    Properties.Instance.DirectoryMessagePublishingInterval());
            
            var unpublishedNotifications = Properties.Instance.DirectoryUnregisteredServiceNotifications();
            
            var directoryService =
                Instance(
                    stage,
                    localNode,
                    network,
                    maxMessageSize,
                    timing,
                    unpublishedNotifications);

            return directoryService;
        }

        public static IDirectoryService Instance(
            Stage stage,
            Node localNode,
            Network network,
            int maxMessageSize,
            Timing timing,
            int unpublishedNotifications) =>
            stage.ActorFor<IDirectoryService>(
                () => new DirectoryServiceActor(localNode, network, maxMessageSize, timing, unpublishedNotifications), "vlingo-directory-service");
    }

    public class Timing
    {
        public Timing(int processingInterval, int publishingInterval)
        {
            ProcessingInterval = processingInterval;
            PublishingInterval = publishingInterval;
        }
            
        public int ProcessingInterval { get; }
            
        public int PublishingInterval { get; }
    }

    public class Network
    {
        public Network(Group publisherGroup, int incomingPort)
        {
            PublisherGroup = publisherGroup;
            IncomingPort = incomingPort;
        }
            
        public Group PublisherGroup { get; }
            
        public int IncomingPort { get; }
    }
}