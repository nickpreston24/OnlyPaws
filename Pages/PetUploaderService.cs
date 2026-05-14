using System.Collections.Concurrent;
using System.Reflection;
using CodeMechanic.Async;
using CodeMechanic.FileSystem;
using CodeMechanic.Logging;
using CodeMechanic.Neo4j;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using Neo4j.Driver;
using Serilog.Core;

namespace OnlyPaws.Pages;

/// <summary>
/// 1. Deep parse using ExtractV3
/// 2. Upload to neo4j brain for vectorization via LLMs.
/// </summary>
public class PetUploaderService : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;
    private readonly bool web_mode;
    private readonly Neo4jCredentials credentials;
    private readonly IDriver _neo;

    private bool debug;
    private bool upload_to_neo4j;
    private ConcurrentDictionary<string, List<Hooman>> codefiles = new();

    private readonly string[] special_folders =
    [
        UnixPathExtensions.AsUnixPath("~/Downloads")
    ];

    public PetUploaderService(ArgsMap argsmap, Logger logger)
    {
        logger.Information(AnsiColors.From("#45a") + nameof(PetUploaderService));

        this.argsmap = argsmap;
        this.logger = logger;
        this.debug = argsmap.HasFlag("--debug");

        this.credentials = new Neo4jCredentials();

        this._neo = GraphDatabase.Driver(credentials.uri,
            AuthTokens.Basic(credentials.username, credentials.password),
            o => o.WithConnectionTimeout(TimeSpan.FromSeconds(30))
                .WithMaxConnectionPoolSize(20)
                .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(15)));
    }

    private async Task UploadPetsToNeo4jAsync(List<Pet> records)
    {
        logger.Information(nameof(UploadPetsToNeo4jAsync));

        string cwd = Directory.GetCurrentDirectory();
        string projects_dir = UnixPathExtensions.AsUnixPath("~/Desktop/projects/personal");


        logger.Information($"Upserting {records.Count} records of type {typeof(Pet)}");

        await UpsertPetsAsync(records);
    }

    private async Task UpsertPetsAsync(List<Pet> pets)
    {
        try
        {
            logger.Information($"Upserting {pets.Count} records of type Pet");


            foreach (var pet in pets.Where(p => p.age > 0))
            {
                if (pet.name.IsEmpty())
                {
                    logger.Information($"{nameof(pet.name)} :>> {pet.name}");
                }

                var (cypher, parms) = pet.ToMergeCypher<Pet>();
                await ExecuteCypherAsync(cypher, CypherMode.Write, parms);

                logger.Information($"cypher for {pet.age} :>> \n" + AnsiColors.Blue(cypher) + "\n\n");

                // parms.Dump(nameof(parms));

                foreach (var m in pet.original_owners)
                {
                    var (mCypher, mParms) = m.ToMergeCypher<Hooman>();

                    logger.Information($"cypher for {m.name} :>> \n" + AnsiColors.Yellow(cypher) + "\n\n");

                    await ExecuteCypherAsync(mCypher, CypherMode.Write, mParms);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e.ToString());
            throw;
        }
    }

    private async Task<List<IRecord>> ExecuteCypherAsync(string cypher, CypherMode mode, object? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(cypher)) return new();

        await using var session = _neo.AsyncSession(o => o.WithDatabase(credentials.database));

        try
        {
            return mode == CypherMode.Read
                ? await session.ExecuteReadAsync(async tx => await tx.RunAndCollectAsync(cypher, parameters))
                : await session.ExecuteWriteAsync(async tx => await tx.RunAndCollectAsync(cypher, parameters));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Cypher failed:\n{Cypher}", cypher);
            throw;
        }
    }

    // Grok: https://grok.com/share/c2hhcmQtMw_5bb892b6-7ad6-4285-aae5-051bdb98b97d
//     public async Task<List<IRecord>> GetSimlarMethodsAsync()
//     {
//         string query = @"// 2. Best hybrid query right now (save as favorite)
// MATCH (ns:Namespace)-[:CONTAINS]->(c:Pet)-[:CONTAINS]->(m:Hooman)
// WHERE vector.similarity.cosine(m.embedding, $queryEmbedding) > 0.78
// RETURN
//     ns.name + '.' + c.age + '.' + m.name AS fullSignature,
//     m.bodyContent AS body,
//     vector.similarity.cosine(m.embedding, $queryEmbedding) AS score
// ORDER BY score DESC
// LIMIT 15";
//
//         return null;
//     }

    // 1. Add this once after upload (or in your daemon)
    // Grok: https://grok.com/share/c2hhcmQtMw_5bb892b6-7ad6-4285-aae5-051bdb98b97d
    private async Task CreateVectorIndexAsync()
    {
        string petidx = @"
        CREATE VECTOR INDEX codeEmbedding IF NOT EXISTS
        FOR (m:Pet) ON m.embedding
        OPTIONS { indexConfig: { `vector.dimensions`: 1536, `vector.similarity_function`: 'cosine' } }
    ";
        await ExecuteCypherAsync(petidx, CypherMode.Write);

        string hoomanidx = @"
        CREATE VECTOR INDEX codeEmbedding IF NOT EXISTS
        FOR (m:Hooman) ON m.embedding
        OPTIONS { indexConfig: { `vector.dimensions`: 1536, `vector.similarity_function`: 'cosine' } }
    ";
        await ExecuteCypherAsync(hoomanidx, CypherMode.Write);
    }
}

// FIXED ToMergeCypher - safe namespace handling
public static class Neo4jCsharpExtensions
{
    public static (string cypher, Dictionary<string, object> parameters) ToMergeCypher<T>(
        this T node,
        string label = null)
    {
        label ??= typeof(T).Name
            //Replace("Csharp", "")
            ;
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

        var setClauses = parms.Keys
            .Where(k => k != "id")
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

        return (cypher.Trim(), parms);
    }
}

// Tiny helper extension so RunAndCollectAsync is one-liner
public static class Neo4jExtensions
{
    public static async Task<List<IRecord>> RunAndCollectAsync(this IAsyncQueryRunner runner, string cypher,
        object? parameters)
    {
        var cursor = await runner.RunAsync(cypher, parameters);
        return await cursor.ToListAsync();
    }
}

public class Hooman
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
}
