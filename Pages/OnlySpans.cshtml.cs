using System.Diagnostics;
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

public class OnlySpans : RazorhatIsland
{
    public OnlySpans(ArgsMap a, Logger l
        , PetUploaderService petrepo
        , OrganizationFinder orgfinder
    ) : base(a, l)
    {
        // 1. Parameterless
        this.defaultShelter = new Shelter();

// 2. Primary constructor (optional params)
        this.shelter = new Shelter(
            Id: Guid.NewGuid(),
            PetCount: 23,
            Name: "OnlyPaws Rescue"
        );

// 3. Static Create method - forces named arguments (highly recommended for clarity)
        this.shelter2 = Shelter.Create(
            Id: Guid.NewGuid(),
            Tags: new[] { "dog-friendly", "no-kill" },
            Name: "Happy Tails Shelter",
            City: "Austin"
        );

// 4. With expressions (still works perfectly)
        this.updated = shelter2 with
        {
            Name = "Super Happy Tails Shelter",
            PetCount = 35
        };
    }

    public Shelter shelter { get; set; }

    public Shelter defaultShelter { get; set; }

    public Shelter updated { get; set; }

    public Shelter shelter2 { get; set; }


    public async Task OnGet()
    {
        var watch = Stopwatch.StartNew();
        // await orgfinder.SeedOrganizations();
        // Pets = await SeedFakePets();

        logger.Information(nameof(OnlySpans) + " -> " + nameof(OnGet));

        watch.LogTime(logger.Information);
    }
}
