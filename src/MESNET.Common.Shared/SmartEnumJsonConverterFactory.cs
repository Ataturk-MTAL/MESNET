using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.SmartEnum;
using Ardalis.SmartEnum.SystemTextJson;

namespace MESNET.Common.Shared;

/// <summary>
/// Tüm SmartEnum tiplerini otomatik olarak Name-based JSON serialize/deserialize eden factory.
/// Marten SystemTextJsonSerializer'a eklenir — her SmartEnum için ayrı converter kaydetmeye gerek kalmaz.
/// </summary>
public sealed class SmartEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // SmartEnum<TEnum> veya SmartEnum<TEnum, TValue> miras alan tipleri destekle
        var current = typeToConvert.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType)
            {
                var generic = current.GetGenericTypeDefinition();
                if (generic == typeof(SmartEnum<>) || generic == typeof(SmartEnum<,>))
                    return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // SmartEnum<TEnum, TValue> base tipini bul
        var valueType = typeof(int); // default
        var current = typeToConvert.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType)
            {
                var generic = current.GetGenericTypeDefinition();
                if (generic == typeof(SmartEnum<,>))
                {
                    valueType = current.GetGenericArguments()[1];
                    break;
                }
                if (generic == typeof(SmartEnum<>))
                {
                    valueType = typeof(int);
                    break;
                }
            }
            current = current.BaseType;
        }

        var converterType = typeof(SmartEnumNameConverter<,>).MakeGenericType(typeToConvert, valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
