// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using Vlingo.Cluster.Model.Attribute;
using Vlingo.Xoom.Common;
using Vlingo.Directory.Model.Message;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Multicast;
using Vlingo.Xoom.Wire.Node;
using ICancellable = Vlingo.Xoom.Common.ICancellable;

namespace Vlingo.Directory.Model
{
    public class DirectoryServiceActor : Actor, IDirectoryService, IChannelReaderConsumer, IScheduled<IntervalType>
    {
        private const string ServiceNamePrefix = "RegisteredService:";
        private const string UnregisteredServiceNamePrefix = "UnregisteredService:";
        private const string UnregisteredCount = "COUNT";

        private ICancellable? _cancellableMessageProcessing;
        private ICancellable? _cancellablePublishing;
        private IAttributesProtocol? _attributesClient;
        private bool _leader;
        private readonly Node _localNode;
        private readonly int _maxMessageSize;
        private readonly Network _network;
        private MulticastPublisherReader? _publisher;
        private readonly Timing _timing;
        private readonly int _unpublishedNotifications;
        private bool _stopped;
        private readonly AtomicReference<List<string>> _consumed = new AtomicReference<List<string>>(new List<string>());

        public DirectoryServiceActor(
            Node localNode,
            Network network,
            int maxMessageSize,
            Timing timing,
            int unpublishedNotifications)
        {
            _localNode = localNode;
            _network = network;
            _maxMessageSize = maxMessageSize;
            _timing = timing;
            _unpublishedNotifications = unpublishedNotifications;
            _stopped = false;
        }
        
        //=========================================
        // DirectoryService
        //=========================================
        
        public void AssignLeadership()
        {
            _leader = true;
            StartProcessing();
        }

        public void RelinquishLeadership()
        {
            _leader = false;
            StopProcessing();
        }

        public void Use(IAttributesProtocol client) => _attributesClient = client;
        
        //=========================================
        // Scheduled
        //=========================================
        
        public void IntervalSignal(IScheduled<IntervalType> scheduled, IntervalType data)
        {
            if (_stopped)
            {
                return;
            }
            
            if (!_leader)
            {
                return;
            }

            switch (data)
            {
                case IntervalType.Processing:
                    _publisher?.ProcessChannel();
                    break;
                case IntervalType.Publishing:
                    _publisher?.SendAvailability();
                    PublishAllServices();
                    break;
            }
        }
        
        //=========================================
        // Startable
        //=========================================

        public override void Start()
        {
            Logger.Info("DIRECTORY: Starting...");
            Logger.Info("DIRECTORY: Waiting to gain leadership...");
            
            base.Start();
        }
        
        //=========================================
        // Stoppable
        //=========================================

        public override void Stop()
        {
            Logger.Info($"DIRECTORY: stopping on node: {_localNode}");
    
            StopProcessing();
            
            base.Stop();
        }
        
        //====================================
        // ChannelReaderConsumer
        //====================================

        public void Consume(RawMessage message)
        {
            var incoming = message.AsTextMessage();
            _consumed.Get()?.Add(incoming);

            var registerService = RegisterService.From(incoming);
            if (registerService.IsValid)
            {
                var attributeSetName = ServiceNamePrefix + registerService.Name.Value;
                foreach (var address in registerService.Addresses)
                {
                    var fullAddress = address.Full;
                    _attributesClient?.Add(attributeSetName, fullAddress, fullAddress);
                }
            }
            else
            {
                var unregisterService = UnregisterService.From(incoming);
                if (unregisterService.IsValid)
                {
                    var attributeSetName = ServiceNamePrefix + unregisterService.Name.Value;
                    _attributesClient?.RemoveAll(attributeSetName);
                    _attributesClient?.Add(UnregisteredServiceNamePrefix + unregisterService.Name.Value, UnregisteredCount, _unpublishedNotifications);
                }
                else
                {
                    Logger.Warn($"DIRECTORY: RECEIVED UNKNOWN: {incoming}");
                }
            }
        }

        //====================================
        // internal implementation
        //====================================
        
        private Name Named(string prefix, string serviceName) => new Name(serviceName.Substring(prefix.Length));

        private void PublishAllServices()
        {
            foreach (var set in _attributesClient!.All.ToList())
            {
                if (set.Name!.StartsWith(ServiceNamePrefix))
                {
                    PublishService(set.Name);
                }
                else if (set.Name.StartsWith(UnregisteredServiceNamePrefix))
                {
                    UnpublishService(set.Name);
                }
            }
        }

        private void PublishService(string name)
        {
            var addresses = new List<Address>();
            foreach (var attribute in _attributesClient!.AllOf(name))
            {
                addresses.Add(Xoom.Wire.Node.Address.From(attribute.ToStringValue()!, AddressType.Main));
            }
            _publisher?.Send(RawMessage.From(0, 0, ServiceRegistered.As(Named(ServiceNamePrefix, name), addresses).ToString()));
        }

        private void UnpublishService(string name)
        {
            _publisher?.Send(RawMessage.From(0, 0, ServiceUnregistered.As(Named(UnregisteredServiceNamePrefix, name)).ToString()));
    
            var unregisteredNotificationsCount = _attributesClient!.Attribute<int>(name, UnregisteredCount);
            var count = unregisteredNotificationsCount.Value - 1;
            if (count - 1 <= 0)
            {
                _attributesClient.RemoveAll(name);
            }
            else
            {
                _attributesClient.Replace(name, UnregisteredCount, count);
            }
        }

        private void StartProcessing()
        {
            _stopped = false;
            if (_publisher == null)
            {
                try
                {
                    Logger.Debug("DIRECTORY: initializing 'vlingo-directory-service'");
                    _publisher = new MulticastPublisherReader(
                        "vlingo-directory-service",
                        _network.PublisherGroup,
                        _network.IncomingPort,
                        _maxMessageSize,
                        SelfAs<IChannelReaderConsumer>(),
                        Logger);
                }
                catch (Exception e)
                {
                    var message = $"DIRECTORY: Failed to create multicast publisher/reader because: {e.Message}";
                    Logger.Error(message, e);
                    throw new InvalidOperationException(message, e);
                }
            }

            if (_cancellableMessageProcessing == null)
            {
                _cancellableMessageProcessing = Stage.Scheduler.Schedule(
                    SelfAs<IScheduled<IntervalType>>(),
                    IntervalType.Processing,
                    TimeSpan.Zero, 
                    TimeSpan.FromMilliseconds(_timing.ProcessingInterval));
            }

            if (_cancellablePublishing == null)
            {
                _cancellablePublishing = Stage.Scheduler.Schedule(
                    SelfAs<IScheduled<IntervalType>>(),
                    IntervalType.Publishing,
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(_timing.PublishingInterval));
            }
        }

        private void StopProcessing()
        {
            _stopped = true;
            if (_publisher != null)
            {
                try
                {
                    _publisher.Close();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _publisher = null;
                }
            }

            if (_cancellableMessageProcessing != null)
            {
                try
                {
                    _cancellableMessageProcessing.Cancel();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _cancellableMessageProcessing = null;
                }
            }
    
            if (_cancellablePublishing != null)
            {
                try
                {
                    _cancellablePublishing.Cancel();
                }
                catch 
                {
                    // ignore
                }
                finally
                {
                    _cancellablePublishing = null;
                }
            }
        }
    }
}