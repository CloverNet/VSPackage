using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Clover_BuildSolution
{
    public class VersionControlInstance
    {
        public DTE DTE { get; private set; }
        public VersionControlExt VersionControlExt { get; private set; }
        public Package Package { get; private set; }
        public VersionControlInstance(Package package)
        {
            Package = package;
            DTE = Package.GetGlobalService(typeof(DTE)) as DTE;
            VersionControlExt = DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
        }

        public bool IsSlnFile()
        {
            return VersionControlExt.Explorer.SelectedItems.Any() &&
                VersionControlExt.Explorer.SelectedItems[0].SourceServerPath.EndsWith(".sln");
        }

        public string GetSlnFile()
        {
            return VersionControlExt.Explorer.SelectedItems[0].LocalPath;
        }
    }
}
