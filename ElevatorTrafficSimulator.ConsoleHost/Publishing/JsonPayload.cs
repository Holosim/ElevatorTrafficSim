using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;

namespace ElevatorTrafficSimulator.ConsoleHost.Publishing;

public static class JsonPayload
{
    public static JsonElement From<T>(T payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }
}
