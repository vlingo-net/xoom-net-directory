// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Multicast;

namespace Vlingo.Xoom.Directory.Client
{
    public interface IDirectoryClient : IStoppable
    {
        void Register(ServiceRegistrationInfo info);

        void Unregister(string serviceName);
    }
    
    // TODO: This is an workaround because C# doesn't allow implementation of default methods in interfaces. Should be fixed with C# 8
    public static class DirectoryClientFactory
    {
        public static string ClientName => "vlingo-directory-client";
        
        public static int DefaultMaxMessageSize = 32767;
        
        public static int DefaultProcessingInterval = 1000;
        
        public static int DefaultProcessingTimeout = 10;

        public static IDirectoryClient Instance(Stage stage, IServiceDiscoveryInterest interest, Group directoryPublisherGroup) =>
            Instance(
                stage,
                interest,
                directoryPublisherGroup,
                DefaultMaxMessageSize,
                DefaultProcessingInterval,
                DefaultProcessingTimeout);

        public static IDirectoryClient Instance(
            Stage stage,
            IServiceDiscoveryInterest interest,
            Group directoryPublisherGroup,
            int maxMessageSize,
            long processingInterval,
            int processingTimeout) =>
            stage.ActorFor<IDirectoryClient>(
                () => new DirectoryClientActor(interest, directoryPublisherGroup, maxMessageSize, processingInterval, processingTimeout), ClientName);
    }
}