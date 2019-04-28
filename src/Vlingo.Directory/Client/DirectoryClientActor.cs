// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading.Tasks;
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
        private PublisherAvailability _directory;
        private SocketChannelWriter _directoryChannel;
        private readonly IServiceDiscoveryInterest _interest;
        private RawMessage _registerService;
        private readonly MulticastSubscriber _subscriber;

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
                SelfAs<IScheduled<object>>(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(processingInterval));
        }
        
        //====================================
        // DirectoryClient
        //====================================
        
        public void Register(ServiceRegistrationInfo info)
        {
            var converted = RegisterService.As(Name.Of(info.Name), ServiceRegistrationInfo.Location.ToAddresses(info.Locations));
            _registerService = RawMessage.From(0, 0, converted.ToString());
        }

        public void Unregister(string serviceName)
        {
            _registerService = null;
            Task.Run(async () => await UnregisterServiceAsync(Name.Of(serviceName)));
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
                        ServiceRegistrationInfo.Location.From(serviceRegistered.Addresses)));
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
            _subscriber.ProbeChannel().Wait();

            Task.Run(async () => await RegisterServiceAsync());
        }
        
        //====================================
        // Stoppable
        //====================================

        public override void Stop()
        {
            _cancellable.Cancel();
            base.Stop();
        }
        //====================================
        // internal implementation
        //====================================

        private void ManageDirectoryChannel(string maybePublisherAvailability)
        {
            var publisherAvailability = PublisherAvailability.From(maybePublisherAvailability);

            if (publisherAvailability.IsValid)
            {
                if (!publisherAvailability.Equals(_directory))
                {
                    _directory = publisherAvailability;
                    _directoryChannel?.Close();
                    _directoryChannel = new SocketChannelWriter(_directory.ToAddress(), Logger);
                }
            }
        }

        private async Task RegisterServiceAsync()
        {
            if (_directoryChannel != null && _registerService != null)
            {
                var expected = _registerService.TotalLength;
                var actual = await _directoryChannel.Write(_registerService, _buffer);
                if (actual != expected)
                {
                    Logger.Log($"DIRECTORY CLIENT: Did not send full service registration message:  {_registerService.AsTextMessage()}");
                }
            }
        }

        private async Task UnregisterServiceAsync(Name serviceName)
        {
            if (_directoryChannel != null)
            {
                var unregister = UnregisterService.As(serviceName);
                var unregisterServiceMessage = RawMessage.From(0, 0, unregister.ToString());
                var expected = unregisterServiceMessage.TotalLength;
                var actual = await _directoryChannel.Write(unregisterServiceMessage, _buffer);
                if (actual != expected)
                {
                    Logger.Log($"DIRECTORY CLIENT: Did not send full service unregister message: {unregisterServiceMessage.AsTextMessage()}");
                }
            }
        }
    }
}