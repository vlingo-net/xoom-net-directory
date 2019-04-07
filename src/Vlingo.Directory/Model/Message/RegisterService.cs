// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Text;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Directory.Model.Message
{
    public class RegisterService : IMessage
    {
        private readonly HashSet<Address> _addresses;
            
        public static string TypeName { get; } = "REGSRVC";

        public IEnumerable<Address> Addresses => _addresses;

        public bool IsValid => !Name.HasNoName;
        
        public Name Name { get; }

        public static RegisterService From(string content)
        {
            if (content.StartsWith(TypeName))
            {
                var name = MessagePartsBuilder.NameFrom(content);
                var addresses = MessagePartsBuilder.AddressFromRecord(content, AddressType.Main);
                return new RegisterService(name, addresses);
            }
            
            return new RegisterService(Name.NoNodeName);
        }

        public static RegisterService As(Name name, Address address) => new RegisterService(name, address);
        
        public static RegisterService As(Name name, IEnumerable<Address> addresses) => new RegisterService(name, addresses);

        public RegisterService(Name name, Address address) : this(name) => _addresses.Add(address);

        public RegisterService(Name name, IEnumerable<Address> addresses) : this(name) => _addresses.UnionWith(addresses);

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

        private RegisterService(Name name)
        {
            Name = name;
            _addresses = new HashSet<Address>();
        }
    }
}