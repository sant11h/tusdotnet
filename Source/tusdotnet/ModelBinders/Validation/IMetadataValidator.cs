﻿#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Threading.Tasks;
using tusdotnet.Models;

namespace tusdotnet.ModelBinders.Validation
{
    public abstract class MetadataValidator<T> : MetadataValidator where T : ResumableUpload
    {
    }

    public abstract class MetadataValidator
    {
        public abstract Task<IEnumerable<string>> ValidateMetadata(Dictionary<string, Metadata> metadata);
    }
}
#endif