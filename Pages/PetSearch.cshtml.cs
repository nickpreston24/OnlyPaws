using System.Diagnostics;
using System.Reflection;
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

    public PetSearch(ArgsMap a, Logger l, PetUploaderService petrepo) : base(a, l)
    {
        this.debug = a.HasFlag("--debug");

        this.petrepo = petrepo;
    }

    public static List<Pet> Pets { get; set; } = new();
    public PetsGrid Results { get; set; } = new();

    public async Task OnGet()
    {
        Pets = await SeedFakePets();

        logger.Information(nameof(PetSearch) + " -> " + nameof(OnGet));
    }


    public async Task<IActionResult> OnGetSearchPets(string search)
    {
        var watch = Stopwatch.StartNew();
        // logger.Information($"Searching for pet matching '{Query}' ...");
        logger.Information($"Searching for pet matching '{search}' ...");

        var pet_records = await petrepo.GetPetsByName(search);
        pet_records.Dump(nameof(pet_records));

        // var movies = pet_records.AsObjectsAsync<Movie>();

        // var pets = pet_records.ToListOf<Pet>(label: "pets");
        // pets.Dump(nameof(pets));

        var results = string.IsNullOrEmpty(search)
            ? Pets
            : Pets
                .Where(pet => pet.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Results = new PetsGrid(results) { pets = results };

        // if (debug)
        // Results.pets.Dump(nameof(Results));

        if (!Request.IsHtmx())
            return Page();

        // Response.Htmx(h =>
        // {
        //     // we want to push the current url
        //     // into the history
        //     h.Push(Request.GetEncodedUrl());
        // });

        watch.LogTime();
        return Partial("_PetsGrid", Results);
    }

    private async Task<List<Pet>> SeedFakePets()
    {
        var watch = Stopwatch.StartNew();

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
        string skill_content = new StringBuilder().AppendEach(skills, s => s.Name, delimiter: ",").ToString();
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
    public static object ToListOf<T>(this List<IRecord> records
        , string label = null
        // , string first_layer = "Properties" // Stopgap - usually we want "Properties".
        , PropertyInfo[] props = null)
    {
        if (label.IsEmpty())
            label = typeof(T).Name.ToLower();

        var results = new List<T>();

        var properties = props?.Length > 0 ? props : typeof(T).GetProperties();

        var pets = records
            .Select(rec => rec[label].As<Dictionary<string, object>>())
            .ToArray();

        pets.Dump(nameof(pets));

        foreach (var rec in records.Select(x => x.Values))
        {
            foreach (var prop in properties)
            {
                string key = prop.Name;
                // object value = rec[key].As(prop.PropertyType);
                // var instance = Activator.CreateInstance<T>();
                // prop.SetValue(instance, value);
            }
        }

        return results;
    }
}
