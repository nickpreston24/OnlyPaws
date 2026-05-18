using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using CodeMechanic.Async;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Logging;
using CodeMechanic.Neo4j;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using Neo4j.Driver;
using Serilog.Core;

namespace OnlyPaws.Pages;

/// <summary>
/// </summary>
public class PetUploaderService : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;
    private readonly bool web_mode;
    private readonly Neo4jCredentials credentials;
    private readonly IDriver neo;

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

        if (debug)
            credentials.Dump(nameof(credentials));

        this.neo = GraphDatabase.Driver(credentials.uri,
            AuthTokens.Basic(credentials.username, credentials.password),
            o => o.WithConnectionTimeout(TimeSpan.FromSeconds(30))
                .WithMaxConnectionPoolSize(20)
                .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(15)));
    }

    public async Task<IReadOnlyList<Pet>> GetPetsByName(string name)
    {
        var pets = new List<Pet>();
        logger.Information(nameof(GetPetsByName));

        string cypher = $@"match (p:Pet)
        where p.name CONTAINS '{name}' or p.name = '{name}'
        return p.name, p.age, p.img_url";

        logger.Information($"{nameof(cypher)} :>> {cypher}");


        // var query_timer = Stopwatch.StartNew();
        // var result = await neo
        //     .ExecutableQuery(cypher)
        //     .ExecuteAsync();
        //
        // query_timer.LogTime(logger.Information, method_name: "regular");

        var execute_non_timer = Stopwatch.StartNew();

        var records = await ExecuteCypherAsync(cypher, CypherMode.Read, new { name });

        execute_non_timer.LogTime(logger.Information, method_name: "exec cypher async");

        return records.Select(MapPet).ToImmutableList();

        // if (result.Result.Count == 0) return new List<Pet>();
        //
        // try
        // {
        //     pets = result.Result
        //         .Select(MapPet)
        //         .ToList();
        //
        //     if (debug)
        //         foreach (var pet in pets)
        //             Console.WriteLine(pet);
        // }
        // catch (Exception e)
        // {
        //     logger.Error(e.ToString());
        //     // throw;
        // }
        // var results = await ExecuteCypherAsync(cypher, CypherMode.Read, new { name, lower = name.ToLower() });

        // Console.WriteLine($"{nameof(results.Count)} :>> {results.Count}");
        // results.Dump(nameof(results));

        return pets.ToImmutableList();
    }

    private static async Task<IEnumerable<Movie>> MoviesSampleMapping(IDriver driver)
    {
        var result = await driver
            .ExecutableQuery("""
                                 MATCH (n:Movie)
                                 RETURN n.title, n.released
                             """)
            .ExecuteAsync();

        var movies = result.Result
            .Select(r => new Movie(
                r["n.title"].As<string>(),
                r["n.released"].As<int>()
            ));

        foreach (var movie in movies)
            Console.WriteLine(movie);

        return movies;
    }

    private record Movie(string title, int released);

    public async Task UploadPetsToNeo4jAsync(List<Pet> records)
    {
        logger.Information(nameof(UploadPetsToNeo4jAsync));

        logger.Information($"Upserting {records.Count} records of type {typeof(Pet)}");

        await UpsertPetsAsync(records);
    }

    private async Task UpsertPetsAsync(List<Pet> pets)
    {
        try
        {
            logger.Information($"Upserting {pets.Count} records of type Pet");
            if (pets.IsNullOrEmpty())
                return;


            foreach (var pet in pets.Where(p => p.name.NotEmpty()))
            {
                var (cypher, parms) = pet.ToMergeCypher<Pet>();

                logger.Information($"cypher for {pet.name} :>> \n" + AnsiColors.Blue(cypher) + "\n\n");

                // parms.Dump(nameof(parms));

                await ExecuteCypherAsync(cypher, CypherMode.Write, parms);

                foreach (var hooman in pet.original_owners ?? Enumerable.Empty<Hooman>())
                {
                    var (mCypher, mParms) = hooman.ToMergeCypher<Hooman>();

                    logger.Information($"cypher for {hooman.name} :>> \n" + AnsiColors.Yellow(cypher) + "\n\n");

                    // await ExecuteCypherAsync(mCypher, CypherMode.Write, mParms);
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
        if (string.IsNullOrWhiteSpace(cypher)) return [];

        await using var session = neo.AsyncSession(o =>
            o.WithDatabase(credentials.database)); // Explicit = no discovery roundtrip

        try
        {
            if (mode == CypherMode.Read)
            {
                // FASTEST PATH for simple reads: direct auto-commit
                var cursor = await session.RunAsync(cypher, parameters);
                return await cursor.ToListAsync();
            }

            // Only use managed tx for writes
            return await session.ExecuteWriteAsync(async tx =>
                await tx.RunAndCollectAsync(cypher, parameters));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Cypher failed:\n{Cypher}", cypher);
            throw;
        }
    }

    // private async Task<List<IRecord>> ExecuteCypherAsync(string cypher, CypherMode mode, object? parameters = null)
    // {
    //     if (string.IsNullOrWhiteSpace(cypher)) return new();
    //
    //     await using var session = neo.AsyncSession(o => o.WithDatabase(credentials.database));
    //
    //     try
    //     {
    //         return mode == CypherMode.Read
    //             ? await session.ExecuteReadAsync(async tx => await tx.RunAndCollectAsync(cypher, parameters))
    //             : await session.ExecuteWriteAsync(async tx => await tx.RunAndCollectAsync(cypher, parameters));
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.Error(ex, "Cypher failed:\n{Cypher}", cypher);
    //         throw;
    //     }
    // }

    private async Task CreateVectorIndexAsync()
    {
        string petidx = @"
        CREATE VECTOR INDEX petEmbedding IF NOT EXISTS
        FOR (m:Pet) ON m.embedding
        OPTIONS { indexConfig: { `vector.dimensions`: 1536, `vector.similarity_function`: 'cosine' } }
    ";
        await ExecuteCypherAsync(petidx, CypherMode.Write);

        string hoomanidx = @"
        CREATE VECTOR INDEX hoomanEmbedding IF NOT EXISTS
        FOR (m:Hooman) ON m.embedding
        OPTIONS { indexConfig: { `vector.dimensions`: 1536, `vector.similarity_function`: 'cosine' } }
    ";
        await ExecuteCypherAsync(hoomanidx, CypherMode.Write);
    }

    public async Task<List<Pet>> GetPets(int limit = 100)
    {
        var cypher = $@"match (p:Pet) return p.name, p.age, p.img_url limit {limit}";

        var result = await this.neo
            .ExecutableQuery(cypher)
            .ExecuteAsync();

        if (result.Result.Count == 0) return new List<Pet>();

        var pets = new List<Pet>();
        try
        {
            pets = result.Result
                .Select(MapPet).ToList();
        }
        catch (Exception e)
        {
            logger.Information(e.ToString());
            // throw;
        }


        return pets;
    }

    private Pet MapPet(IRecord record)
    {
        return new Pet
        {
            name = record.TryGet<string>("p.name"),
            age = record.TryGet<string>("p.age"),
            img_url = record.TryGet<string>("p.img_url"),
            story = record.TryGet<string>("p.story"),
        };
    }
}
