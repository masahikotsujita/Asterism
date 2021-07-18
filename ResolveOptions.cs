using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Asterism {

    [Verb("resolve", HelpText = "Resolve dependencies")]
    class ResolveOptions {

        [Option('p', "platform", Separator = ',', HelpText = "Specify platforms to be built,separated by ','.")]
        public IEnumerable<String> Platforms { get; set; }

        [Option('c', "configuration", Separator =',', HelpText = "Specify configurations to be built, separated by ','.")]
        public IEnumerable<String> Configurations { get; set; }

    }

}
