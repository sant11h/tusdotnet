﻿#if NETCOREAPP3_1_OR_GREATER

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using tusdotnet.Adapters;
using tusdotnet.Extensions;
using tusdotnet.Models;

namespace tusdotnet
{
    internal class TusEndpointInvoker
    {
        private readonly Func<HttpContext, Task<DefaultTusConfiguration>> _endpointSpecificFactory;

        internal TusEndpointInvoker(Func<HttpContext, Task<DefaultTusConfiguration>> factory)
        {
            _endpointSpecificFactory = factory;
        }

        internal async Task Invoke(HttpContext context)
        {
            var config = await _endpointSpecificFactory.Invoke(context);
            if (config == null)
            {
                context.NotFound();
                return;
            }

            EndpointConfigurationValidator.Instance.Validate(config);

            var urlPath = GetUrlPath(context);
            var (request, response) = CreateRequestAndResponseAdapters(context);

            var handled = await TusProtocolHandlerIntentBased.Invoke(new ContextAdapter(urlPath, EndpointUrlHelper.Instance)
            {
                Request = request,
                Response = response,
                Configuration = config,
                CancellationToken = context.RequestAborted,
                HttpContext = context
            });

            if (handled == ResultType.ContinueExecution)
            {
                context.NotFound();
            }
        }

        private static (RequestAdapter Request, ResponseAdapter Response) CreateRequestAndResponseAdapters(HttpContext context)
        {
            var request = DotnetCoreAdapterFactory.CreateRequestAdapter(context, DotnetCoreAdapterFactory.GetRequestUri(context));
            var response = DotnetCoreAdapterFactory.CreateResponseAdapter(context);

            return (request, response);
        }

        private static string GetUrlPath(HttpContext httpContext)
        {
            var fileId = httpContext.GetRouteValue(EndpointRouteConstants.FileId) as string;

            if (string.IsNullOrEmpty(fileId))
                return httpContext.Request.Path;

            var path = httpContext.Request.Path.Value.AsSpan();
            return path[..(path.LastIndexOf('/') + 1)].ToString();

        }
    }
}

#endif