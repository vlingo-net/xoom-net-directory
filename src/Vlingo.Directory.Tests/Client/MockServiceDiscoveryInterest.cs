// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Linq;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Directory.Client;
using Xunit.Abstractions;

namespace Vlingo.Directory.Tests.Client
{
    public class MockServiceDiscoveryInterest : IServiceDiscoveryInterest
    {
        private readonly ITestOutputHelper _output;
        private AccessSafely _access;

        private AtomicInteger _interestedIn;
        private AtomicInteger _informDiscovered;
        private AtomicInteger _informUnregistered;

        public MockServiceDiscoveryInterest(string name, ITestOutputHelper output)
        {
            _output = output;
            Name = name;
            DiscoveredServices = new ConcurrentBag<ServiceRegistrationInfo>();
            ServicesSeen = new ConcurrentBag<string>();
            UnregisteredServices = new ConcurrentBag<string>();
        }
        
        public bool InterestedIn(string serviceName)
        {
            _output.WriteLine($"InterestedIn {Name} comes in: {serviceName}");
            if (!ServicesSeen.Contains(serviceName))
            {
                _output.WriteLine($"InterestedIn {Name} registering: {serviceName}");
                ServicesSeen.Add(serviceName);
                _access?.WriteUsing("interestedIn", 1);
            }
            return true;
        }

        public void InformDiscovered(ServiceRegistrationInfo discoveredService)
        {
            _output.WriteLine($"InformDiscovered {Name} comes in: {discoveredService}");
            if (!DiscoveredServices.Contains(discoveredService))
            {
                _output.WriteLine($"InformDiscovered {Name} registering: {discoveredService}");
                DiscoveredServices.Add(discoveredService);
                _access?.WriteUsing("informDiscovered", 1);
            }
        }

        public void InformUnregistered(string unregisteredServiceName)
        {
            _output.WriteLine($"InformUnregistered {Name} comes in: {unregisteredServiceName}");
            if (!UnregisteredServices.Contains(unregisteredServiceName))
            {
                _output.WriteLine($"InformUnregistered {Name} unregistering: {unregisteredServiceName}");
                UnregisteredServices.Add(unregisteredServiceName);
                _access?.WriteUsing("informUnregistered", 1);
            }
        }

        public AccessSafely AfterCompleting(int times)
        {
            _interestedIn = new AtomicInteger(0);
            _informDiscovered = new AtomicInteger(0);
            _informUnregistered = new AtomicInteger(0);
            _access = AccessSafely.AfterCompleting(times)
                .WritingWith<int>("interestedIn", value => _interestedIn.AddAndGet(value))
                .ReadingWith("interestedIn", () => _interestedIn.Get())
                .WritingWith<int>("informDiscovered", value => _informDiscovered.AddAndGet(value))
                .ReadingWith("informDiscovered", () => _informDiscovered.Get())
                .WritingWith<int>("informUnregistered", value => _informUnregistered.AddAndGet(value))
                .ReadingWith("informUnregistered", () => _informUnregistered.Get());

            return _access;
        }
        
        public string Name { get; }
        
        public ConcurrentBag<ServiceRegistrationInfo> DiscoveredServices { get; }
        
        public ConcurrentBag<string> ServicesSeen { get; }
        
        public ConcurrentBag<string> UnregisteredServices { get; }
    }
}