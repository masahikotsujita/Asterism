using System;
using CommandLine;

namespace Asterism {

internal class Program {
    private static void Main(string[] args) {
        Parser.Default.ParseArguments<ResolveOptions, object>(args)
              .MapResult(
                  (ResolveOptions options) => {
                      var command = new ResolveCommand(options);
                      return command.Run();
                  },
                  errors => {
                      foreach (var error in errors) {
                          Console.WriteLine($"{error}");
                      }
                      return -1;
                  });
    }
}

}