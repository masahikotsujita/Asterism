using System;
using CommandLine;

namespace Asterism {

internal class Program {
    private static int Main(string[] args) {
        return Parser.Default.ParseArguments<InitOptions, ResolveOptions, object>(args)
              .MapResult(
                  (InitOptions options) => {
                      var command = new InitCommand(options);
                      return command.Run();
                  },
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