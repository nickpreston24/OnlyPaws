using System.Diagnostics;
using System.Text;
using CodeMechanic.Diagnostics;
using CodeMechanic.Razorhat;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using Htmx;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Serilog.Core;

namespace OnlyPaws.Pages;

public class PetSearch : RazorhatIsland
{
    private readonly bool debug;
    private readonly PetUploaderService petrepo;
    private readonly OrganizationFinder orgfinder;
    private bool enable_history_pushes = false;

    public PetSearch(ArgsMap a, Logger l
        , PetUploaderService petrepo
        , OrganizationFinder orgfinder
    ) : base(a, l)
    {
        this.debug = a.HasFlag("--debug");

        this.petrepo = petrepo;
        this.orgfinder = orgfinder;
    }

    public static List<Pet> Pets { get; set; } = new();
    public PetsGrid Results { get; set; } = new();

    public async Task OnGet()
    {
        var watch = Stopwatch.StartNew();
        // await orgfinder.SeedOrganizations();
        // Pets = await SeedFakePets();

        logger.Information(nameof(PetSearch) + " -> " + nameof(OnGet));

        watch.LogTime(logger.Information);
    }


    public async Task<IActionResult> OnGetSearchPets(string search)
    {
        try
        {
            var watch = Stopwatch.StartNew();
            // logger.Information($"Searching for pet matching '{Query}' ...");
            logger.Information($"Searching for pet matching '{search}' ...");

            var pet_records = await petrepo.GetPetsByName(search);
            if (debug) pet_records.Dump(nameof(pet_records));

            // if (pet_records.Count == 0)
            //     return Partial("_PetsGrid", new PetsGrid());

            var results = pet_records.ToList();

            // string.IsNullOrEmpty(search)
            // ? Pets
            // : Pets
            //     .Where(pet => pet.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
            //     .ToList();

            if (debug)
                Results.Dump(nameof(Results));

            Results = new PetsGrid(results) { pets = results };

            if (!Request.IsHtmx())
                return Page();

            if (this.enable_history_pushes)
                Response.Htmx(h =>
                {
                    // we want to push the current url
                    // into the history
                    h.Push(Request.GetEncodedUrl());
                });

            watch.LogTime();
            return Partial("_PetsGrid", Results);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Partial("_PetsGrid", Results);
        }
    }

    private async Task<List<Pet>> SeedFakePets()
    {
        var watch = Stopwatch.StartNew();


        var existing_pets = await petrepo.GetPets(limit: 100);

        if (!existing_pets.IsNullOrEmpty())
        {
            logger.Information($"Found {existing_pets.Count} existing pets");
            existing_pets.Dump(nameof(existing_pets));

            return existing_pets;
        }

        var fake_pets = new List<Pet>()
        {
            new Pet()
            {
                img_url = "img/catto_only_paws.jpg", name = "Col. Scritches",


                skills = new List<Skill>()
                {
                    new Skill("speech-buttons"),
                }
            },
            new Pet()
            {
                name = "Sadie",
                img_url = "img/doggo_only_paws.jpg",
                skills = new List<Skill>()
                {
                    new Skill("sit"),
                    new Skill("fetch")
                }
            }
        };


        try
        {
            await petrepo.UploadPetsToNeo4jAsync(fake_pets);

            // (string upsert_pets_cypher, var parms) = fake_pets.ToMergeCypher();
            //
            // logger.Information($"Cypher for pet :>> {upsert_pets_cypher}");
        }
        catch (Exception e)
        {
            logger.Error(e.ToString());
        }

        logger.Information($"Total pets loaded {fake_pets.Count}");

        watch.LogTime();
        return fake_pets;
    }
}

public record struct PetsGrid(List<Pet> pets)
{
    public List<Pet> pets { get; set; } = new();
}

public record struct Pet(string name, double age)
{
    // public string name { get; set; } = string.Empty;
    //
    // public double age { get; set; }

    public string img_url { get; set; } = string.Empty;
    public List<Hooman> original_owners { get; set; } = new();
    public string? hook => name.NotEmpty() ? $"Click to see {name}'s story!" : $"Click to see their story!";
    public List<Skill> skills { get; set; } = new();
    public string story { get; set; } = "lorem ipsum";

    public override string ToString()
    {
        string skill_content = new StringBuilder()
            .AppendEach(skills ?? Enumerable.Empty<Skill>(), s => s.Name, delimiter: ",").ToString();

        string content = $@"
        name: {name}
        age: {age}
        skills: {skill_content}
        ";

        Console.WriteLine($"{nameof(content)} :>> {content}");
        return content;
    }
}

public record struct Skill
{
    public Skill(string name)
    {
        this.Name = name;
    }

    public string Name { get; set; } = string.Empty;
}

public record struct Profile()
{
    public bool IsLoggedIn { get; set; } = false;
    public Hooman User { get; set; } = new();
}

public static class Neo4jRecordExtensions
{
    // public static object ToListOf<T>(this List<IRecord> records
    //     , string label = null
    //     // , string first_layer = "Properties" // Stopgap - usually we want "Properties".
    //     , PropertyInfo[] props = null)
    // {
    //     if (label.IsEmpty())
    //         label = typeof(T).Name.ToLower();
    //
    //     var results = new List<T>();
    //
    //     var properties = props?.Length > 0 ? props : typeof(T).GetProperties();
    //
    //     var pets = records
    //         .Select(rec => rec[label].As<Dictionary<string, object>>())
    //         .ToArray();
    //
    //     pets.Dump(nameof(pets));
    //
    //     foreach (var rec in records.Select(x => x.Values))
    //     {
    //         foreach (var prop in properties)
    //         {
    //             string key = prop.Name;
    //             // object value = rec[key].As(prop.PropertyType);
    //             // var instance = Activator.CreateInstance<T>();
    //             // prop.SetValue(instance, value);
    //         }
    //     }
    //
    //     return results;
    // }
    //

    public static T? TryGet<T>(this IRecord r, string key)
    {
        if (!r.Keys.Contains(key))
            return default;

        var value = r[key];

        if (value is null)
            return default;

        return value.As<T>();
    }
}
