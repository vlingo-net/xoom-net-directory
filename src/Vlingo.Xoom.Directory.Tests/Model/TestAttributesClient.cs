// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Vlingo.Xoom.Cluster.Model.Attribute;

namespace Vlingo.Xoom.Directory.Tests.Model;

public class TestAttributesClient : IAttributesProtocol
{
    private readonly ConcurrentDictionary<string, AttributeSet> _attributeSets;

    public TestAttributesClient() => _attributeSets = new ConcurrentDictionary<string, AttributeSet>();

    public void Add<T>(string attributeSetName, string attributeName, T value)
    {
        if (!_attributeSets.TryGetValue(attributeSetName, out var set))
        {
            set = AttributeSet.Named(attributeSetName);
            _attributeSets.AddOrUpdate(attributeSetName, a => set, (n, u) => set);
        }
        set.AddIfAbsent(Cluster.Model.Attribute.Attribute<T>.From(attributeName, value));
    }

    public void Replace<T>(string attributeSetName, string attributeName, T value)
    {
        if (_attributeSets.TryGetValue(attributeSetName, out var set))
        {
            if (!set.IsNone)
            {
                var tracked = set.AttributeNamed(attributeName);
      
                if (tracked.IsPresent)
                {
                    var other = Cluster.Model.Attribute.Attribute<T>.From(attributeName, value);
        
                    if (!tracked.SameAs(other))
                    {
                        set.Replace(tracked.ReplacingValueWith(other));
                    }
                }
            }
        }
    }

    public void Remove(string attributeSetName, string attributeName)
    {
        if (_attributeSets.TryGetValue(attributeSetName, out var set))
        {
            if (!set.IsNone)
            {
                var tracked = set.AttributeNamed(attributeName);
      
                if (tracked.IsPresent)
                {
                    set.Remove(tracked.Attribute);
                }
            }
        }
    }

    public void RemoveAll(string attributeSetName) => _attributeSets.TryRemove(attributeSetName, out _);

    public IEnumerable<Attribute> AllOf(string attributeSetName)
    {
        var all = new List<Attribute>();
        if (_attributeSets.TryGetValue(attributeSetName, out var set) && set.IsDefined)
        {
            foreach (var tracked in set.All)
            {
                if (tracked.IsPresent)
                {
                    all.Add(tracked.Attribute);
                }
            }
        }

        return all;
    }

    public Attribute<T> Attribute<T>(string attributeSetName, string attributeName)
    {
        if (_attributeSets.TryGetValue(attributeSetName, out var set) && set.IsDefined)
        {
            var tracked = set.AttributeNamed(attributeName);
            if (tracked.IsPresent)
            {
                return (Attribute<T>)tracked.Attribute;
            }
        }
        return Cluster.Model.Attribute.Attribute<T>.Undefined;
    }

    public IEnumerable<AttributeSet> All => _attributeSets.Values;
}