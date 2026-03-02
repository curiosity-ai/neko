using System;
using System.IO;

var input = "test_multi";
var inputFullPath = Path.GetFullPath(input);
Console.WriteLine(inputFullPath);
if (Directory.Exists(inputFullPath))
{
    foreach (var subDir in Directory.GetDirectories(inputFullPath))
    {
        Console.WriteLine($"Found dir: {subDir}");
        var yml = Path.Combine(subDir, "neko.yml");
        if (File.Exists(yml))
        {
            Console.WriteLine($"Found neko.yml in {subDir}");
        }
    }
}
