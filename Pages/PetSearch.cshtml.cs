using System.Text;
using CodeMechanic.Diagnostics;
using CodeMechanic.Razorhat;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;

namespace OnlyPaws.Pages;

public class PetSearch : RazorhatIsland
{
    private readonly bool debug;

    public PetSearch(ArgsMap a, Logger l) : base(a, l)
    {
        this.debug = a.HasFlag("--debug");
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
        // logger.Information($"Searching for pet matching '{Query}' ...");
        logger.Information($"Searching for pet matching '{search}' ...");

        var results = string.IsNullOrEmpty(search)
            ? Pets
            : Pets
                .Where(pet => pet.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();


        Results = new PetsGrid(results) { pets = results };
        // if (debug)
        Results.pets.Dump(nameof(Results));

        if (!Request.IsHtmx())
            return Page();

        // Response.Htmx(h =>
        // {
        //     // we want to push the current url
        //     // into the history
        //     h.Push(Request.GetEncodedUrl());
        // });

        return Partial("_PetsGrid", Results);
    }

    private async Task<List<Pet>> SeedFakePets()
    {
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


        // try
        // {
        //     (string upsert_pets_cypher, var parms) = fake_pets.ToMergeCypher();
        //
        //     logger.Information($"Cypher for pet :>> {upsert_pets_cypher}");
        // }
        // catch (Exception e)
        // {
        //     logger.Error(e.ToString());
        // }

        logger.Information($"Total pets loaded {fake_pets.Count}");

        return fake_pets;
    }
}

public record struct PetsGrid(List<Pet> pets)
{
    public List<Pet> pets { get; set; } = new();
}

public record struct Pet()
{
    public string name { get; set; } = string.Empty;

    public double age { get; set; }

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
