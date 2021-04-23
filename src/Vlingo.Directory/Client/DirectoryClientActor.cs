// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Xoom.Common;
using Vlingo.Directory.Model.Message;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using Vlingo.Xoom.Actors;
using ICancellable = Vlingo.Xoom.Common.ICancellable;

namespace Vlingo.Directory.Client
{
    public sealed class DirectoryClientActor : Actor, IDirectoryClient, IChannelReaderConsumer, IScheduled<object>
    {
        private readonly MemoryStream _buffer;
        private readonly ICancellable _cancellable;
        private PublisherAvailability? _directory;
        private SocketChannelWriter? _directoryChannel;
        private readonly IServiceDiscoveryInterest _interest;
        private RawMessage? _registerService;
        private readonly MulticastSubscriber _subscriber;
        private Address? _testAddress;
        private readonly int _id;

        public DirectoryClientActor(
            IServiceDiscoveryInterest interest,
            Group directoryPublisherGroup,
            int maxMessageSize,
            long processingInterval,
            int processingTimeout)
        {
            _id = new Random().Next(1, 1000);
            _interest = interest;
            _buffer = new MemoryStream(maxMessageSize);
            _subscriber = new MulticastSubscriber(
                DirectoryClientFactory.ClientName,
                directoryPublisherGroup,
                maxMessageSize,
                processingTimeout,
                Logger);
            _subscriber.OpenFor(SelfAs<IChannelReaderConsumer>());
            _cancellable = Stage.Scheduler.Schedule(
                SelfAs<IScheduled<object?>>(), null, TimeSpan.FromMilliseconds(1000L), TimeSpan.FromMilliseconds(processingInterval));
        }
        
        //====================================
        // DirectoryClient
        //====================================
        
        public void Register(ServiceRegistrationInfo info)
        {
            var converted = Model.Message.RegisterService.As(Name.Of(info.Name), Location.ToAddresses(info.Locations));
            _registerService = RawMessage.From(0, 0, converted.ToString());
        }

        public void Unregister(string serviceName)
        {
            _registerService = null;
            UnregisterService(Name.Of(serviceName));
        }
        
        //====================================
        // ChannelReaderConsumer
        //====================================

        public void Consume(RawMessage message)
        {
            var incoming = message.AsTextMessage();
            var serviceRegistered = ServiceRegistered.From(incoming);

            if (serviceRegistered.IsValid && _interest.InterestedIn(serviceRegistered.Name.Value))
            {
                _interest.InformDiscovered(
                    new ServiceRegistrationInfo(serviceRegistered.Name.Value,
                        Location.From(serviceRegistered.Addresses)));
            }
            else
            {
                var serviceUnregistered = ServiceUnregistered.From(incoming);

                if (serviceUnregistered.IsValid && _interest.InterestedIn(serviceUnregistered.Name.Value))
                {
                    _interest.InformUnregistered(serviceUnregistered.Name.Value);
                }
                else
                {
                    ManageDirectoryChannel(incoming);
                }
            }
        }
        
        //====================================
        // Scheduled
        //====================================

        public void IntervalSignal(IScheduled<object> scheduled, object data)
        {
            _subscriber.ProbeChannel();
            RegisterService();   
        }
        
        //====================================
        // Stoppable
        //====================================

        public override void Stop()
        {
            _cancellable.Cancel();
            _subscriber.Close();
            _buffer.Dispose();
            base.Stop();
        }
        
        //====================================
        // Unit testing
        //====================================
        public void TestSetDirectoryAddress(Address testAddress) => _testAddress = testAddress;

        //====================================
        // internal implementation
        //====================================
        private void PrepareDirectoryChannel()
        {
            if (_directory != null)
            {
                _directoryChannel?.Close();
                _directoryChannel = new SocketChannelWriter(_testAddress ?? _directory.ToAddress(), Logger);   
            }
            else
            {
                Logger.Debug($"DIRECTORY CLIENT [{_id}]: Cannot initialize the socket because directory is null.");
            }
        }
        
        private void ManageDirectoryChannel(string maybePublisherAvailability)
        {
            var publisherAvailability = PublisherAvailability.From(maybePublisherAvailability);

            if (publisherAvailability.IsValid)
            {
                if (!publisherAvailability.Equals(_directory!))
                {
                    Logger.Debug($"DIRECTORY CLIENT [{_id}]: Initializing socket...");
                    _directory = publisherAvailability;
                    PrepareDirectoryChannel();
                }
            }
        }

        private void RegisterService()
        {
            if (_directoryChannel != null && _registerService != null)
            {
                var expected = _registerService.TotalLength;
                var actual = _directoryChannel.Write(_registerService, _buffer);
                if (actual != expected)
                {
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Did not send full service registration message:  {_registerService.AsTextMessage()}. Actual - {actual}, Expected - {expected}");
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Channel state: {_directoryChannel}");
                }

                if (_directoryChannel.IsBroken)
                {
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Channel is broken. Preparing a new one");
                    PrepareDirectoryChannel();
                }
            }
        }

        private void UnregisterService(Name serviceName)
        {
            if (_directoryChannel != null)
            {
                var unregister = Model.Message.UnregisterService.As(serviceName);
                var unregisterServiceMessage = RawMessage.From(0, 0, unregister.ToString());
                var expected = unregisterServiceMessage.TotalLength;
                var actual = _directoryChannel.Write(unregisterServiceMessage, _buffer);
                if (actual != expected)
                {
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Did not send full service unregister message: {unregisterServiceMessage.AsTextMessage()}. Actual - {actual}, Expected - {expected}");
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Channel state: {_directoryChannel}");
                }
                
                if (_directoryChannel.IsBroken)
                {
                    Logger.Warn($"DIRECTORY CLIENT [{_id}]: Channel is broken. Preparing a new one");
                    PrepareDirectoryChannel();
                }
            }
        }
    }
}