using System;
using FlintSoft.CQRS;
using FluentValidation;

namespace Visitor.Web.Features;

public static class Extensions
{
    public static IHostApplicationBuilder? AddFeatures(this IHostApplicationBuilder? builder)
    {
        builder?.AddFlintSoftCQRS(typeof(Extensions));
        builder?.Services.AddValidatorsFromAssembly(typeof(Extensions).Assembly);
        return builder;
    }
}
