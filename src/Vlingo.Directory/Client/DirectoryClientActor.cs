// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Directory.Model.Message;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using ICancellable = Vlingo.Common.ICancellable;

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

        public DirectoryClientActor(
            IServiceDiscoveryInterest interest,
            Group directoryPublisherGroup,
            int maxMessageSize,
            long processingInterval,
            int processingTimeout)
        {
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
                SelfAs<IScheduled<object>>(), null, TimeSpan.FromMilliseconds(1000L), TimeSpan.FromMilliseconds(processingInterval));
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
            Logger.Debug($"CLIENT - Consuming message {incoming}");
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
            Logger.Debug("CLIENT - Probing channel...");
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
        public void TestSetDirectoryAddress(Address testAddress)
        {
            _testAddress = testAddress;
        }
        
        protected override void BeforeRestart(Exception reason)
        {
            Logger.Debug($"CLIENT - Before restart: {reason.Message}", reason);
            base.BeforeRestart(reason);
        }

        protected override void AfterRestart(Exception reason)
        {
            base.AfterRestart(reason);
            Logger.Debug($"CLIENT - After restart: {reason.Message}", reason);
        }
        
        protected override void BeforeStart()
        {
            Logger.Debug("CLIENT - Before start");
            base.BeforeStart();
        }

        protected override void AfterStop()
        {
            Logger.Debug("CLIENT - After stop");
            base.AfterStop();
        }
        
        //====================================
        // internal implementation
        //====================================

        private void ManageDirectoryChannel(string maybePublisherAvailability)
        {
            var publisherAvailability = PublisherAvailability.From(maybePublisherAvailability);

            if (publisherAvailability.IsValid)
            {
                if (!publisherAvailability.Equals(_directory!))
                {
                    _directory = publisherAvailability;
                    Logger.Debug($"Closing directory channel {_directoryChannel} because received {maybePublisherAvailability}");
                    _directoryChannel?.Close();
                    _directoryChannel = new SocketChannelWriter(_testAddress ?? _directory.ToAddress(), Logger);
                }
            }
        }

        private void RegisterService()
        {
            if (_directoryChannel != null && _registerService != null)
            {
                var expected = _registerService.TotalLength;
                Logger.Debug($"CLIENT - Writing service registration {_registerService.AsTextMessage()}...");
                var actual = _directoryChannel.Write(_registerService, _buffer);
                if (actual != expected)
                {
                    Logger.Warn($"DIRECTORY CLIENT: Did not send full service registration message:  {_registerService.AsTextMessage()}. Actual - {actual}, Expected - {expected}");
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
                Logger.Debug($"CLIENT - Writing service unregistration {_registerService.AsTextMessage()}...");
                var actual = _directoryChannel.Write(unregisterServiceMessage, _buffer);
                if (actual != expected)
                {
                    Logger.Warn($"DIRECTORY CLIENT: Did not send full service unregister message: {unregisterServiceMessage.AsTextMessage()}. Actual - {actual}, Expected - {expected}");
                }
            }
        }
    }
}