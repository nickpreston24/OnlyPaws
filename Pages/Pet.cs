using System.Text;
using CodeMechanic.Types;
using OnlyPaws.Pages;

namespace OnlyPaws;

public record struct Pet(string name, double age)
{
    public string name { get; set; } = string.Empty;
    public double age { get; set; }

    public string img_url { get; set; } = string.Empty;
    public List<Hooman> original_owners { get; set; } = new();
    public string? hook => name.NotEmpty() ? $"Click to see {name}'s story!" : $"Click to see their story!";
    public List<Skill> skills { get; set; } = new();
    public string story { get; set; } = "lorem ipsum";
    public double matchscore { get; set; }
    public string breed { get; } = string.Empty;
    public string location { get; set; } = string.Empty;
    public string id { get; set; } = string.Empty;
    public string[] traits { get; set; }

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
