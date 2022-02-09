// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using Vlingo.Xoom.Directory.Client;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;

namespace Vlingo.Xoom.Directory.Tests.Client
{
    public class ServiceRegistrationInfoTest
    {
        [Fact]
        public void TestInfo()
        {
            var info = new ServiceRegistrationInfo(
                "test-service", 
                new []
                {
                    new Location("1.2.3.4", 111),
                    new Location("1.2.3.45", 222), 
                });
            
            Assert.Equal("test-service", info.Name);
            Assert.Equal(2, info.Locations.Count());

            var locations = info.Locations.ToList();
            
            Assert.Equal(new Location("1.2.3.4", 111), locations[0]);
            Assert.Equal(new Location("1.2.3.45", 222), locations[1]);
            
            var infoAgain = new ServiceRegistrationInfo(
                "test-service", 
                new []
                {
                    new Location("1.2.3.4", 111),
                    new Location("1.2.3.45", 222), 
                });
            
            Assert.Equal(info, infoAgain);
        }

        [Fact]
        public void TestToFrom()
        {
            var twoLocations = new []
            {
                new Location("1.2.3.4", 111),
                new Location("1.2.3.45", 222), 
            };

            var twoAddresses = Location.ToAddresses(twoLocations);

            var adresses = twoAddresses.ToList();
            Assert.Equal(new Address(Host.Of("1.2.3.4"), 111, AddressType.Main), adresses[0]);
            Assert.Equal(new Address(Host.Of("1.2.3.45"), 222, AddressType.Main), adresses[1]);

            var locations = twoLocations.ToList();
            Assert.Equal(locations[0], Location.From(adresses[0]));

            var convertedLocations = Location.From(adresses);
            Assert.Equal(locations, convertedLocations);
        }
    }
}