using System;
using Vlingo.Actors;

namespace Vlingo.Directory.Client
{
    public class DirectoryClient__Proxy : IDirectoryClient
    {
        private const string RegisterRepresentation1 = "Register(ServiceRegistrationInfo)";
        private const string UnregisterRepresentation2 = "Unregister(string)";
        private const string StopRepresentation3 = "Stop()";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public DirectoryClient__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public bool IsStopped => false;

        public void Register(ServiceRegistrationInfo info)
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryClient> consumer = x => x.Register(info);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, RegisterRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryClient>(actor, consumer, RegisterRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, RegisterRepresentation1));
            }
        }

        public void Unregister(string serviceName)
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryClient> consumer = x => x.Unregister(serviceName);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, UnregisterRepresentation2);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryClient>(actor, consumer, UnregisterRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, UnregisterRepresentation2));
            }
        }

        public void Stop()
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryClient> consumer = x => x.Stop();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StopRepresentation3);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryClient>(actor, consumer, StopRepresentation3));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StopRepresentation3));
            }
        }
    }
}