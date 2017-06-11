using System;
using Microsoft.Extensions.DependencyInjection;

namespace Joernaal
{
    public class JoernaalContext
    {
        public JoernaalContext(Item item, IServiceScope serviceScope)
        {
            Item = item;
            ProcessingServices = serviceScope.ServiceProvider;
        }

        public Item Item { get; }

        public IServiceProvider ProcessingServices { get; }

        public ProcessingPhase Phase { get; set; }
    }

    public enum ProcessingPhase
    {
        Preperation = 0,
        Conversion = 1,
        UpdatingReferences = 2,
        Saving = 3
    }
}