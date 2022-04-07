using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using Refit;

namespace Tools.Extensions;

public static class QueryExtension
{
    public static Uri AddQueries<TParams>(this Uri uri, TParams queryParams)
    {
        var properties =
            (from p in queryParams.GetType().GetProperties()
             where p.CanRead
             where p.GetValue(queryParams, null) is not null
             select p).ToList();

        var queries = properties.Select(info =>
        {
            var type = info.PropertyType;
            var attribute = info.GetCustomAttribute<AliasAsAttribute>();
            var value = info.GetValue(queryParams, null);

            string valueStr;

            switch (value)
            {
                case Enum enumeration:
                {
                    var name = Enum.GetName(type, value);
                    var attr = type.GetField(name).GetCustomAttribute<EnumMemberAttribute>();

                    valueStr = attr.Value ?? name;
                    break;
                }
                case IEnumerable enumerable when value is not string:
                {
                    var elemType = type.IsGenericType
                                       ? type.GetGenericArguments()[0]
                                       : type.GetElementType();

                    if (elemType.IsPrimitive || elemType == typeof(string))
                        valueStr = string.Join(" ", enumerable.Cast<object>());
                    else if (elemType.IsEnum)
                    {
                        var elemNames = (from object? e in enumerable
                                         let enumType = e.GetType()
                                         let enumName = Enum.GetName(enumType, e)
                                         let enumAttr = enumType.GetField(enumName).GetCustomAttribute<EnumMemberAttribute>()
                                         select enumAttr is not null
                                                    ? enumAttr.Value
                                                    : enumName).ToList();

                        valueStr = string.Join(" ", elemNames);
                    }
                    else throw new ArgumentException("Generic arguments of enumerables must be primitives, strings or enums.");
                        
                    break;
                }
                default:
                    valueStr = value.ToString();
                    break;
            }

            if (attribute is null)
                return info.Name + '=' + valueStr;
            return attribute.Name + '=' + valueStr;
        });

        var query = string.Join("&", queries);

        return uri.AppendQuery(query);
    }

    public static Uri AppendQuery(this Uri uri, string queryToAppend)
    {
        var baseUri = new UriBuilder(uri);

        if (baseUri.Query != null && baseUri.Query.Length > 1)
            baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
        else
            baseUri.Query = queryToAppend;

        return baseUri.Uri;
    }

    public static Uri AddQuery(this Uri uri, string name, string value)
    {
        var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);

        httpValueCollection.Remove(name);
        httpValueCollection.Add(name, value);

        var ub = new UriBuilder(uri);

        // this code block is taken from httpValueCollection.ToString() method
        // and modified so it encodes strings with HttpUtility.UrlEncode
        if (httpValueCollection.Count == 0)
            ub.Query = string.Empty;
        else
        {
            var sb = new StringBuilder();

            for (var i = 0; i < httpValueCollection.Count; i++)
            {
                var text = httpValueCollection.GetKey(i);
                {
                    text = HttpUtility.UrlEncode(text);

                    var val = (text != null) ? (text + "=") : string.Empty;
                    var vals = httpValueCollection.GetValues(i);

                    if (sb.Length > 0)
                        sb.Append('&');

                    if (vals == null || vals.Length == 0)
                        sb.Append(val);
                    else
                    {
                        if (vals.Length == 1)
                        {
                            sb.Append(val);
                            sb.Append(HttpUtility.UrlEncode(vals[0]));
                        }
                        else
                        {
                            for (var j = 0; j < vals.Length; j++)
                            {
                                if (j > 0)
                                    sb.Append('&');

                                sb.Append(val);
                                sb.Append(HttpUtility.UrlEncode(vals[j]));
                            }
                        }
                    }
                }
            }

            ub.Query = sb.ToString();
        }

        return ub.Uri;
    }
}