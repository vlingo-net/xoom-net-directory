// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors;

namespace Vlingo.Directory.Tests.Model
{
    public class DirectoryServiceSupervisorTestActor : Actor, ISupervisor
    {
        public void Inform(Exception error, ISupervised supervised)
        {
            Logger.Error("SUPERVISION CATCH", error);
            supervised.RestartWithin(SupervisionStrategy.Period, SupervisionStrategy.Intensity, SupervisionStrategy.Scope);
        }

        public ISupervisionStrategy SupervisionStrategy { get; } = new RestartSupervisionStrategy();

        public ISupervisor Supervisor => Stage.World.DefaultSupervisor;
        
        private class RestartSupervisionStrategy : ISupervisionStrategy
        {
            public int Intensity => SupervisionStrategyConstants.DefaultIntensity;

            public long Period => SupervisionStrategyConstants.ForeverPeriod;

            public SupervisionStrategyConstants.Scope Scope => SupervisionStrategyConstants.Scope.One;
        }
    }
}