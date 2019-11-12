// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Directory.Client;

namespace Vlingo.Directory.Tests.Client
{
    public class MockServiceDiscoveryInterest : IServiceDiscoveryInterest
    {
        public static AccessSafely InterestsSeen;

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
                InterestsSeen?.WriteUsing("interest", 1);
            }
            return true;
        }

        public void InformDiscovered(ServiceRegistrationInfo discoveredService)
        {
            if (!DiscoveredServices.Contains(discoveredService))
            {
                DiscoveredServices.Add(discoveredService);
                InterestsSeen?.WriteUsing("interest", 1);
            }
        }

        public void InformUnregistered(string unregisteredServiceName)
        {
            if (!UnregisteredServices.Contains(unregisteredServiceName))
            {
                UnregisteredServices.Add(unregisteredServiceName);
                InterestsSeen?.WriteUsing("interest", 1);
            }
        }
        
        public string Name { get; }
        
        public List<ServiceRegistrationInfo> DiscoveredServices { get; }
        
        public List<string> ServicesSeen { get; }
        
        public List<string> UnregisteredServices { get; }
    }
}