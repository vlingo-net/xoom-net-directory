// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vlingo.Actors;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
using Vlingo.Directory.Client;
using Vlingo.Directory.Model;
using Vlingo.Directory.Tests.Client;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Fdx.Inbound;
using Vlingo.Wire.Message;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Vlingo.Directory.Tests.Model
{
    public class DirectoryServiceTest : IDisposable
    {
        private readonly TestActor<IDirectoryClient> _client1;
        private readonly TestActor<IDirectoryClient> _client2;
        private readonly TestActor<IDirectoryClient> _client3;
        private readonly TestActor<IDirectoryService> _directory;
        private Group _group;
        private readonly MockServiceDiscoveryInterest _interest1;
        private readonly MockServiceDiscoveryInterest _interest2;
        private readonly MockServiceDiscoveryInterest _interest3;
        private readonly List<MockServiceDiscoveryInterest> _interests;
        private Node _node;
        private readonly TestWorld _testWorld;
        private readonly ITestOutputHelper _output;
        
        private SocketChannelWriter _channelWriter;
        private IChannelReader _channelReader;
        private static readonly string TestMessage = "TEST ";
        
        [Fact]
        public async Task TestChannelWriter()
        {
            var consumer = new MockChannelReaderConsumer();
            
            _channelReader.OpenFor(consumer);
            
            var buffer = new MemoryStream(1024);
            buffer.SetLength(1024);
            
            var message1 = TestMessage + 1;
            var rawMessage1 = RawMessage.From(0, 0, message1);
            await _channelWriter.Write(rawMessage1, buffer);
            
            await ProbeUntilConsumed(_channelReader, consumer);
            
            Assert.Equal(1, consumer.ConsumeCount);
            Assert.Equal(message1, consumer.Messages.First());
            
            var message2 = TestMessage + 2;
            var rawMessage2 = RawMessage.From(0, 0, message2);
            await _channelWriter.Write(rawMessage2, buffer);
            
            await ProbeUntilConsumed(_channelReader, consumer);
            
            Assert.Equal(2, consumer.ConsumeCount);
            Assert.Equal(message2, consumer.Messages.Last());
            
        }

        [Fact(Skip = "AppVeyor not finishing")]
        public void TestShouldInformInterest()
        {
            _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());

            // directory assigned leadership
            _directory.Actor.AssignLeadership();
    
            var location = new Location("test-host", 1234);
            var info = new ServiceRegistrationInfo("test-service", new List<Location> {location});
    
            MockServiceDiscoveryInterest.InterestsSeen = TestUntil.Happenings(6);
            _client1.Actor.Register(info);
            MockServiceDiscoveryInterest.InterestsSeen.Completes();
    
            Assert.NotEmpty(_interest1.ServicesSeen);
            Assert.Contains("test-service", _interest1.ServicesSeen);
            Assert.NotEmpty(_interest1.DiscoveredServices);
            Assert.Contains(info, _interest1.DiscoveredServices);
        }

        [Fact(Skip = "AppVeyor not finishing")]
        public void TestShouldUnregister()
        {
            _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());
    
            // directory assigned leadership
            _directory.Actor.AssignLeadership();
    
            var location1 = new Location("test-host1", 1234);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> {location1});
            _client1.Actor.Register(info1);
    
            var location2 = new Location("test-host2", 1234);
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> {location2});
            _client2.Actor.Register(info2);
    
            var location3 = new Location("test-host3", 1234);
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> {location3});
            _client3.Actor.Register(info3);
            Pause();
            
            _client1.Actor.Unregister(info1.Name);
            Pause();

            foreach (var interest in new List<MockServiceDiscoveryInterest> {_interest2, _interest3})
            {
                _output.WriteLine($"COUNT: {interest.ServicesSeen.Count + interest.DiscoveredServices.Count + interest.UnregisteredServices.Count}");
                var discoveredServices = interest.DiscoveredServices.ToList();
                Assert.NotEmpty(interest.ServicesSeen);
                Assert.Contains(info1.Name, interest.ServicesSeen);
                Assert.NotEmpty(discoveredServices);
                Assert.Contains(info1, discoveredServices);
                Assert.NotEmpty(interest.UnregisteredServices);
                foreach (var unregisteredService in interest.UnregisteredServices)
                {
                    _output.WriteLine(unregisteredService);
                }
                Assert.Contains(info1.Name, interest.UnregisteredServices);
            }
        }

        [Fact(Skip = "AppVeyor not finishing")]
        public void TestShouldNotInformInterest()
        {
            _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());
    
            // directory NOT assigned leadership
            _directory.Actor.RelinquishLeadership(); // actually never had leadership, but be explicit and prove no harm
    
            var location1 = new Location("test-host1", 1234);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> {location1});
            _client1.Actor.Register(info1);
            
            Pause();
    
            Assert.Empty(_interest1.ServicesSeen);
            Assert.DoesNotContain("test-service", _interest1.ServicesSeen);
            Assert.Empty(_interest1.DiscoveredServices);
            Assert.DoesNotContain(info1, _interest1.DiscoveredServices);
        }

        [Fact(Skip = "AppVeyor not finishing")]
        public void TestAlteredLeadership()
        {
            _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());
    
            // START directory assigned leadership
            _directory.Actor.AssignLeadership();
    
            var location1 = new Location("test-host1", 1234);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> {location1});
            _client1.Actor.Register(info1);
    
            var location2 = new Location("test-host2", 1234);
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> {location2});
            _client2.Actor.Register(info2);
    
            var location3 = new Location("test-host3", 1234);
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> {location3});
            _client3.Actor.Register(info3);
            
            Pause();

            foreach (var interest in _interests)
            {
                var discoveredServices = interest.DiscoveredServices.ToList();
                Assert.NotEmpty(interest.ServicesSeen);
                Assert.Contains("test-service1", interest.ServicesSeen);
                Assert.Contains("test-service2", interest.ServicesSeen);
                Assert.Contains("test-service3", interest.ServicesSeen);
                Assert.NotEmpty(discoveredServices);
                Assert.Contains(info1, discoveredServices);
                Assert.Contains(info2, discoveredServices);
                Assert.Contains(info3, discoveredServices);
            }
            
            // ALTER directory relinquished leadership
            _directory.Actor.RelinquishLeadership(); 
            Pause();

            foreach (var interest in _interests)
            {
                interest.ServicesSeen.Clear();
                interest.DiscoveredServices.Clear();
            }
            
            Pause();

            foreach (var interest in _interests)
            {
                Assert.Empty(interest.ServicesSeen);
                Assert.DoesNotContain("test-service1", interest.ServicesSeen);
                Assert.DoesNotContain("test-service2", interest.ServicesSeen);
                Assert.DoesNotContain("test-service3", interest.ServicesSeen);
                Assert.Empty(interest.DiscoveredServices);
                Assert.DoesNotContain(info1, interest.DiscoveredServices);
                Assert.DoesNotContain(info2, interest.DiscoveredServices);
                Assert.DoesNotContain(info3, interest.DiscoveredServices);
            }
            
            // ALTER directory assigned leadership
            _directory.Actor.AssignLeadership(); 
            Pause();

            foreach (var interest in _interests)
            {
                interest.ServicesSeen.Clear();
                interest.DiscoveredServices.Clear();
            }
            
            Pause();
            
            foreach (var interest in _interests)
            {
                Assert.NotEmpty(interest.ServicesSeen);
                Assert.Contains("test-service1", interest.ServicesSeen);
                Assert.Contains("test-service2", interest.ServicesSeen);
                Assert.Contains("test-service3", interest.ServicesSeen);
                Assert.NotEmpty(interest.DiscoveredServices);
                Assert.Contains(info1, interest.DiscoveredServices);
                Assert.Contains(info2, interest.DiscoveredServices);
                Assert.Contains(info3, interest.DiscoveredServices);
            }
        }

        [Fact(Skip = "AppVeyor not finishing")]
        public void TestRegisterDiscoverMutiple()
        {
            _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());
            _directory.Actor.AssignLeadership();
    
            var location1 = new Location("test-host1", 1234);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> {location1});
            _client1.Actor.Register(info1);
    
            var location2 = new Location("test-host2", 1234);
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> {location2});
            _client2.Actor.Register(info2);
    
            var location3 = new Location("test-host3", 1234);
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> {location3});
            _client3.Actor.Register(info3);
    
            Pause();
    
            foreach (var interest in _interests) {
                Assert.NotNull(interest.ServicesSeen);
                Assert.Contains("test-service1", interest.ServicesSeen);
                Assert.Contains("test-service2", interest.ServicesSeen);
                Assert.Contains("test-service3", interest.ServicesSeen);
                Assert.NotEmpty(interest.DiscoveredServices);
                Assert.Contains(info1, interest.DiscoveredServices);
                Assert.Contains(info2, interest.DiscoveredServices);
                Assert.Contains(info3, interest.DiscoveredServices);
            }
        }
        
        public DirectoryServiceTest(ITestOutputHelper output)
        {
            _output = output;
            var converter = new Converter(output);
            if (!Debugger.IsAttached)
            {
                Console.SetOut(converter);
            }
            
            var node = Node.With(Id.Of(2), Name.Of("node2"), Host.Of("0.0.0.0"), 37377, 37378);
            var logger = ConsoleLogger.TestInstance();
            _channelWriter = new SocketChannelWriter(node.OperationalAddress, logger);
            _channelReader = new SocketChannelInboundReader(node.OperationalAddress.Port, "test-reader", 1024, logger);
            
            _testWorld = TestWorld.Start("test");
    
            _node = Node.With(Id.Of(1), Name.Of("node1"), Host.Of("localhost"), 37371, 37372);
    
            _group = new Group("237.37.37.1", 37371);
    
            _directory = _testWorld.ActorFor<IDirectoryService>(
                Definition.Has<DirectoryServiceActor>(
                    Definition.Parameters(_node, new Network(_group, 37399), 1024, new Timing(100, 100), 20)));
    
            _interest1 = new MockServiceDiscoveryInterest("interest1");
    
            _client1 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest1, _group, 1024, 50, 10)));
    
            _interest2 = new MockServiceDiscoveryInterest("interest2");
    
            _client2 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest2, _group, 1024, 50, 10)));
    
            _interest3 = new MockServiceDiscoveryInterest("interest3");
    
            _client3 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest3, _group, 1024, 50, 10)));
    
            _interests = new List<MockServiceDiscoveryInterest> {_interest1, _interest2, _interest3};
        }

        public void Dispose()
        {
            _directory.Actor.Stop();
            _client1.Actor.Stop();
            _client2.Actor.Stop();
            _client3.Actor.Stop();
            _testWorld.Terminate();
        }

        private void Pause()
        {
            Pause(1000);
        }

        private void Pause(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
        
        private async Task ProbeUntilConsumed(IChannelReader reader, MockChannelReaderConsumer consumer)
        {
            var currentConsumedCount = consumer.ConsumeCount;
    
            for (int idx = 0; idx < 100; ++idx)
            {
                await reader.ProbeChannel();

                if (consumer.ConsumeCount > currentConsumedCount)
                {
                    break;
                }
            }
        }
    }
}