// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Actors.TestKit;
using Vlingo.Directory.Client;
using Vlingo.Directory.Model;
using Vlingo.Wire.Multicast;

namespace Vlingo.Directory.Tests.Model
{
    public class DirectoryServiceTest
    {
        private TestActor<IDirectoryClient> _client1;
        private TestActor<IDirectoryClient> _client2;
        private TestActor<IDirectoryClient> _client3;
        private TestActor<IDirectoryService> _directory;
        private Group _group;
    }
}