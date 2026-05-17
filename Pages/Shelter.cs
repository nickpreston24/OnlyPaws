namespace OnlyPaws;

public readonly record struct Shelter
{
    public Guid Id { get; init; }
    public string[] Tags { get; init; }
    public uint PetCount { get; init; }
    public string Name { get; init; }
    public string Address { get; init; }
    public string City { get; init; }
    public string State { get; init; }

    public bool IsEmpty => PetCount == 0 && string.IsNullOrEmpty(Name);

    // Primary constructor with optional parameters (your favorite!)
    public Shelter(
        Guid Id = default,
        string[]? Tags = null,
        uint PetCount = 0,
        string Name = "",
        string Address = "",
        string City = "",
        string State = "")
    {
        this.Id = Id;
        this.Tags = Tags ?? Array.Empty<string>();
        this.PetCount = PetCount;
        this.Name = Name;
        this.Address = Address;
        this.City = City;
        this.State = State;
    }

    // Parameterless constructor for maximum convenience
    public Shelter()
        : this(default, null, 0, "", "", "", "")
    {
    }


    // Optional: Constructor that forces named arguments (prevents ordering mistakes)
    public static Shelter Create(
        Guid? Id = null,
        string[]? Tags = null,
        uint PetCount = 0,
        string Name = "",
        string Address = "",
        string City = "",
        string State = "")
    {
        return new Shelter(
            Id: Id ?? Guid.Empty,
            Tags: Tags,
            PetCount: PetCount,
            Name: Name,
            Address: Address,
            City: City,
            State: State
        );
    }

    // Deconstruct method (great for tuples)
    public void Deconstruct(
        out Guid id,
        out string[] tags,
        out uint petCount,
        out string name,
        out string address,
        out string city,
        out string state)
    {
        id = Id;
        tags = Tags;
        petCount = PetCount;
        name = Name;
        address = Address;
        city = City;
        state = State;
    }
}

//
// public readonly record struct Shelter(
//     Guid Id = default,
//     string[] Tags = null!,
//     uint PetCount = 0,
//     string Name = "",
//     string Address = "",
//     string City = "",
//     string State = "")
// {
//     // This runs after the primary constructor parameters are set
//     public Shelter()
//         : this(default, Array.Empty<string>(), 0, "", "", "", "")
//     {
//     }
//
//     // Optional: You can add validation or computed members here
// }


//
// public readonly record struct Shelter
// {
//     public string Name { get; init; }
//     public string Address { get; init; }
//     public string City { get; init; }
//     public string State { get; init; }
//     public string[] Tags { get; init; }
//     public uint PetCount { get; init; }
//     public Guid Id { get; init; }
//
//     public Shelter(uint petCount = 0)
//     {
//         Name = string.Empty;
//         Address = string.Empty;
//         City = string.Empty;
//         State = string.Empty;
//         Tags = Array.Empty<string>();
//         PetCount = petCount;
//         Id = Guid.Empty;
//     }
// }
//

//
// public record struct Shelter
// {
//     public Shelter()
//     {
//         PetCount = 0;
//     }
//
//     public string Name { get; set; } = string.Empty;
//
//     public string City { get; set; } = string.Empty;
//
//     public string State { get; set; } = string.Empty;
//
//     public string[] Tags { get; set; } = Array.Empty<string>();
//     public uint PetCount { get; set; }
// }
