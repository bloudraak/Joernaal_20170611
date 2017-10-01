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