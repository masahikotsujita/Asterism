using System.Collections.Generic;
using CommandLine;

namespace Asterism {

[Verb("init", HelpText = "Fetch and build dependencies according to asterismfile.lock")]
internal class InitOptions
{
    [Option('p', "platform", Separator = ',', HelpText = "Specify platforms to be built,separated by ','.")]
    public IEnumerable<string> Platforms { get; set; }

    [Option('c', "configuration", Separator = ',', HelpText = "Specify configurations to be built, separated by ','.")]
    public IEnumerable<string> Configurations { get; set; }
}

}