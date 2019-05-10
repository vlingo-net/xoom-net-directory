// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Vlingo.Cluster.Model.Application;
using Vlingo.Cluster.Model.Attribute;
using Vlingo.Wire.Fdx.Outbound;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Directory.Model
{
    public class DirectoryApplication : ClusterApplicationAdapter
    {
        private readonly IDirectoryService _directoryService;
        private readonly Node _localNode;
        private Boolean _leading;

        public DirectoryApplication(Node localNode)
        {
            _localNode = localNode;

            _directoryService = DirectoryServiceFactory.Instance(Stage, localNode);
        }
        
        //====================================
        // ClusterApplication
        //====================================
        
        public override void HandleApplicationMessage(RawMessage message, IApplicationOutboundStream responder)
        {
        }

        public override void InformAllLiveNodes(IEnumerable<Node> liveNodes, bool isHealthyCluster)
        {
        }

        public override void InformLeaderElected(Id leaderId, bool isHealthyCluster, bool isLocalNodeLeading)
        {
            Logger.Log($"DIRECTORY: Leader elected: {leaderId}");
     
            if (isLocalNodeLeading)
            {
                _leading = true;
                Logger.Log("DIRECTORY: Assigned leadership; starting processing.");
                _directoryService.AssignLeadership();
            }
            else
            {
                _leading = false;
                Logger.Log($"DIRECTORY: Remote node assigned leadership: {leaderId}");
      
                // prevent split brain in case another leader pushes in. if this node
                // is not currently leading this operation will have no harm.
                _directoryService.RelinquishLeadership();
            }
        }

        public override void InformLeaderLost(Id lostLeaderId, bool isHealthyCluster)
        {
            Logger.Log($"DIRECTORY: Leader lost: {lostLeaderId}");
     
            if (_localNode.Id.Equals(lostLeaderId))
            {
                _leading = false;
                _directoryService.RelinquishLeadership();
            }
        }

        public override void InformLocalNodeShutDown(Id nodeId)
        {
            Logger.Log($"DIRECTORY: Local node left cluster: {nodeId}; relinquishing leadership");
            _leading = false;
    
            // prevent split brain in case another leader pushes in. if this node
            // is not currently leading this operation will have no harm.
            _directoryService.RelinquishLeadership();
        }

        public override void InformLocalNodeStarted(Id nodeId)
        {
        }

        public override void InformNodeIsHealthy(Id nodeId, bool isHealthyCluster)
        {
        }

        public override void InformNodeJoinedCluster(Id nodeId, bool isHealthyCluster)
        {
        }

        public override void InformNodeLeftCluster(Id nodeId, bool isHealthyCluster)
        {
            if (_localNode.Id.Equals(nodeId))
            {
                Logger.Log($"DIRECTORY: Node left cluster: {nodeId}; relinquishing leadership");
                _leading = false;
      
                // prevent split brain in case another leader pushes in. if this node
                // is not currently leading this operation will have no harm.
                _directoryService.RelinquishLeadership();
            }
            else
            {
                var healthyMessage = isHealthyCluster ? "; cluster still healthy" : "; cluster not healthy";
                Logger.Log($"DIRECTORY: Node left cluster: {nodeId} {healthyMessage}");
            }
        }

        public override void InformQuorumAchieved()
        {
            if (_leading)
            {
                Logger.Log("DIRECTORY: Quorum reachieved; restarting processing.");
                _directoryService.AssignLeadership();
            }
            else
            {
                Logger.Log("DIRECTORY: Quorum achieved");
            }
        }

        public override void InformQuorumLost()
        {
            Logger.Log("DIRECTORY: Quorum lost; pausing processing.");
    
            if (_leading)
            {
                _directoryService.RelinquishLeadership();
            }
        }

        public override void InformAttributesClient(IAttributesProtocol client)
        {
            Logger.Log("DIRECTORY: Attributes Client received.");
     
            _directoryService.Use(client);
        }

        public override void InformAttributeSetCreated(string attributeSetName)
        {
        }

        public override void InformAttributeAdded(string attributeSetName, string attributeName)
        {
        }

        public override void InformAttributeRemoved(string attributeSetName, string attributeName)
        {
        }

        public override void InformAttributeSetRemoved(string attributeSetName)
        {
        }

        public override void InformAttributeReplaced(string attributeSetName, string attributeName)
        {
        }

        public override void Stop()
        {
            _directoryService.Stop();
            
            base.Stop();
        }
    }
}