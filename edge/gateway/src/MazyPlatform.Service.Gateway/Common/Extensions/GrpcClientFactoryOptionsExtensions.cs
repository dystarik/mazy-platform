namespace MazyPlatform.Service.Gateway.Common.Extensions;

using Grpc.Net.ClientFactory;

using MazyPlatform.Service.Gateway.Interceptors;

internal static class GrpcClientFactoryOptionsExtensions
{
    public static GrpcClientFactoryOptions AddHeadersPropagation(this GrpcClientFactoryOptions options)
    {
        options.InterceptorRegistrations.Add(new InterceptorRegistration(
            InterceptorScope.Channel,
            sp => sp.GetRequiredService<HeadersPropagationInterceptor>()));

        return options;
    }
}
