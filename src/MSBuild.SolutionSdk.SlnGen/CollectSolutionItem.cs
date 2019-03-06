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
    public class CollectSolutionItem : Task
    {
        [Required]
        public ITaskItem[] SlnProjects { get; set; }
        [Output]
        public ITaskItem[] OutputSolutionItems { get; set; }
        ITaskItem CreateSolutionItem(ProjectItem projectItem)
        {
            var item = new TaskItem(projectItem.EvaluatedInclude);
            item.SetMetadata("SolutionFolder", projectItem.GetMetadataValue("SolutionFolder"));
            item.SetMetadata("SolutionFile", projectItem.Project.FullPath);
            return item;
        }
        IEnumerable<ITaskItem> GetSolutionItems()
        {
            foreach(var slnproj in SlnProjects)
            {
                var project = ProjectUtil.LoadProject(slnproj);
                foreach(var item in project.GetItems("SolutionItem"))
                {
                    yield return CreateSolutionItem(item);
                }
            }
        }
        public override bool Execute()
        {
            OutputSolutionItems = GetSolutionItems().ToArray();
            return true;
        }
    }
}