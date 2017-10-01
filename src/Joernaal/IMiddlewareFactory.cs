// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;

    public interface IMiddlewareFactory
    {
        IMiddleware Create(Type middlewareType);

        void Release(IMiddleware middleware);
    }
}