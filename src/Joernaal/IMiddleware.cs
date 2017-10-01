namespace Joernaal
{
    using System.Threading.Tasks;

    public interface IMiddleware
    {
        Task InvokeAsync(JoernaalContext context, ProcessDelegate next);
    }
}