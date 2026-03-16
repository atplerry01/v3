namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

/// <summary>
/// Parses WHYCEPOLICY DSL strings into PolicyDefinition records.
///
/// DSL format:
///   POLICY "policy-id"
///   NAME "Human readable name"
///   VERSION 1
///   DOMAIN "identity"
///   WHEN field operator "value"
///   THEN action_type key="value" key2="value2"
///
/// Example:
///   POLICY "min-trust-score"
///   NAME "Minimum Trust Score"
///   VERSION 1
///   DOMAIN "identity"
///   WHEN trust_score less_than "50"
///   THEN deny reason="Trust score too low"
/// </summary>
public sealed class PolicyDslParserEngine
{
    private static readonly HashSet<string> KnownActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "allow", "deny", "log", "notify", "flag", "escalate"
    };

    private static readonly HashSet<string> KnownOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "equals", "not_equals", "greater_than", "less_than",
        "contains", "not_contains", "starts_with", "ends_with"
    };

    public PolicyDefinition Parse(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl))
            throw new ArgumentException("DSL input cannot be empty.");

        var lines = dsl
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();

        string? policyId = null;
        string? name = null;
        int? version = null;
        string? targetDomain = null;
        var conditions = new List<PolicyCondition>();
        var actions = new List<PolicyAction>();

        foreach (var line in lines)
        {
            var directive = GetDirective(line);

            switch (directive.ToUpperInvariant())
            {
                case "POLICY":
                    policyId = ExtractQuotedValue(line, "POLICY");
                    break;

                case "NAME":
                    name = ExtractQuotedValue(line, "NAME");
                    break;

                case "VERSION":
                    var versionStr = ExtractRawValue(line, "VERSION");
                    if (!int.TryParse(versionStr, out var v) || v < 1)
                        throw new ArgumentException($"Invalid VERSION: '{versionStr}'. Must be a positive integer.");
                    version = v;
                    break;

                case "DOMAIN":
                    targetDomain = ExtractQuotedValue(line, "DOMAIN");
                    break;

                case "WHEN":
                    conditions.Add(ParseCondition(line));
                    break;

                case "THEN":
                    actions.Add(ParseAction(line));
                    break;

                default:
                    throw new ArgumentException($"Unknown directive: '{directive}'.");
            }
        }

        if (policyId is null)
            throw new ArgumentException("Missing required field: POLICY.");
        if (name is null)
            throw new ArgumentException("Missing required field: NAME.");
        if (version is null)
            throw new ArgumentException("Missing required field: VERSION.");
        if (targetDomain is null)
            throw new ArgumentException("Missing required field: DOMAIN.");
        if (conditions.Count == 0)
            throw new ArgumentException("At least one WHEN condition is required.");
        if (actions.Count == 0)
            throw new ArgumentException("At least one THEN action is required.");

        return new PolicyDefinition(
            policyId,
            name,
            version.Value,
            targetDomain,
            conditions,
            actions,
            DateTime.UtcNow
        );
    }

    private static string GetDirective(string line)
    {
        var spaceIndex = line.IndexOf(' ');
        return spaceIndex < 0 ? line : line[..spaceIndex];
    }

    private static string ExtractQuotedValue(string line, string directive)
    {
        var rest = line[(directive.Length)..].Trim();
        if (rest.Length < 2 || rest[0] != '"' || rest[^1] != '"')
            throw new ArgumentException($"{directive} value must be a quoted string.");

        var value = rest[1..^1];
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{directive} value cannot be empty.");

        return value;
    }

    private static string ExtractRawValue(string line, string directive)
    {
        var rest = line[(directive.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(rest))
            throw new ArgumentException($"{directive} value cannot be empty.");
        return rest;
    }

    private static PolicyCondition ParseCondition(string line)
    {
        // WHEN field operator "value"
        var rest = line["WHEN".Length..].Trim();
        var tokens = Tokenize(rest);

        if (tokens.Count < 3)
            throw new ArgumentException($"Invalid WHEN condition: '{line}'. Expected: WHEN field operator \"value\".");

        var field = tokens[0];
        var op = tokens[1];
        var value = tokens[2];

        if (!KnownOperators.Contains(op))
            throw new ArgumentException($"Unknown operator: '{op}'. Known operators: {string.Join(", ", KnownOperators)}.");

        return new PolicyCondition(field, op, value);
    }

    private static PolicyAction ParseAction(string line)
    {
        // THEN action_type key="value" key2="value2"
        var rest = line["THEN".Length..].Trim();
        var tokens = Tokenize(rest);

        if (tokens.Count < 1)
            throw new ArgumentException($"Invalid THEN action: '{line}'. Expected: THEN action_type [key=\"value\" ...].");

        var actionType = tokens[0];

        if (!KnownActions.Contains(actionType))
            throw new ArgumentException($"Unknown action: '{actionType}'. Known actions: {string.Join(", ", KnownActions)}.");

        var parameters = new Dictionary<string, string>();

        for (int i = 1; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var eqIndex = token.IndexOf('=');
            if (eqIndex < 1)
                throw new ArgumentException($"Invalid parameter format: '{token}'. Expected key=\"value\".");

            var key = token[..eqIndex];
            var val = token[(eqIndex + 1)..];

            if (val.Length >= 2 && val[0] == '"' && val[^1] == '"')
                val = val[1..^1];

            parameters[key] = val;
        }

        return new PolicyAction(actionType, parameters);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var i = 0;

        while (i < input.Length)
        {
            if (char.IsWhiteSpace(input[i]))
            {
                i++;
                continue;
            }

            if (input[i] == '"')
            {
                var end = input.IndexOf('"', i + 1);
                if (end < 0)
                    throw new ArgumentException($"Unterminated string starting at position {i}.");

                tokens.Add(input[(i + 1)..end]);
                i = end + 1;
                continue;
            }

            var start = i;
            while (i < input.Length && !char.IsWhiteSpace(input[i]))
            {
                // Handle key="value" as a single token
                if (input[i] == '"')
                {
                    var end = input.IndexOf('"', i + 1);
                    if (end < 0)
                        throw new ArgumentException($"Unterminated string starting at position {i}.");
                    i = end + 1;
                    continue;
                }
                i++;
            }

            tokens.Add(input[start..i]);
        }

        return tokens;
    }
}
