using System;
using Microsoft.Extensions.DependencyInjection;

namespace Joernaal
{
    public class HttpContext
    {
        public HttpContext(Item item, IServiceScope serviceScope)
        {
            Item = item;
            RequestServices = serviceScope.ServiceProvider;
        }

        public Item Item { get; }

        public IServiceProvider RequestServices { get; }
    }
}