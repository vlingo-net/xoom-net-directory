// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
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

namespace Vlingo.Directory.Tests.Client
{
    public class MockServiceDiscoveryInterest : IServiceDiscoveryInterest
    {
        private AccessSafely _access;

        public MockServiceDiscoveryInterest(string name)
        {
            Name = name;
            DiscoveredServices = new ConcurrentBag<ServiceRegistrationInfo>();
            ServicesSeen = new ConcurrentBag<string>();
            UnregisteredServices = new ConcurrentBag<string>();
        }
        
        public bool InterestedIn(string serviceName)
        {
            if (!ServicesSeen.Contains(serviceName))
            {
                ServicesSeen.Add(serviceName);
                _access?.WriteUsing("interestedIn", 1);
            }
            return true;
        }

        public void InformDiscovered(ServiceRegistrationInfo discoveredService)
        {
            if (!DiscoveredServices.Contains(discoveredService))
            {
                DiscoveredServices.Add(discoveredService);
                _access?.WriteUsing("informDiscovered", 1);
            }
        }

        public void InformUnregistered(string unregisteredServiceName)
        {
            if (!UnregisteredServices.Contains(unregisteredServiceName))
            {
                UnregisteredServices.Add(unregisteredServiceName);
                _access?.WriteUsing("informUnregistered", 1);
            }
        }

        public AccessSafely AfterCompleting(int times)
        {
            var interestedIn = new AtomicInteger(0);
            var informDiscovered = new AtomicInteger(0);
            var informUnregistered = new AtomicInteger(0);
            _access = AccessSafely.AfterCompleting(times)
                .WritingWith<int>("interestedIn", _ => interestedIn.IncrementAndGet())
                .ReadingWith("interestedIn", () => interestedIn.Get())
                .WritingWith<int>("informDiscovered", _ => informDiscovered.IncrementAndGet())
                .ReadingWith("informDiscovered", () => informDiscovered.Get())
                .WritingWith<int>("informUnregistered", _ => informUnregistered.IncrementAndGet())
                .ReadingWith("informUnregistered", () => informUnregistered.Get());

            return _access;
        }
        
        public string Name { get; }
        
        public ConcurrentBag<ServiceRegistrationInfo> DiscoveredServices { get; }
        
        public ConcurrentBag<string> ServicesSeen { get; }
        
        public ConcurrentBag<string> UnregisteredServices { get; }
    }
}