// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Directory.Client;

namespace Vlingo.Directory.Tests.Client
{
    public class MockServiceDiscoveryInterest : IServiceDiscoveryInterest
    {
        private AccessSafely _access;
        
        public AtomicInteger _interestedIn = new AtomicInteger(0);
        public AtomicInteger _informDiscovered = new AtomicInteger(0);
        public AtomicInteger _informUnregistered = new AtomicInteger(0);

        public MockServiceDiscoveryInterest(string name)
        {
            Name = name;
            DiscoveredServices = new List<ServiceRegistrationInfo>();
            ServicesSeen = new List<string>();
            UnregisteredServices = new List<string>();
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
        
        public List<ServiceRegistrationInfo> DiscoveredServices { get; }
        
        public List<string> ServicesSeen { get; }
        
        public List<string> UnregisteredServices { get; }
    }
}