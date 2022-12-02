﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tusdotnet.Adapters;
using tusdotnet.Constants;
using tusdotnet.IntentHandlers;
using tusdotnet.Models.Concatenation;

namespace tusdotnet
{
    internal static class IntentAnalyzer
    {
        public static async Task<List<Tuple<IntentHandler, ResponseAdapter>>?> DetermineIntents(ContextAdapter context)
        {
            var firstIntent = DetermineIntent(context);

            if (firstIntent == IntentHandler.NotApplicable)
                return null;

            // TODO figure out how to do multi intent with new contexts

            // Detect intents for "creation with upload"
            var secondIntent = await IntentIncludesCreationWithUpload(firstIntent, context);

            if (secondIntent is null)
                return new() { new(firstIntent, new()) };

            return new()
            {
                new(firstIntent, new()),
                new(secondIntent, new())
            };
        }

        internal static void ModifyContextForNextIntent(ContextAdapter context, IntentHandler previous, IntentHandler next)
        {
            if (previous is not CreateFileHandler and not ConcatenateFilesHandler)
                return;

            if (next is not WriteFileHandler)
                return;

            context.Request.Headers.Remove(HeaderConstants.UploadLength);
            context.Request.Headers[HeaderConstants.UploadOffset] = "0";
        }

        public static async Task<ResponseAdapter> MergeResponses(ContextAdapter context, IEnumerable<Tuple<IntentHandler, ResponseAdapter>> multiResponse)
        {
            var responses = multiResponse.ToArray();

            var first = responses[0];
            var second = responses.Length > 1 ? responses[1] : null;

            if (first.Item1 is not CreateFileHandler and not ConcatenateFilesHandler)
            {
                return first.Item2;
            }

            if (second?.Item1 is not WriteFileHandler)
            {
                return first.Item2;
            }

            if (first.Item2.Status != System.Net.HttpStatusCode.Created)
                return first.Item2;

            var uploadOffset = second.Item2.Headers.TryGetValue(HeaderConstants.UploadOffset, out var uploadOffsetString)
                ? long.Parse(uploadOffsetString)
                : await context.StoreAdapter.GetUploadOffsetAsync(context.FileId, context.CancellationToken);

            first.Item2.SetHeader(HeaderConstants.UploadOffset, uploadOffset.ToString());

            if (second.Item2.Headers.TryGetValue(HeaderConstants.UploadExpires, out var uploadExpires))
            {
                first.Item2.SetHeader(HeaderConstants.UploadExpires, uploadExpires);
            }

            return first.Item2;
        }

        private static async Task<IntentHandler?> IntentIncludesCreationWithUpload(IntentHandler firstIntent, ContextAdapter context)
        {
            if (firstIntent is not CreateFileHandler and not ConcatenateFilesHandler)
                return null;

            // Final files does not support writing.
            if (firstIntent is ConcatenateFilesHandler concatenateFilesHandler && concatenateFilesHandler.UploadConcat.Type is FileConcatFinal)
                return null;

            try
            {

                var writeFileContext = await WriteFileContextForCreationWithUpload.FromCreationContext(context);
                if (!writeFileContext.FileContentIsAvailable)
                    return null;

                context.Request.Body = writeFileContext.Body;

                return new WriteFileHandler(context, true);
            }
            catch
            {
                return null;
            }
        }

        public static IntentHandler DetermineIntent(ContextAdapter context)
        {
            var httpMethod = GetHttpMethod(context.Request);

            if (RequestRequiresTusResumableHeader(httpMethod))
            {
                if (context.Request.Headers.TusResumable == null)
                {
                    return IntentHandler.NotApplicable;
                }
            }

            if (MethodRequiresFileIdUrl(httpMethod))
            {
                if (!context.UrlHelper.UrlMatchesFileIdUrl(context))
                {
                    return IntentHandler.NotApplicable;
                }
            }
            else if (!context.UrlHelper.UrlMatchesUrlPath(context))
            {
                return IntentHandler.NotApplicable;
            }

            return httpMethod switch
            {
                "post" => DetermineIntentForPost(context),
                "patch" => DetermineIntentForPatch(context),
                "head" => DetermineIntentForHead(context),
                "options" => DetermineIntentForOptions(context),
                "delete" => DetermineIntentForDelete(context),
                _ => IntentHandler.NotApplicable,
            };
        }

        /// <summary>
        /// Returns the request method taking X-Http-Method-Override into account.
        /// </summary>
        /// <param name="request">The request to get the method for</param>
        /// <returns>The request method</returns>
        private static string GetHttpMethod(RequestAdapter request)
        {
            var method = request.Headers.XHttpMethodOveride;

            if (string.IsNullOrWhiteSpace(method))
            {
                method = request.Method;
            }

            return method.ToLower();
        }

        private static bool MethodRequiresFileIdUrl(string httpMethod)
        {
            return httpMethod switch
            {
                "head" or "patch" or "delete" => true,
                _ => false,
            };
        }

        private static IntentHandler DetermineIntentForOptions(ContextAdapter context)
        {
            return new GetOptionsHandler(context);
        }

        private static IntentHandler DetermineIntentForHead(ContextAdapter context)
        {
            return new GetFileInfoHandler(context);
        }

        private static IntentHandler DetermineIntentForPost(ContextAdapter context)
        {
            if (!context.StoreAdapter.Extensions.Creation)
                return IntentHandler.NotApplicable;

            var hasUploadConcatHeader = context.Request.Headers.ContainsKey(HeaderConstants.UploadConcat);

            if (context.StoreAdapter.Extensions.Concatenation && hasUploadConcatHeader)
            {
                return new ConcatenateFilesHandler(context);
            }

            return new CreateFileHandler(context);
        }

        private static IntentHandler DetermineIntentForPatch(ContextAdapter context)
        {
            return new WriteFileHandler(context, initiatedFromCreationWithUpload: false);
        }

        private static IntentHandler DetermineIntentForDelete(ContextAdapter context)
        {
            if (!context.StoreAdapter.Extensions.Termination)
                return IntentHandler.NotApplicable;

            return new DeleteFileHandler(context);
        }

        private static bool RequestRequiresTusResumableHeader(string httpMethod)
        {
            return httpMethod != "options";
        }
    }
}
