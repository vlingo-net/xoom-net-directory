// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Linq;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Actors.TestKit;
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Directory.Client;

namespace Vlingo.Xoom.Directory.Tests.Client
{
    public class MockServiceDiscoveryInterest : IServiceDiscoveryInterest
    {
        private readonly ILogger _logger;
        private AccessSafely _access;

        public MockServiceDiscoveryInterest(string name, ILogger logger)
        {
            _logger = logger;
            Name = name;
            DiscoveredServices = new ConcurrentBag<ServiceRegistrationInfo>();
            ServicesSeen = new ConcurrentBag<string>();
            UnregisteredServices = new ConcurrentBag<string>();
        }
        
        public bool InterestedIn(string serviceName)
        {
            if (!ServicesSeen.Contains(serviceName))
            {
                _logger.Debug($"Service seen: {serviceName}");
                ServicesSeen.Add(serviceName);
                _access?.WriteUsing("interestedIn", 1);
            }
            return true;
        }

        public void InformDiscovered(ServiceRegistrationInfo discoveredService)
        {
            if (!DiscoveredServices.Contains(discoveredService))
            {
                _logger.Debug($"Service discovered: {discoveredService}");
                DiscoveredServices.Add(discoveredService);
                _access?.WriteUsing("informDiscovered", 1);
            }
        }

        public void InformUnregistered(string unregisteredServiceName)
        {
            if (!UnregisteredServices.Contains(unregisteredServiceName))
            {
                _logger.Debug($"Service unregistered: {unregisteredServiceName}");
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