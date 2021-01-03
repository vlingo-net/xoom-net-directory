// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using Vlingo.Wire.Node;

namespace Vlingo.Directory.Client
{
    public sealed class ServiceRegistrationInfo : IComparable<ServiceRegistrationInfo>
    {
        public ServiceRegistrationInfo(string name, IEnumerable<Location> locations)
        {
            Name = name;
            Locations = locations;
        }
        
        public string Name { get; }
        
        public IEnumerable<Location> Locations { get; }
        
        public int CompareTo(ServiceRegistrationInfo? other)
        {
            if (other == null || other.GetType() != typeof(ServiceRegistrationInfo))
            {
                return 1;
            }

            var result =  string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            if (!Locations.SequenceEqual(other.Locations, new LocationComparer()))
            {
                return 1;
            }

            return 0;
        }

        public override bool Equals(object? obj) => CompareTo(obj as ServiceRegistrationInfo) == 0;

        public override int GetHashCode() => 31 * Name.GetHashCode() + Locations.Sum(l => l.GetHashCode());

        public override string ToString() => $"ServiceRegistrationInfo[name={Name}, locations={Locations}]";
    }

    public class Location : IComparable<Location>
    {
        public static Location From(Address address) => new Location(address.HostName, address.Port);

        public static IEnumerable<Location> From(IEnumerable<Address> addresses)
        {
            var listAddresses = addresses.ToList();
            var locations = new List<Location>(listAddresses.Count);
            foreach (var address in listAddresses)
            {
                locations.Add(new Location(address.HostName, address.Port));
            }

            return locations;
        }

        public static IEnumerable<Address> ToAddresses(IEnumerable<Location> locations)
        {
            var listLocations = locations.ToList();
            var addresses = new List<Address>(listLocations.Count);
            foreach (var location in listLocations)
            {
                addresses.Add(new Address(Host.Of(location.Address), location.Port, AddressType.Main));
            }

            return addresses;
        }

        public Location(string address, int port)
        {
            Address = address;
            Port = port;
        }
            
        public string Address { get; }
        
        public int Port { get; }
            
        public int CompareTo(Location? other)
        {
            if (other == null || other.GetType() != typeof(Location))
            {
                return 1;
            }

            var result =  string.Compare(Address, other.Address, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = Port.CompareTo(other.Port);
            if (result != 0)
            {
                return result;
            }

            return 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(Location))
            {
                return false;
            }

            var otherLocation = (Location) obj;
            return Address.Equals(otherLocation.Address) && Port.Equals(otherLocation.Port);
        }

        public override int GetHashCode() => 31 * (Address.GetHashCode() + Port.GetHashCode());

        public override string ToString() => $"Location[address={Address}, port={Port}]";
    }
    
    public class LocationComparer : IEqualityComparer<Location>
    {
        public bool Equals(Location? x, Location? y) => x != null && x.Equals(y);

        public int GetHashCode(Location obj) => obj.GetHashCode();
    }
}