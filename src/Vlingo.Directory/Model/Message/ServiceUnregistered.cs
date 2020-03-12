// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Text;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Directory.Model.Message
{
    public class ServiceUnregistered : IMessage
    {
        public static string TypeName { get; } = "SRVCUNREGD";

        public bool IsValid => !Name.HasNoName;
        
        public Name Name { get; }

        public static ServiceUnregistered From(string content)
        {
            if (content.StartsWith(TypeName))
            {
                var name = MessagePartsBuilder.NameFrom(content);
                return new ServiceUnregistered(name);
            }
            
            return new ServiceUnregistered(Name.NoNodeName);
        }

        public static ServiceUnregistered As(Name name) => new ServiceUnregistered(name);
        
        public ServiceUnregistered(Name name)
        {
            Name = name;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.Append(TypeName).Append("\n").Append("nm=").Append(Name.Value);

            return builder.ToString();
        }
    }
}