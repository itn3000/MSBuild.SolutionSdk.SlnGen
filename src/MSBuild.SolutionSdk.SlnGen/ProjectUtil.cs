using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuild.SolutionSdk.Tasks.Sln;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Definition;

namespace MSBuild.SolutionSdk.Tasks
{
    internal static class ProjectUtil
    {
        public static Project LoadProject(ITaskItem slnproj, string configuration = null, string platform = null)
        {
            return LoadProject(slnproj.ItemSpec.Replace('\\', Path.DirectorySeparatorChar));
        }
        public static Project LoadProject(ProjectItem item, string configuration = null, string platform = null)
        {
            return LoadProject(item.EvaluatedInclude.Replace('\\', Path.DirectorySeparatorChar));
        }
        public static Project LoadProject(string projectPath, string configuration = null, string platform = null)
        {
            var popt = new ProjectOptions();
            if (configuration != null || platform != null)
            {
                popt.GlobalProperties = popt.GlobalProperties ?? new Dictionary<string, string>();
                if (configuration != null)
                {
                    popt.GlobalProperties["Configuration"] = configuration;
                }
                if (platform != null)
                {
                    popt.GlobalProperties["Platform"] = platform;
                }
            }
            var collection = new ProjectCollection(popt.GlobalProperties);
            return collection.LoadProject(projectPath);
        }
    }
}