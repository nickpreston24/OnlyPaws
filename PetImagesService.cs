using System.Collections.Concurrent;
using CodeMechanic.Async;
using CodeMechanic.FileSystem;
using CodeMechanic.Shargs;
using Serilog.Core;


public class PetImagesService : QueuedService
{
    private readonly ArgsMap argsmap;
    private readonly Logger logger;

    public PetImagesService(ArgsMap argsmap, Logger logger)
    {
        this.argsmap = argsmap;
        this.logger = logger;


        steps.Add(FindAllImagesOnDisk);
    }

    private async Task<string[]> FindAllImagesOnDisk()
    {
        string[] special_folders =
        [
            "~/Downloads".AsUnixPath()
        ];

        ConcurrentBag<string> pet_pics = new ConcurrentBag<string>();

        var pics_iterator = new Grepper()
        {
            FileNamePattern = @"only[_]?paws",
            FileSearchMask = "*.jpg,*.png,*webp*,*.gif"
        }.DiscoverDirectoriesDfsAsync();


        await foreach (var pic in pics_iterator)
        {
            pet_pics.Add(pic);
        }

        logger.Information($"Total pet pics on disk: {pet_pics.Count}");

        return pet_pics.ToArray();
    }
}
