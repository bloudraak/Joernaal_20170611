using System;

namespace Joernaal
{
    public interface IMiddlewareFactory
    {
        IMiddleware Create(Type middlewareType);

        void Release(IMiddleware middleware);
    }
}