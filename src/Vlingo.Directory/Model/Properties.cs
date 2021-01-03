// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Vlingo.Common;

namespace Vlingo.Directory.Model
{
    public sealed class Properties : ConfigurationProperties
    {
        private static readonly string _propertiesFile = "vlingo-directory.json";

        private static readonly Func<Properties> Factory = () =>
        {
           var props = new Properties();
           props.Load(new FileInfo(_propertiesFile));
           return props;
        };

        private static Lazy<Properties> SingleInstance { get; } = new Lazy<Properties>(Factory, true);

        public static Properties Instance => SingleInstance.Value;

        public string DirectoryGroupAddress()
        {
            var address = GetString("directory.group.address", string.Empty);

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Must define a directory group address in properties file.");
            }

            return address!;
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
            var parsed = GetString(key, defaultValue.ToString());
            if (!string.IsNullOrEmpty(parsed))
            {
                return bool.Parse(parsed);   
            }

            return defaultValue;
        }

        public int GetInteger(string key, int defaultValue)
        {
            var parsed = GetString(key, defaultValue.ToString());
            if (!string.IsNullOrEmpty(parsed))
            {
                return int.Parse(parsed);   
            }

            return defaultValue;
        }

        public string? GetString(string key, string defaultValue)
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
    }
}