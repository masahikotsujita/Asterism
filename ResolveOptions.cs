using System.Collections.Generic;
using CommandLine;

namespace Asterism {

[Verb("resolve", HelpText = "Resolve dependencies")]
internal class ResolveOptions {
    [Option('p', "platform", Separator = ',', HelpText = "Specify platforms to be built,separated by ','.")]
    public IEnumerable<string> Platforms { get; set; }

    [Option('c', "configuration", Separator = ',', HelpText = "Specify configurations to be built, separated by ','.")]
    public IEnumerable<string> Configurations { get; set; }
}

}