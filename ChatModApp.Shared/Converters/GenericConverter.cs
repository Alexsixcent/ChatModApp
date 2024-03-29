﻿#nullable enable
using ReactiveUI;

namespace ChatModApp.Shared.Converters;

public abstract class GenericConverter<TSource, TResult> : IBindingTypeConverter
{
    public abstract int GetAffinityForObjects(Type fromType, Type toType);

    public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
    {
        var res = TryConvert((TSource) @from, out var to);

        result = to;
        return res;
    }

    public abstract bool TryConvert(TSource from, out TResult result);
}