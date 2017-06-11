using System;

namespace Joernaal
{
    public interface IApplicationBuilder
    {
        IServiceProvider ApplicationServices { get; set; }

        IApplicationBuilder Use(Func<ProcessDelegate, ProcessDelegate> middleware);

        ProcessDelegate Build();
    }
}