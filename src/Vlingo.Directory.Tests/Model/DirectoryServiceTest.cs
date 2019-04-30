// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Vlingo.Actors;
using Vlingo.Actors.TestKit;
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
        private TestActor<IDirectoryClient> _client1;
        private TestActor<IDirectoryClient> _client2;
        private TestActor<IDirectoryClient> _client3;
        private TestActor<IDirectoryService> _directory;
        private Group _group;
        private MockServiceDiscoveryInterest _interest1;
        private MockServiceDiscoveryInterest _interest2;
        private MockServiceDiscoveryInterest _interest3;
        private List<MockServiceDiscoveryInterest> _interests;
        private Node _node;
        private TestWorld _testWorld;

        [Fact(Skip = "Waiting to fix proxy generator in vlingo.actors")]
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
    
            Assert.Empty(_interest1.ServicesSeen);
            Assert.Contains("test-service", _interest1.ServicesSeen);
            Assert.Empty(_interest1.DiscoveredServices);
            Assert.Contains(info, _interest1.DiscoveredServices);
        }
        
        public DirectoryServiceTest(ITestOutputHelper output)
        {
            var converter = new Converter(output);
            Console.SetOut(converter);
            
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
    }
}