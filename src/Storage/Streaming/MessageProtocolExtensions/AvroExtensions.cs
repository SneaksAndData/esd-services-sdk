using System.IO;
using System.Text;
using Avro;
using Avro.Generic;
using Avro.IO;

namespace Snd.Sdk.Storage.Streaming.MessageProtocolExtensions;

/// <summary>
/// Convert AVRO messages from and to json.
/// </summary>
public static class AvroExtensions
{
    /// <summary>  
    /// Converts Avro-encoded data to JSON format.  
    /// </summary>  
    /// <param name="avroBytes">The Avro-encoded data to convert.</param>  
    /// <param name="schema">The schema used to encode the data.</param>  
    /// <param name="includeNamespace">Whether to include the namespace in the JSON output.</param>  
    /// <returns>The JSON representation of the Avro-encoded data.</returns>  
    public static string AvroToJson(byte[] avroBytes, Schema schema, bool includeNamespace)
    {
        var reader = new GenericDatumReader<object>(schema, schema);

        var decoder = new BinaryDecoder(new MemoryStream(avroBytes));
        var datum = reader.Read(null, decoder);
        return DatumToJson(datum, schema, includeNamespace);
    }

    /// <summary>  
    /// Converts a datum encoded with the specified schema to JSON format.  
    /// </summary>  
    /// <param name="datum">The datum to convert.</param>  
    /// <param name="schema">The schema used to encode the datum.</param>  
    /// <param name="includeNamespace">Whether to include the namespace in the JSON output.</param>  
    /// <returns>The JSON representation of the datum.</returns>
    public static string DatumToJson(object datum, Schema schema, bool includeNamespace)
    {
        var writer = new GenericDatumWriter<object>(schema);
        var output = new MemoryStream();

        var encoder = new JsonEncoder(schema, output)
        {
            IncludeNamespace = includeNamespace
        };
        writer.Write(datum, encoder);
        encoder.Flush();
        output.Flush();

        return Encoding.UTF8.GetString(output.ToArray());
    }
}
