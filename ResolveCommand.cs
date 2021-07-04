using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asterism {

    class ResolveCommand {
        
        public ResolveCommand(ResolveOptions options) {
            this.options = options;
        }

        public void Run() {
            
        }

        public ResolveOptions options { get; }

    }

}
