using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Asterism {

    class ResolveCommand {
        
        public ResolveCommand(ResolveOptions options) {
            this.options = options;
        }

        public void Run() {
            var workingDirectoryPath = Directory.GetCurrentDirectory();
            var asterismfilePath = Path.Combine(workingDirectoryPath, @".asterismfile.yml");
            var asterismfile = new Asterismfile(asterismfilePath);
        }

        public ResolveOptions options { get; }

    }

}
