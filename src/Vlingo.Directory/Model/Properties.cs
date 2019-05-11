// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vlingo.Directory.Model
{
    public sealed class Properties
    {
        private static readonly string _propertiesFile = "vlingo-cluster.properties";

        private static Func<Properties> _factory = () => Open();

        private static Lazy<Properties> SingleInstance => new Lazy<Properties>(_factory, true);
        
        private readonly IDictionary<string, string> _dictionary;

        public static Properties Instance => SingleInstance.Value;
        
        public static Properties Open()
        {
            var props = new Properties(new Dictionary<string, string>());
            props.Load(new FileInfo(_propertiesFile));
            return props;
        }
        
        public string DirectoryGroupAddress()
        {
            var address = GetString( "directory.group.address", string.Empty);

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Must define a directory group address in properties file.");
            }

            return address;
        }
        
        public int DirectoryGroupPort()
        {
            var port = GetInteger("directory.group.port", -1);

            if (port == -1)
            {
                throw new InvalidOperationException("Must define a directory group port in properties file.");
            }

            return port;
        }
        
        public int DirectoryIncomingPort()
        {
            var port = GetInteger("directory.incoming.port", -1);

            if (port == -1)
            {
                throw new InvalidOperationException("Must define a directory incoming port in properties file.");
            }

            return port;
        }
        
        public int DirectoryMessageBufferSize() => GetInteger("directory.message.buffer.size", 32767);
        
        public int DirectoryMessageProcessingInterval() => GetInteger("directory.message.processing.interval", 100);
        
        public int DirectoryMessagePublishingInterval() => GetInteger("directory.message.publishing.interval", 5000);
        
        public int DirectoryUnregisteredServiceNotifications() => GetInteger("directory.unregistered.service.notifications", 20);
        
        public bool GetBoolean(string key, bool defaultValue)
        {
            return bool.Parse(GetString(key, defaultValue.ToString()));
        }
        
        public int GetInteger(string key, int defaultValue)
        {
            return int.Parse(GetString(key, defaultValue.ToString()));
        }
        
        public string GetString(string key, string defaultValue)
        {
            return GetProperty(key, defaultValue);
        }
        
        public void ValidateRequired(string nodeName)
        {
            // assertions in each accessor

            DirectoryGroupAddress();
    
            DirectoryGroupPort();
    
            DirectoryIncomingPort();
        }
        
        private Properties(Dictionary<string, string> properties)
        {
            _dictionary = properties;
        }
        
        private string GetProperty(string key) => GetProperty(key, null);

        private string GetProperty(string key, string defaultValue)
        {
            if(_dictionary.TryGetValue(key, out string value))
            {
                return value;
            }

            return defaultValue;
        }
        
        private void SetProperty(string key, string value)
        {
            _dictionary[key] = value;
        }
        
        private void Load(FileInfo configFile)
        {
            foreach(var line in File.ReadAllLines(configFile.FullName))
            {
                if(string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                {
                    continue;
                }

                var items = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                var key = items[0].Trim();
                var val = string.Join("=", items.Skip(1)).Trim();
                SetProperty(key, val);
            }
        }
    }
}