// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Directory.Client;
using Vlingo.Directory.Model;
using Vlingo.Directory.Tests.Client;
using Vlingo.Wire.Multicast;
using Vlingo.Wire.Node;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Directory.Tests.Model
{
    public class DirectoryServiceTest : IDisposable
    {
        private static readonly Random Random = new Random();
        private static AtomicInteger _portToUse = new AtomicInteger(Random.Next(10_000, 50_000));
        
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

        [Fact]
        public void TestShouldInformInterest()
        {
            // _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());

            // directory assigned leadership
            _directory.Actor.AssignLeadership();

            var location = new Location("test-host", _portToUse.GetAndIncrement());
            var info = new ServiceRegistrationInfo("test-service", new List<Location> { location });

            var accessSafely = _interest1.AfterCompleting(1);
            _client1.Actor.Register(info);
            
            Pause();
            
            accessSafely.ReadFromExpecting("interestedIn", 1);

            Assert.NotEmpty(_interest1.ServicesSeen);
            Assert.Contains("test-service", _interest1.ServicesSeen);
            Assert.NotEmpty(_interest1.DiscoveredServices);
            Assert.Contains(info, _interest1.DiscoveredServices);
        }

        [Fact]
        public void TestShouldUnregister()
        {
            // _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());

            // directory assigned leadership
            _directory.Actor.AssignLeadership();

            var accessSafely1 = _interest1.AfterCompleting(3);
            var accessSafely2 = _interest2.AfterCompleting(3);
            var accessSafely3 = _interest3.AfterCompleting(3);

            var location1 = new Location("test-host1", _portToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", _portToUse.GetAndIncrement());
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", _portToUse.GetAndIncrement());
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

            Pause();
            
//            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));
            
            _client1.Actor.Unregister(info1.Name);
            
            Pause();
            
//            Assert.Equal(1, accessSafely1.ReadFromExpecting("informUnregistered", 1));
//            Assert.Equal(1, accessSafely2.ReadFromExpecting("informUnregistered", 1));
//            Assert.Equal(1, accessSafely3.ReadFromExpecting("informUnregistered", 1));

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

        [Fact]
        public void TestShouldNotInformInterest()
        {
            // _directory.Actor.Start();
            _directory.Actor.Use(new TestAttributesClient());

            // directory NOT assigned leadership
            _directory.Actor.RelinquishLeadership(); // actually never had leadership, but be explicit and prove no harm
            
            var location1 = new Location("test-host1", _portToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            Pause();
            
            Assert.Empty(_interest1.ServicesSeen);
            Assert.DoesNotContain("test-service", _interest1.ServicesSeen);
            Assert.Empty(_interest1.DiscoveredServices);
            Assert.DoesNotContain(info1, _interest1.DiscoveredServices);
        }

        [Fact]
        public void TestAlteredLeadership()
        {
            _directory.Actor.Use(new TestAttributesClient());
            
            // START directory assigned leadership
            _directory.Actor.AssignLeadership();

            var accessSafely1 = _interest1.AfterCompleting(3);
            var accessSafely2 = _interest2.AfterCompleting(3);
            var accessSafely3 = _interest3.AfterCompleting(3);
            
            var location1 = new Location("test-host1", _portToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", _portToUse.GetAndIncrement());
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", _portToUse.GetAndIncrement());
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

//            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));
//            
//            Assert.Equal(3, accessSafely1.ReadFromExpecting("informDiscovered", 3));
//            Assert.Equal(3, accessSafely2.ReadFromExpecting("informDiscovered", 3));
//            Assert.Equal(3, accessSafely3.ReadFromExpecting("informDiscovered", 3));
            
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

//            accessSafely1 = _interest1.AfterCompleting(6);
//            accessSafely2 = _interest2.AfterCompleting(6);
//            accessSafely3 = _interest3.AfterCompleting(6);
            
            Pause();
            
//            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
//            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));
//
//            Assert.Equal(3, accessSafely1.ReadFromExpecting("informDiscovered", 3));
//            Assert.Equal(3, accessSafely2.ReadFromExpecting("informDiscovered", 3));
//            Assert.Equal(3, accessSafely3.ReadFromExpecting("informDiscovered", 3));

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

        [Fact]
        public void TestRegisterDiscoverMultiple()
        {
            _directory.Actor.Use(new TestAttributesClient());
            _directory.Actor.AssignLeadership();
            
            var accessSafely1 = _interest1.AfterCompleting(3);
            var accessSafely2 = _interest2.AfterCompleting(3);
            var accessSafely3 = _interest3.AfterCompleting(3);

            var location1 = new Location("test-host1", _portToUse.GetAndIncrement());
            var info1 = new ServiceRegistrationInfo("test-service1", new List<Location> { location1 });
            _client1.Actor.Register(info1);

            var location2 = new Location("test-host2", _portToUse.GetAndIncrement());
            var info2 = new ServiceRegistrationInfo("test-service2", new List<Location> { location2 });
            _client2.Actor.Register(info2);

            var location3 = new Location("test-host3", _portToUse.GetAndIncrement());
            var info3 = new ServiceRegistrationInfo("test-service3", new List<Location> { location3 });
            _client3.Actor.Register(info3);

            Assert.Equal(3, accessSafely1.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("interestedIn", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("interestedIn", 3));
            
            Assert.Equal(3, accessSafely1.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely2.ReadFromExpecting("informDiscovered", 3));
            Assert.Equal(3, accessSafely3.ReadFromExpecting("informDiscovered", 3));

            Pause();

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

        public DirectoryServiceTest(ITestOutputHelper output)
        {
            _output = output;
            var converter = new Converter(output);
            if (!Debugger.IsAttached)
            {
                Console.SetOut(converter);
            }

            _testWorld = TestWorld.Start("test");

            var operationalPort = _portToUse.GetAndIncrement();
            var applicationPort = _portToUse.GetAndIncrement();
            _node = Node.With(Id.Of(1), Name.Of("node1"), Host.Of("localhost"), operationalPort, applicationPort);

            _group = new Group("237.37.37.1", operationalPort);

            var incomingPort = _portToUse.GetAndIncrement();
            _directory = _testWorld.ActorFor<IDirectoryService>(
                Definition.Has<DirectoryServiceActor>(
                    Definition.Parameters(_node, new Network(_group, incomingPort), 1024, new Timing(100, 100), 20)));

            _interest1 = new MockServiceDiscoveryInterest("interest1", output);

            _client1 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest1, _group, 1024, 100, 50)));

            _interest2 = new MockServiceDiscoveryInterest("interest2", output);

            _client2 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest2, _group, 1024, 100, 50)));

            _interest3 = new MockServiceDiscoveryInterest("interest3", output);

            _client3 = _testWorld.ActorFor<IDirectoryClient>(
                Definition.Has<DirectoryClientActor>(
                    Definition.Parameters(_interest3, _group, 1024, 100, 50)));

            var testAddress = Address.From(Host.Of("localhost"), incomingPort, AddressType.Main);
            ((DirectoryClientActor)_client1.ActorInside).TestSetDirectoryAddress(testAddress);
            ((DirectoryClientActor)_client2.ActorInside).TestSetDirectoryAddress(testAddress);
            ((DirectoryClientActor)_client3.ActorInside).TestSetDirectoryAddress(testAddress);

            _interests = new List<MockServiceDiscoveryInterest> { _interest1, _interest2, _interest3 };
        }

        public void Dispose()
        {
            _directory.Actor.Stop();
            _client1.Actor.Stop();
            _client2.Actor.Stop();
            _client3.Actor.Stop();
            _testWorld.Terminate();
        }

        private void Pause(int milliseconds = 1000)
        {
            Thread.Sleep(milliseconds);
        }
    }
}