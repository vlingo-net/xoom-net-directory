using System;
using Vlingo.Actors;
using Vlingo.Cluster.Model.Attribute;

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

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public DirectoryService__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public bool IsStopped => false;

        public void AssignLeadership()
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.AssignLeadership();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, AssignLeadershipRepresentation1);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryService>(actor, consumer, AssignLeadershipRepresentation1));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, AssignLeadershipRepresentation1));
            }
        }

        public void RelinquishLeadership()
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.RelinquishLeadership();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, RelinquishLeadershipRepresentation2);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryService>(actor, consumer,
                        RelinquishLeadershipRepresentation2));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, RelinquishLeadershipRepresentation2));
            }
        }

        public void Use(IAttributesProtocol client)
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Use(client);
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, UseRepresentation3);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryService>(actor, consumer, UseRepresentation3));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, UseRepresentation3));
            }
        }

        public void Start()
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Start();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StartRepresentation4);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryService>(actor, consumer, StartRepresentation4));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StartRepresentation4));
            }
        }
        
        public void Conclude()
        {
            if (!actor.IsStopped)
            {
                Action<IStoppable> consumer = x => x.Conclude();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, ConcludeRepresentation0);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IStoppable>(actor, consumer, ConcludeRepresentation0));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, ConcludeRepresentation0));
            }
        }

        public void Stop()
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryService> consumer = x => x.Stop();
                if (mailbox.IsPreallocated)
                {
                    mailbox.Send(actor, consumer, null, StopRepresentation5);
                }
                else
                {
                    mailbox.Send(new LocalMessage<IDirectoryService>(actor, consumer, StopRepresentation5));
                }
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, StopRepresentation5));
            }
        }
    }
}