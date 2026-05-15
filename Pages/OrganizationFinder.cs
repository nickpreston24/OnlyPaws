using System.Diagnostics;
using CodeMechanic.Async;
using CodeMechanic.Diagnostics;
using CodeMechanic.Shargs;
using CodeMechanic.Types;
using JsonFlatFileDataStore;
using Serilog.Core;

namespace OnlyPaws.Pages;

/// <summary>
/// Finds various pet organizations that may have dogs and cats up for adoption
/// </summary>
public class OrganizationFinder : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;
    private bool debug;
    private readonly DataStore socials_db;
    private readonly string socials_db_path;
    private readonly IDocumentCollection<Organization> orgscollection;

    public OrganizationFinder(ArgsMap argsmap, Logger logger)
    {
        this.argsmap = argsmap;
        this.logger = logger;
        this.debug = argsmap.HasFlag("--debug");


        this.socials_db_path = Path.Combine(Directory.GetCurrentDirectory(), "seeds", "socials.json");
        this.socials_db = new DataStore(socials_db_path);

        this.orgscollection = socials_db.GetCollection<Organization>("organizations");

        steps.Add(SeedOrganizations);
    }

    public async Task SeedOrganizations()
    {
        var watch = Stopwatch.StartNew();
        var orgs = orgscollection.AsQueryable();

        var (org_cypher, parameters) = orgs.ToMergeCypher("Organization");

        logger.Information($"{nameof(org_cypher)} :>> {org_cypher}");


        // var latest_id = orgs.Any() ? orgs.Max(x => x.id) : 1;
        // var org = new Organization("PetSmart") { id = latest_id + 1 };
        // logger.Information($"{nameof(SeedOrganizations)} organization: '{org.name}' added.");


        watch.LogTime(logger.Information);
    }


    public sealed class Organization
    {
        public Organization(string name)
        {
            this.name = name;
        }

        public int id { get; set; }
        public string name { get; set; } = string.Empty;
    }
}
