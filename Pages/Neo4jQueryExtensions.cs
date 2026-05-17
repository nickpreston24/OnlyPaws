using System.Diagnostics;
using System.Reflection;
using CodeMechanic.Diagnostics;
using Neo4j.Driver;

namespace OnlyPaws.Pages;

public static class Neo4jQueryExtensions
{
    public static (string cypher, Dictionary<string, object> parameters) ToMergeCypher<T>(
        this T node,
        string label = null)
    {
        var watch = Stopwatch.StartNew();
        label ??= typeof(T).Name;
        Console.WriteLine($"{nameof(label)} :>> {label}");

        var parms = new Dictionary<string, object>();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        string id = "";

        foreach (var p in props)
        {
            var value = p.GetValue(node);
            if (value == null) continue;

            string key = p.Name switch
            {
                "params" => "parameters",
                "return" => "returnType",
                _ => p.Name
            };

            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                parms[key] = s.Trim();
                if (key is "name" or "age")
                    id = s;
            }
            else if (value is not List<Hooman> && value is not null)
            {
                parms[key] = value;
            }
        }

        // Safe ID + namespace fallback
        // string namespace_name = parms.GetValueOrDefault("name") as string ?? "Global";
        // if (typeof(T) == typeof(Pet))
        // {
        //     var cn = parms.GetValueOrDefault("age") as string ?? "Unknown";
        //     id = $"{namespace_name}.{cn}";
        // }
        // else if (typeof(T) == typeof(Hooman))
        // {
        //     Console.WriteLine($"HIT THE METHODNAME CYPHER GENERATION AND LABEL IS: {label}");
        //     var class_name = parms.GetValueOrDefault("age") as string ?? "Unknown";
        //     var method_name = parms.GetValueOrDefault("name") as string ?? "Unknown";
        //     id = $"{namespace_name}.{class_name}.{method_name}";
        // }

        parms["id"] = id;
        // parms["name"] = namespace_name; // ensure it's never null

        parms["skills"] = null;
        parms.Remove("skills");

        var setClauses = parms.Keys
            .Where(k => k != "id" && k != "skills")
            .Select(k => $"n.{k} = ${k}");

        string setPart = $"SET {string.Join(", ", setClauses)}, n.updated = timestamp()";

        string cypher = $@"
MERGE (n:{label} {{id: $id}})
{setPart}
";


//
//         if (typeof(T) == typeof(Pet))
//         {
//             cypher += @"
// WITH n
// MERGE (pet:Pet {name: $name})
// SET pet.updated = timestamp()
// MERGE (pet)-[:CONTAINS]->(n)
// ";
//         }
//
//         if (typeof(T) == typeof(Hooman) && parms.ContainsKey("age"))
//         {
//             Console.WriteLine($"HIT THE METHODNAME CYPHER GENERATION (MERGE CLASS & NAMESPACE) AND LABEL IS: {label}");
//             cypher += @"
// WITH n
// MERGE (pet:Pet {id: $petId})
// MERGE (pet)-[:CONTAINS]->(n)
// ";
//             parms["classId"] = $"{namespace_name}.{parms.GetValueOrDefault("age")}";
//         }

        watch.LogTime();
        return (cypher.Trim(), parms);
    }

    public static async Task<List<IRecord>> RunAndCollectAsync(this IAsyncQueryRunner runner, string cypher,
        object? parameters)
    {
        var cursor = await runner.RunAsync(cypher, parameters);
        return await cursor.ToListAsync();
    }
}
