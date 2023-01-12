﻿#if NET6_0_OR_GREATER
using System;
using tusdotnet.Adapters;
using tusdotnet.Models;

namespace tusdotnet.Runners.TusV1Process
{
    public class DeleteFileRequest : TusV1Request
    {
        public string FileId { get; set; }

        internal ContextAdapter ToContextAdapter(DefaultTusConfiguration config)
        {
            return ToContextAdapter("delete", config, fileId: FileId);
        }

        internal static DeleteFileRequest FromContextAdapter(ContextAdapter context)
        {
            return new()
            {
                FileId = context.FileId,
                CancellationToken = context.CancellationToken
            };
        }
    }
}
#endif