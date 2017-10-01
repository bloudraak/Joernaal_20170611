// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System.Threading.Tasks;

    public interface IMiddleware
    {
        Task InvokeAsync(JoernaalContext context, ProcessDelegate next);
    }
}