// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;

    public interface IApplicationBuilder
    {
        IServiceProvider ApplicationServices { get; set; }

        IApplicationBuilder Use(Func<ProcessDelegate, ProcessDelegate> middleware);

        ProcessDelegate Build();
    }
}