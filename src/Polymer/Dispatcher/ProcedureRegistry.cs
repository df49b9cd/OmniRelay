using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymer.Dispatcher;

internal sealed class ProcedureRegistry
{
    private readonly Dictionary<string, ProcedureSpec> _procedures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public void Register(ProcedureSpec spec)
    {
        if (spec is null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        var key = CreateKey(spec.Service, spec.Name, spec.Kind);
        var aliasKeys = spec.Aliases.Select(alias => CreateKey(spec.Service, alias, spec.Kind)).ToArray();

        lock (_gate)
        {
            if (_procedures.ContainsKey(key))
            {
                throw new InvalidOperationException($"Procedure '{spec.Name}' ({spec.Kind}) is already registered.");
            }

            if (_aliases.ContainsKey(key))
            {
                throw new InvalidOperationException($"Procedure '{spec.Name}' ({spec.Kind}) conflicts with an existing alias.");
            }

            foreach (var aliasKey in aliasKeys)
            {
                if (_procedures.ContainsKey(aliasKey) || _aliases.ContainsKey(aliasKey))
                {
                    throw new InvalidOperationException($"Alias '{aliasKey}' for procedure '{spec.Name}' conflicts with an existing registration.");
                }
            }

            _procedures.Add(key, spec);

            foreach (var aliasKey in aliasKeys)
            {
                _aliases.Add(aliasKey, key);
            }
        }
    }

    public bool TryGet(string service, string name, ProcedureKind kind, out ProcedureSpec spec)
    {
        var key = CreateKey(service, name, kind);

        lock (_gate)
        {
            if (_procedures.TryGetValue(key, out spec!))
            {
                return true;
            }

            if (_aliases.TryGetValue(key, out var canonical) &&
                _procedures.TryGetValue(canonical, out spec!))
            {
                return true;
            }

            spec = null!;
            return false;
        }
    }

    public IReadOnlyCollection<ProcedureSpec> Snapshot()
    {
        lock (_gate)
        {
            return _procedures.Values.ToArray();
        }
    }

    private static string CreateKey(string service, string name, ProcedureKind kind) =>
        $"{service}::{name}:{kind}";
}
