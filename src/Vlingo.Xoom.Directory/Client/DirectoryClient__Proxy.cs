// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;

namespace Vlingo.Xoom.Directory.Client
{
    public class DirectoryClient__Proxy : IDirectoryClient
    {
        private const string ConcludeRepresentation0 = "Conclude()";
        private const string RegisterRepresentation1 = "Register(ServiceRegistrationInfo)";
        private const string UnregisterRepresentation2 = "Unregister(string)";
        private const string StopRepresentation3 = "Stop()";

        private readonly Actor _actor;
        private readonly IMailbox _mailbox;

        public DirectoryClient__Proxy(Actor actor, IMailbox mailbox)
        {
            _actor = actor;
            _mailbox = mailbox;
        }

        public bool IsStopped => false;

        public void Register(ServiceRegistrationInfo info)
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryClient> consumer = x => x.Register(info);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, RegisterRepresentation1);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryClient>(_actor, consumer, RegisterRepresentation1));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, RegisterRepresentation1));
            }
        }

        public void Unregister(string serviceName)
        {
            if (!_actor.IsStopped)
            {
                Action<IDirectoryClient> consumer = x => x.Unregister(serviceName);
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, UnregisterRepresentation2);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryClient>(_actor, consumer, UnregisterRepresentation2));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, UnregisterRepresentation2));
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
                Action<IDirectoryClient> consumer = x => x.Stop();
                if (_mailbox.IsPreallocated)
                {
                    _mailbox.Send(_actor, consumer, null, StopRepresentation3);
                }
                else
                {
                    _mailbox.Send(new LocalMessage<IDirectoryClient>(_actor, consumer, StopRepresentation3));
                }
            }
            else
            {
                _actor.DeadLetters?.FailedDelivery(new DeadLetter(_actor, StopRepresentation3));
            }
        }
    }
}