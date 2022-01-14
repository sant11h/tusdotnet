﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace tusdotnet.Tus2
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapTus2<T>(this IEndpointRouteBuilder endpoints, string route, EndpointConfiguration configuration = null) where T : TusHandler
        {
            return endpoints.Map(route, async httpContext =>
            {
                await Tus2Endpoint.Invoke<T>(httpContext, configuration);
            });
        }
    }
}
