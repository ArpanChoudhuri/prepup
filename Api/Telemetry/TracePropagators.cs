// Telemetry/TracePropagators.cs
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

public static class TracePropagators
{
    private static readonly TextMapPropagator Prop = Propagators.DefaultTextMapPropagator;

    public static void InjectIntoDictionary(Activity? act, IDictionary<string, string> carrier)
    {
        var ctx = act is null ? default : new PropagationContext(act.Context, Baggage.Current);
        Prop.Inject(ctx, carrier, static (c, k, v) => c[k] = v);
    }

    public static ActivityContext ExtractFromDictionary(IDictionary<string, string> carrier)
    {
        var ctx = Prop.Extract(default, carrier,
            static (c, k) => c.TryGetValue(k, out var v) ? new[] { v } : Array.Empty<string>());
        return ctx.ActivityContext;
    }
}
