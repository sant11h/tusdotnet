﻿using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace tusdotnet.Tus2
{
    public class CreateFileProcedureResponse : Tus2BaseResponse
    {
        protected override Task WriteResponse(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}