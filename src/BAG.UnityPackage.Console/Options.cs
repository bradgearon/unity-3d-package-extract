using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace BAG.UnityPackage.Cmd
{
    [TabCompletion]
    [ArgExample("xup [-i] test.unitypackage [-o] test", "extracts test.unitypackage to the folder 'test'")]
    public class Options
    {
        [ArgPosition(0), ArgRequired(PromptIfMissing = true), ArgExistingFile(), ArgShortcut("i")]
        [ArgDescription("Input file, the file to extract")]
        public string In { get; set; }

        [ArgPosition(1), ArgShortcut("o")]
        [ArgDescription("Output path, defaults to the current path and name of the input file")]
        public string Out { get; set; }

        [ArgShortcut("d")]
        [ArgDescription("debug, true to launch debugger")]
        public bool Debug { get; set; }
    }
}
