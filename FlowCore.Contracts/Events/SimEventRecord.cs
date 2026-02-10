using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;

namespace FlowCore.Contracts.Events;

public readonly record struct SimEventRecord(
    int RunId,
    long Sequence,
    double T,
    SimEventType Type,
    string Source,
    string Message,
    JsonElement Payload
);

