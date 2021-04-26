// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Linq;
using Vlingo.Directory.Model.Message;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;

namespace Vlingo.Directory.Tests.Model.Message
{
    public class ServiceRegisteredTest
    {
        private readonly string _textMessage = "SRVCREGD\nnm=test-service\naddr=1.2.3.4:111\naddr=1.2.3.45:222";
        
        [Fact]
        public void TestMessage()
        {
            var registerService = new ServiceRegistered(Name.Of("test-service"),
                new[]
                {
                    Address.From(Host.Of("1.2.3.4"), 111, AddressType.Main),
                    Address.From(Host.Of("1.2.3.45"), 222, AddressType.Main),
                });
                
            
            Assert.Equal(2, registerService.Addresses.Count());
            Assert.Equal(_textMessage, registerService.ToString());
        }

        [Fact]
        public void TestValidity()
        {
            var registerService = new ServiceRegistered(Name.Of("test-service"),
                new[]
                {
                    Address.From(Host.Of("1.2.3.4"), 111, AddressType.Main),
                    Address.From(Host.Of("1.2.3.45"), 222, AddressType.Main),
                });
            
            Assert.True(registerService.IsValid);
            Assert.False(ServiceRegistered.From("blah").IsValid);
            Assert.True(ServiceRegistered.From(_textMessage).IsValid);
        }
    }
}