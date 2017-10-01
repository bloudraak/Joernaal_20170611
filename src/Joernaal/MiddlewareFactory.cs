// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

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
            return _serviceProvider.GetRequiredService(middlewareType) as IMiddleware;
        }

        public void Release(IMiddleware middleware)
        {
            // The container owns the lifetime of the service
        }
    }
}