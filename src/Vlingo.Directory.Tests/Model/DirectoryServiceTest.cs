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
using Vlingo.Actors;
using Vlingo.Actors.Plugin.Logging.Console;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Directory.Client;
using Vlingo.Directory.Model;
using Vlingo.Directory.Model.Message;
using Vlingo.Directory.Tests.Client;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Directory.Tests.Model
{
    public class DirectoryServiceTest : IDisposable
    {
        private static readonly Random Random = new Random();
        private static readonly AtomicInteger PortToUse = new AtomicInteger(Random.Next(10_000, 50_000));
        
        private readonly TestActor<IDirectoryClient> _client1;
        private readonly TestActor<IDirectoryClient> _client2;
        private readonly TestActor<IDirectoryClient> _client3;
        private readonly TestActor<IDirectoryService> _directory;
        private readonly MockServiceDiscoveryInterest _interest1;
        private readonly MockServiceDiscoveryInterest _interest2;
        private readonly MockServiceDiscoveryInterest _interest3;
        private readonly List<MockServiceDiscoveryInterest> _interests;
        private readonly TestWorld _testWorld;
        private readonly ITestOutputHelper _output;
        private Address _testAddress;

        [Fact(Skip = "Testing")]
        public void TestShouldInformInterest()
        {
            _directory.Actor.Use(new TestAttributesClient());

            // directory assigned leadership
            _directory.Actor.AssignLeadership();

            var location = new Location("test-host", PortToUse.GetAndIncrement());
            var info = new ServiceRegistrationInfo("test-service", new List<Location> { location });

            var accessSafely = _interest1.AfterCompleting(2);
            _client1.Actor.Register(info);
            
            accessSafely.ReadFromExpecting("interestedIn", 1);

            Assert.NotEmpty(_interest1.ServicesSeen);
            Assert.Contains("test-service", _interest1.ServicesSeen);
            Assert.NotEmpty(_interest1.DiscoveredServices);
            Assert.Contains(info, _interest1.DiscoveredServices);
        }

        [Fact(Skip = "Freezes")]
        public void TestShouldUnregister()
        {
            _directory.Actor.Use(new TestAttributesClient());

            // directory assigned leadership
            _directory.Actor.AssignLeadership();

            var accessSafely1 = _interest1.AfterCompleting(3);
            var accessSafely2 = _interest2.AfterCompleting(3);
            var accessSafely3 = _interest3.AfterCompleting(3);

            var location1 = new Location("test-host1", PortToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", PortToUse.GetAndIncrement());
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", PortToUse.GetAndIncrement());
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

            accessSafely1.ReadFromExpecting("interestedIn", 3);
            accessSafely2.ReadFromExpecting("interestedIn", 3);
            accessSafely3.ReadFromExpecting("interestedIn", 3);
            
            _client1.Actor.Unregister(info1.Name);
            
            accessSafely1.ReadFromExpecting("informUnregistered", 1);
            accessSafely2.ReadFromExpecting("informUnregistered", 1);
            accessSafely3.ReadFromExpecting("informUnregistered", 1);

            foreach (var interest in new List<MockServiceDiscoveryInterest> { _interest2, _interest3 })
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

        [Fact(Skip = "Testing")]
        public void TestShouldNotInformInterest()
        {
            _directory.Actor.Use(new TestAttributesClient());

            // directory NOT assigned leadership
            _directory.Actor.RelinquishLeadership(); // actually never had leadership, but be explicit and prove no harm
            
            var location1 = new Location("test-host1", PortToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            Assert.Empty(_interest1.ServicesSeen);
            Assert.DoesNotContain("test-service", _interest1.ServicesSeen);
            Assert.Empty(_interest1.DiscoveredServices);
            Assert.DoesNotContain(info1, _interest1.DiscoveredServices);
        }

        [Fact(Skip = "Freezes")]
        public void TestAlteredLeadership()
        {
            _directory.Actor.Use(new TestAttributesClient());
            
            // START directory assigned leadership
            _directory.Actor.AssignLeadership();

            var accessSafely1 = _interest1.AfterCompleting(3);
            var accessSafely2 = _interest2.AfterCompleting(3);
            var accessSafely3 = _interest3.AfterCompleting(3);
            
            var location1 = new Location("test-host1", PortToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", PortToUse.GetAndIncrement());
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", PortToUse.GetAndIncrement());
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));
            
            Assert.Equal(3, accessSafely1.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("informDiscovered", 3));
            
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

            foreach (var interest in _interests)
            {
                interest.ServicesSeen.Clear();
                interest.DiscoveredServices.Clear();
            }

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

            foreach (var interest in _interests)
            {
                interest.ServicesSeen.Clear();
                interest.DiscoveredServices.Clear();
            }
            
            accessSafely1 = _interest1.AfterCompleting(3);
            accessSafely2 = _interest2.AfterCompleting(3);
            accessSafely3 = _interest3.AfterCompleting(3);
            
            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));

            Assert.Equal(3, accessSafely1.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("informDiscovered", 3));

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

        [Fact(Skip = "Freezes")]
        public void TestRegisterDiscoverMultiple()
        {
            _directory.Actor.Use(new TestAttributesClient());
            _directory.Actor.AssignLeadership();

            var accessSafely1 = _interest1.AfterCompleting(6);
            var accessSafely2 = _interest2.AfterCompleting(6);
            var accessSafely3 = _interest3.AfterCompleting(6);

            var locationPort = PortToUse.GetAndIncrement();
            var location1 = new Location("test-host1", locationPort);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", locationPort);
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", locationPort);
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

            accessSafely1.ReadFromExpecting("interestedIn", 3);
            accessSafely2.ReadFromExpecting("interestedIn", 3);
            accessSafely3.ReadFromExpecting("interestedIn", 3);
            
            accessSafely1.ReadFromExpecting("informDiscovered", 3);
            accessSafely2.ReadFromExpecting("informDiscovered", 3);
            accessSafely3.ReadFromExpecting("informDiscovered", 3);

            foreach (var interest in _interests)
            {
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

        [Fact]
        public void SmokeTestForCi()
        {
            _directory.Actor.Use(new TestAttributesClient());
            _directory.Actor.AssignLeadership();
         
            var locationPort = PortToUse.GetAndIncrement();
            
            var writer1 = new SocketChannelWriter(_testAddress, ConsoleLogger.TestInstance());
            var location1 = new Location("test-host1", locationPort);
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            var converted1 = RegisterService.As(Name.Of(info1.Name), Location.ToAddresses(info1.Locations));
            var registerService1 = RawMessage.From(0, 0, converted1.ToString());
            var buffer1 = new MemoryStream(1024);
            
            var writer2 = new SocketChannelWriter(_testAddress, ConsoleLogger.TestInstance());
            var location2 = new Location("test-host2", locationPort);
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            var converted2 = RegisterService.As(Name.Of(info2.Name), Location.ToAddresses(info2.Locations));
            var registerService2 = RawMessage.From(0, 0, converted2.ToString());
            var buffer2 = new MemoryStream(1024);
            
            var writer3 = new SocketChannelWriter(_testAddress, ConsoleLogger.TestInstance());
            var location3 = new Location("test-host3", locationPort);
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            var converted3 = RegisterService.As(Name.Of(info3.Name), Location.ToAddresses(info3.Locations));
            var registerService3 = RawMessage.From(0, 0, converted3.ToString());
            var buffer3 = new MemoryStream(1024);

            var t1 = new Thread(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    Pause(50);
                    writer1.Write(registerService1, buffer1);   
                }
            });
            t1.Start();
            
            var t2 = new Thread(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    Pause(50);
                    writer2.Write(registerService2, buffer2);   
                }
            });
            t2.Start();
            
            var t3 = new Thread(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    Pause(50);
                    writer3.Write(registerService3, buffer3);   
                }
            });
            t3.Start();

            while (((DirectoryServiceActor)_directory.ActorInside).Consumed.Get().Count < 200)
            {
                Pause(10);
            }
            
            Assert.Equal(200, ((DirectoryServiceActor)_directory.ActorInside).Consumed.Get().Count);
        }
        
        public DirectoryServiceTest(ITestOutputHelper output)
        {
            _output = output;
            var converter = new Converter(output);
            if (!Debugger.IsAttached)
            {
                Console.SetOut(converter);
            }

            _testWorld = TestWorld.Start("test");

            var operationalPort = PortToUse.GetAndIncrement();
            var applicationPort = PortToUse.GetAndIncrement();
            var node = Node.With(Id.Of(1), Name.Of("node1"), Host.Of("localhost"), operationalPort, applicationPort);

            var @group = new Group("237.37.37.1", operationalPort);

            var incomingPort = PortToUse.GetAndIncrement();

            _directory = _testWorld.ActorFor<IDirectoryService>(
                Definition.Has<DirectoryServiceActor>(
                    Definition.Parameters(node, new Network(@group, incomingPort), 1024, new Timing(100, 100), 10)));
            
            _interest1 = new MockServiceDiscoveryInterest("interest1", output);

            /*_client1 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest1, @group, 1024, 50, 10)));

            _interest2 = new MockServiceDiscoveryInterest("interest2", output);

            _client2 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest2, @group, 1024, 50, 10)));

            _interest3 = new MockServiceDiscoveryInterest("interest3", output);

            _client3 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest3, @group, 1024, 50, 10)));*/

            _testAddress = Address.From(Host.Of("localhost"), incomingPort, AddressType.Main);
//            ((DirectoryClientActor)_client1.ActorInside).TestSetDirectoryAddress(_testAddress);
//            ((DirectoryClientActor)_client2.ActorInside).TestSetDirectoryAddress(_testAddress);
//            ((DirectoryClientActor)_client3.ActorInside).TestSetDirectoryAddress(_testAddress);

            _interests = new List<MockServiceDiscoveryInterest> { _interest1, _interest2, _interest3 };
        }

        public void Dispose()
        {
            _directory.Actor.Stop();
//            _client1.Actor.Stop();
//            _client2.Actor.Stop();
//            _client3.Actor.Stop();
            _testWorld.Terminate();
        }

        private void Pause(int milliseconds = 1000)
        {
            Thread.Sleep(milliseconds);
        }
    }
}