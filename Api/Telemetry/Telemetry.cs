// Telemetry/Traces.cs (shared)
using System.Diagnostics;

namespace PrepUp.Telemetry;
public static class Traces
{
    public static readonly ActivitySource OutboxSource = new("OutboxDispatcher");
}
