// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using Vlingo.Directory.Client;
using Xunit;

namespace Vlingo.Directory.Tests.Client
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
                    new ServiceRegistrationInfo.Location("1.2.3.4", 111),
                    new ServiceRegistrationInfo.Location("1.2.3.45", 222), 
                });
            
            Assert.Equal("test-service", info.Name);
            Assert.Equal(2, info.Locations.Count());

            var locations = info.Locations.ToList();
            
            Assert.Equal(new ServiceRegistrationInfo.Location("1.2.3.4", 111), locations[0]);
            Assert.Equal(new ServiceRegistrationInfo.Location("1.2.3.45", 222), locations[1]);
            
            var infoAgain = new ServiceRegistrationInfo(
                "test-service", 
                new []
                {
                    new ServiceRegistrationInfo.Location("1.2.3.4", 111),
                    new ServiceRegistrationInfo.Location("1.2.3.45", 222), 
                });
            
            Assert.Equal(info, infoAgain);
        }
    }
}