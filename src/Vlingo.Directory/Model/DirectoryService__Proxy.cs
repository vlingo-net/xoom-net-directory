// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Cluster.Model.Attribute;

namespace Vlingo.Directory.Model
{
    public class DirectoryService__Proxy : IDirectoryService
    {
        private const string ConcludeRepresentation0 = "Conclude()";
        private const string AssignLeadershipRepresentation1 = "AssignLeadership()";
        private const string RelinquishLeadershipRepresentation2 = "RelinquishLeadership()";
        private const string UseRepresentation3 = "Use(IAttributesProtocol)";
        private const string StartRepresentation4 = "Start()";
        private const string StopRepresentation5 = "Stop()";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public DirectoryService__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public bool IsStopped => false;

        public void AssignLeadership()
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.AssignLeadership();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, AssignLeadershipRepresentation1);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryService>(_actor, consumer, AssignLeadershipRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, AssignLeadershipRepresentation1));
            }
        }

        public void RelinquishLeadership()
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.RelinquishLeadership();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RelinquishLeadershipRepresentation2);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryService>(_actor, consumer,
                        RelinquishLeadershipRepresentation2));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RelinquishLeadershipRepresentation2));
            }
        }

        public void Use(IAttributesProtocol client)
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Use(client);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, UseRepresentation3);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryService>(_actor, consumer, UseRepresentation3));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, UseRepresentation3));
            }
        }

        public void Start()
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Start();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, StartRepresentation4);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryService>(_actor, consumer, StartRepresentation4));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StartRepresentation4));
            }
        }
        
        public void Conclude()
        {
            if (!_actor.IsStopped)
            {
                Action<IStoppable> consumer = x => x.Conclude();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, ConcludeRepresentation0);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IStoppable>(_actor, consumer, ConcludeRepresentation0));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, ConcludeRepresentation0));
            }
        }

        public void Stop()
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Stop();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, StopRepresentation5);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryService>(_actor, consumer, StopRepresentation5));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StopRepresentation5));
            }
        }
    }
}