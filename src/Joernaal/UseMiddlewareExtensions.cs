using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Joernaal
{
    public static class UseMiddlewareExtensions
    {
        internal const string InvokeMethodName = "Invoke";
        internal const string InvokeAsyncMethodName = "InvokeAsync";

        private static readonly MethodInfo GetServiceInfo = typeof(UseMiddlewareExtensions)
            .GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

        public static IApplicationBuilder UseMiddleware<TMiddleware>(this IApplicationBuilder app, params object[] args)
        {
            return app.UseMiddleware(typeof(TMiddleware), args);
        }

        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder app, Type middleware, params object[] args)
        {
            if (typeof(IMiddleware).GetTypeInfo().IsAssignableFrom(middleware.GetTypeInfo()))
            {
                if (args.Length > 0)
                {
                    throw new NotSupportedException("Explicit Middleware doesn't support constructor parameters as it is created through the service provider");
                }
                return UseMiddlewareInterface(app, middleware);
            }

            var applicationServices = app.ApplicationServices;
            return app.Use(next =>
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m =>
                    string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)
                    || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
                ).ToArray();

                if (invokeMethods.Length > 1)
                {
                    throw new InvalidOperationException("The middleware class has multiple invoke methods");
                }

                if (invokeMethods.Length == 0)
                {
                    throw new InvalidOperationException("The middleware class has no invoke methods");
                }

                var methodinfo = invokeMethods[0];
                if (!typeof(Task).IsAssignableFrom(methodinfo.ReturnType))
                {
                    throw new InvalidOperationException("The invoke method does not return a type Task");
                }

                var parameters = methodinfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(HttpContext))
                {
                    throw new InvalidOperationException("The first parameter does not return an invoke method");
                }

                var ctorArgs = new object[args.Length + 1];
                ctorArgs[0] = next;
                Array.Copy(args, 0, ctorArgs, 1, args.Length);
                var instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, ctorArgs);
                if (parameters.Length == 1)
                {
                    return (ProcessDelegate)methodinfo.CreateDelegate(typeof(ProcessDelegate), instance);
                }

                var factory = Compile<object>(methodinfo, parameters);
                return context =>
                {
                    var serviceProvider = context.RequestServices ?? applicationServices;
                    if (serviceProvider == null)
                    {
                        throw new InvalidOperationException("There is no service provider available");
                    }
                    return factory(instance, context, serviceProvider);
                };
            });
        }

        private static IApplicationBuilder UseMiddlewareInterface(IApplicationBuilder app, Type middlewareType)
        {
            return app.Use(next =>
            {
                return async context =>
                {
                    var middlewareFactory = (IMiddlewareFactory)context.RequestServices.GetService(typeof(IMiddlewareFactory));
                    if (middlewareFactory == null)
                    {
                        // No middleware factory
                        throw new InvalidOperationException("There is no middleware factory registered");
                    }

                    var middleware = middlewareFactory.Create(middlewareType);
                    if (middleware == null)
                    {
                        // The factory returned null, it's a broken implementation
                        throw new InvalidOperationException("Unable to create middleware");
                    }

                    try
                    {
                        await middleware.InvokeAsync(context, next);
                    }
                    finally
                    {
                        middlewareFactory.Release(middleware);
                    }
                };
            });
        }

        private static Func<T, HttpContext, IServiceProvider, Task> Compile<T>(MethodInfo methodinfo, ParameterInfo[] parameters)
        {
            // If we call something like
            //
            // public class Middleware
            // {
            //    public Task Invoke(HttpContext context, ILoggerFactory loggeryFactory)
            //    {
            //
            //    }
            // }
            //

            // We'll end up with something like this:
            //   Generic version:
            //
            //   Task Invoke(Middleware instance, HttpContext httpContext, IServiceprovider provider)
            //   {
            //      return instance.Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
            //   }

            //   Non generic version:
            //
            //   Task Invoke(object instance, HttpContext httpContext, IServiceprovider provider)
            //   {
            //      return ((Middleware)instance).Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
            //   }

            var middleware = typeof(T);

            var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
            var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            var instanceArg = Expression.Parameter(middleware, "middleware");

            var methodArguments = new Expression[parameters.Length];
            methodArguments[0] = httpContextArg;
            for (int i = 1; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    throw new NotSupportedException("The invoke method doesn't support out or ref parameters");
                }

                var parameterTypeExpression = new Expression[]
                {
                    providerArg,
                    Expression.Constant(parameterType, typeof(Type)),
                    Expression.Constant(methodinfo.DeclaringType, typeof(Type))
                };

                var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
                methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
            }

            Expression middlewareInstanceArg = instanceArg;
            if (methodinfo.DeclaringType != typeof(T))
            {
                middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodinfo.DeclaringType);
            }

            var body = Expression.Call(middlewareInstanceArg, methodinfo, methodArguments);

            var lambda = Expression.Lambda<Func<T, HttpContext, IServiceProvider, Task>>(body, instanceArg, httpContextArg, providerArg);

            return lambda.Compile();
        }

        private static object GetService(IServiceProvider sp, Type type, Type middleware)
        {
            return sp.GetRequiredService(type);
        }
    }
}