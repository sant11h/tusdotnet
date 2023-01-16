﻿#if NET6_0_OR_GREATER

using System;
using System.Net;
using tusdotnet.Adapters;
using tusdotnet.Constants;
using tusdotnet.Extensions;
using tusdotnet.Helpers;

namespace tusdotnet.Runners.TusV1Process
{
    public class CreateFileResponse : TusV1Response
    {
        public CreateFileResponse(HttpStatusCode statusCode, string errorMessage) : base(statusCode, errorMessage)
        {
        }

        public string FileId { get; set; }

        public DateTimeOffset? UploadExpires { get; set; }

        internal static CreateFileResponse FromContextAdapter(ContextAdapter context)
        {
            return new(context.Response.Status, context.Response.Message)
            {
                FileId = context.FileId,
                UploadExpires = context.Response.GetResponseHeaderDateTimeOffset(HeaderConstants.UploadExpires)
            };
        }

        internal override void CopySpecificsToCommonContext(ContextAdapter commonContext)
        {
            commonContext.Response.SetHeader(HeaderConstants.Location, commonContext.ConfigUrlPath + "/" + FileId);
            commonContext.FileId = FileId;

            if (UploadExpires is not null)
                commonContext.Response.SetHeader(HeaderConstants.UploadExpires, ExpirationHelper.FormatHeader(UploadExpires));
        }
    }
}

#endif