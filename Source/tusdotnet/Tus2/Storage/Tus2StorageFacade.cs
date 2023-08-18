﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace tusdotnet.Tus2
{
    public class Tus2StorageFacade
    {
        public Tus2StorageFacade(Tus2Storage storage)
        {
            Storage = storage;
        }

        public Tus2Storage Storage { get; }

        public virtual async Task<UploadRetrievingProcedureResponse> RetrieveOffset(RetrieveOffsetContext context)
        {
            var offsetTask = Storage.GetOffset(context.Headers.ResourceId);
            var isCompleteTask = Storage.IsComplete(context.Headers.ResourceId);

            await Task.WhenAll(offsetTask, isCompleteTask);

            var offset = offsetTask.Result;
            var isComplete = isCompleteTask.Result;

            return new()
            {
                Status = HttpStatusCode.NoContent,
                UploadOffset = offset,
                UploadIncomplete = !isComplete,
            };
        }

        public virtual async Task<UploadCancellationProcedureResponse> Delete(DeleteContext context)
        {
            await Storage.Delete(context.Headers.ResourceId);

            return new();
        }

        public virtual async Task<CreateFileProcedureResponse> CreateFile(CreateFileContext context)
        {
            await Storage.CreateFile(context.Headers.ResourceId, context);

            return new();
        }

        public async Task<UploadTransferProcedureResponse> WriteData(WriteDataContext context)
        {
            try
            {
                await Storage.WriteData(context.Headers.ResourceId, context);
            }
            catch (OperationCanceledException)
            {
                // Left blank. This is the case when the store does throws on cancellation instead of returning.
            }

            var uploadIncomplete = context.Headers.UploadIncomplete == true;

            var uploadOffset = await Storage.GetOffset(context.Headers.ResourceId);

            if (context.CancellationToken.IsCancellationRequested)
            {
                return new()
                {
                    DisconnectClient = true,
                    RequestUploadIncomplete = uploadIncomplete
                };
            }

            if (uploadIncomplete)
            {
                return new()
                {
                    Status = HttpStatusCode.Created,
                    RequestUploadIncomplete = true,
                    UploadOffset = uploadOffset
                };
            }

            await Storage.MarkComplete(context.Headers.ResourceId);

            return new()
            {
                Status = HttpStatusCode.Created,
                UploadOffset = uploadOffset,
                RequestUploadIncomplete = false,
                EntireUploadCompleted = true
            };
        }

    }
}
