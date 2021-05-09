// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Text;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Directory.Model.Message
{
    public class ServiceRegistered : IMessage
    {
        private readonly HashSet<Address> _addresses;
            
        public static string TypeName => "SRVCREGD";

        public IEnumerable<Address> Addresses => _addresses;

        public bool IsValid => !Name.HasNoName;
        
        public Name Name { get; }

        public static ServiceRegistered From(string content)
        {
            if (content.StartsWith(TypeName))
            {
                var name = MessagePartsBuilder.NameFrom(content);
                var addresses = MessagePartsBuilder.AddressFromRecord(content, AddressType.Main);
                return new ServiceRegistered(name, addresses);
            }
            
            return new ServiceRegistered(Name.NoNodeName);
        }

        public static ServiceRegistered As(Name name, Address address) => new ServiceRegistered(name, address);
        
        public static ServiceRegistered As(Name name, IEnumerable<Address> addresses) => new ServiceRegistered(name, addresses);

        public ServiceRegistered(Name name, Address address) : this(name) => _addresses.Add(address);

        public ServiceRegistered(Name name, IEnumerable<Address> addresses) : this(name) => _addresses.UnionWith(addresses);

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.Append(TypeName).Append("\n").Append("nm=").Append(Name.Value);

            foreach (var address in _addresses)
            {
                builder.Append("\n").Append(AddressType.Main.Field).Append(address.Host.Name).Append(":").Append(address.Port);
            }

            return builder.ToString();
        }

        private ServiceRegistered(Name name)
        {
            Name = name;
            _addresses = new HashSet<Address>();
        }
    }
}