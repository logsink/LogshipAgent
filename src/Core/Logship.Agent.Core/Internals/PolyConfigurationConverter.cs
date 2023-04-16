using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Internals
{
    internal class PolyConfigurationConverter<TItem, TList> : JsonConverter<TList>
        where TItem : notnull
        where TList : IList<TItem>, new()
    {
        private readonly IDictionary<string, Type> mappings;
        public PolyConfigurationConverter(IDictionary<string, Type> mappings)
        {
            this.mappings = mappings;
        }

        public override bool CanConvert(Type typeToConvert)
            => typeof(TList).IsAssignableFrom(typeToConvert);

        public override TList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            // Helper function for validating where you are in the JSON    
            void validateToken(Utf8JsonReader reader, JsonTokenType tokenType)
            {
                if (reader.TokenType != tokenType)
                {
                    throw new JsonException($"Invalid token: Was expecting a '{tokenType}' token but received a '{reader.TokenType}' token");
                }
            }

            validateToken(reader, JsonTokenType.StartArray);

            var results = new TList();

            reader.Read(); // Advance to the first object after the StartArray token. This should be either a StartObject token, or the EndArray token. Anything else is invalid.

            while (reader.TokenType == JsonTokenType.StartObject)
            { // Start of 'wrapper' object

                reader.Read(); // Move to property name
                validateToken(reader, JsonTokenType.PropertyName);

                var typeKey = reader.GetString();
                if (typeKey == null)
                {
                    throw new JsonException($"Invalid type key. Expected string \"type\" property.");
                }

                reader.Read(); // Move to start of object (stored in this property)
                validateToken(reader, JsonTokenType.StartObject); // Start of vehicle
                if (this.mappings.TryGetValue(typeKey, out var concreteItemType))
                {
                    TItem? item = (TItem?)JsonSerializer.Deserialize(ref reader, concreteItemType, options);
                    if (item == null)
                    {
                        throw new JsonException($"Unable to deserialize type {typeof(TItem).FullName}, result was null.");
                    }

                    results.Add(item);
                }
                else
                {
                    throw new JsonException($"Unknown type key '{typeKey}' found");
                }

                reader.Read(); // Move past end of item object
                reader.Read(); // Move past end of 'wrapper' object
            }

            validateToken(reader, JsonTokenType.EndArray);
            return results;
        }

        public override void Write(Utf8JsonWriter writer, TList items, JsonSerializerOptions options)
        {

            //writer.WriteStartArray();

            //foreach (var item in items)
            //{

            //    var itemType = item.GetType();

            //    writer.WriteStartObject();

            //    if (this.mappings.TryGetValue(itemType, out var typeKey))
            //    {
            //        writer.WritePropertyName(typeKey);
            //        JsonSerializer.Serialize(writer, item, itemType, options);
            //    }
            //    else
            //    {
            //        throw new JsonException($"Unknown type '{itemType.FullName}' found");
            //    }

            //    writer.WriteEndObject();
            //}

            //writer.WriteEndArray();
            throw new NotSupportedException();
        }
    }
}
