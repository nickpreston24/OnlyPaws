using CodeMechanic.Razorhat;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using Serilog.Core;

namespace OnlyPaws.Pages;

public class PetSearch : RazorhatIsland
{
    public PetSearch(ArgsMap a, Logger l) : base(a, l)
    {
    }

    public List<Pet> Pets { get; set; } = new();

    public async Task OnGet()
    {
        Pets = await SeedFakePets();

        logger.Information(nameof(PetSearch) + " -> " + nameof(OnGet));
    }

    private async Task<List<Pet>> SeedFakePets()
    {
        var fake_pets = new List<Pet>()
        {
            new Pet() { img_url = "img/catto_only_paws.jpg", name = "Col. Scritches" },
            new Pet()
            {
                name = "Sadie",
                img_url = "img/doggo_only_paws.jpg"
            }
        };

        string upsert_pets_cypher = @"merge (pet:Pet {name: 'Sadie', id='1'})";


        return fake_pets;
    }
}

public record struct Pet()
{
    public string name { get; set; } = string.Empty;

    public double age { get; set; }

    public string img_url { get; set; } = string.Empty;
    public List<Hooman> original_owners { get; set; } = new();
    public string? hook => name.NotEmpty() ? $"Click to see {name}'s story!" : $"Click to see their story!";
}

public record struct Profile()
{
    public bool IsLoggedIn { get; set; } = false;
    public Hooman User { get; set; } = new();
}
