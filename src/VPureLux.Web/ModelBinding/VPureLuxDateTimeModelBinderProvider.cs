using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VPureLux.Web.ModelBinding;

public class VPureLuxDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.UnderlyingOrModelType == typeof(DateTime)
            ? new VPureLuxDateTimeModelBinder()
            : null;
    }
}
