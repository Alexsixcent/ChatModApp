using DynamicData;
using Newtonsoft.Json;

namespace ChatModApp.Shared.Converters;

internal class ObservableListConverter<T> : JsonConverter<IObservableList<T>>
{
    public override void WriteJson(JsonWriter writer, IObservableList<T>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.Items, typeof(T[]));
    }

    public override IObservableList<T>? ReadJson(JsonReader reader, Type objectType, IObservableList<T>? existingValue, bool hasExistingValue,
                                                 JsonSerializer serializer)
    {
        var list = serializer.Deserialize<T[]>(reader);

        if (list == null) 
            return existingValue;
        
        var sourceList = new SourceList<T>();

        sourceList.AddRange(list);
        return sourceList;
    }
}