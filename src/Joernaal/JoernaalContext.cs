namespace Joernaal
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

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
}