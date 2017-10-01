namespace Joernaal
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class MiddlewareFactory : IMiddlewareFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MiddlewareFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMiddleware Create(Type middlewareType)
        {
            return ServiceProviderServiceExtensions.GetRequiredService(_serviceProvider, middlewareType) as IMiddleware;
        }

        public void Release(IMiddleware middleware)
        {
            // The container owns the lifetime of the service
        }
    }
}