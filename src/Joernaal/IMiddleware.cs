using System.Threading.Tasks;

namespace Joernaal
{
    public interface IMiddleware
    {
        Task InvokeAsync(JoernaalContext context, ProcessDelegate next);
    }
}