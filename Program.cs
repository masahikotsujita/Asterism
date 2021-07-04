using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Asterism {

    class Program {

        static void Main(string[] args) {
            Parser.Default.ParseArguments<ResolveOptions, object>(args)
            .WithParsed<ResolveOptions>(options => {
                var command = new ResolveCommand(options);
                command.Run();
            })
            .WithNotParsed(errors => {
                foreach (var error in errors) {
                    Console.WriteLine($"{error}");
                }
            });
        }

    }

}
