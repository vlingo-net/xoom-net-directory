// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Text;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Directory.Model.Message;

public class UnregisterService : IMessage
{
    public static string TypeName => "UNREGSRVC";

    public bool IsValid => !Name.HasNoName;
        
    public Name Name { get; }

    public static UnregisterService From(string content)
    {
        if (content.StartsWith(TypeName))
        {
            var name = MessagePartsBuilder.NameFrom(content);
            return new UnregisterService(name);
        }
            
        return new UnregisterService(Name.NoNodeName);
    }

    public static UnregisterService As(Name name) => new UnregisterService(name);
        
    public UnregisterService(Name name)
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