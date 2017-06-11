using System.Threading.Tasks;

namespace Joernaal
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context, ProcessDelegate next);
    }
}