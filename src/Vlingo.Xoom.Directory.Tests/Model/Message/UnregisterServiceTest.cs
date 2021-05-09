// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Directory.Model.Message;
using Vlingo.Xoom.Wire.Nodes;
using Xunit;

namespace Vlingo.Xoom.Directory.Tests.Model.Message
{
    public class UnregisterServiceTest
    {
        private readonly string _textMessage = "UNREGSRVC\nnm=test-service";
        
        [Fact]
        public void TestMessage()
        {
            var unregisterService = UnregisterService.As(Name.Of("test-service"));
            
            Assert.Equal(Name.Of("test-service"), unregisterService.Name);
            Assert.Equal(_textMessage, unregisterService.ToString());
        }

        [Fact]
        public void TestValidity()
        {
            var unregisterService = UnregisterService.As(Name.Of("test-service"));
            
            Assert.True(unregisterService.IsValid);
            Assert.False(UnregisterService.From("blah").IsValid);
            Assert.True(UnregisterService.From(_textMessage).IsValid);
        }
    }
}