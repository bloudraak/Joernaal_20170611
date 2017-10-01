// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class ApplicationBuilder : IApplicationBuilder
    {
        private readonly IList<Func<ProcessDelegate, ProcessDelegate>> _components =
                new List<Func<ProcessDelegate, ProcessDelegate>>();

        public ApplicationBuilder(IServiceProvider serviceProvider)
        {
            ApplicationServices = serviceProvider;
        }

        public IServiceProvider ApplicationServices { get; set; }


        public IApplicationBuilder Use(Func<ProcessDelegate, ProcessDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public ProcessDelegate Build()
        {
            // By default, we're doing nothing
            ProcessDelegate app = context => Task.CompletedTask;

            foreach (var component in _components.Reverse())
                app = component(app);

            return app;
        }
    }
}