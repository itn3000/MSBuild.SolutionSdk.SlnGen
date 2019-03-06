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
    public class CollectProjectMetadata : Task
    {
        [Required]
        public ITaskItem[] SlnProjects { get; set; }
        [Required]
        public ITaskItem[] Configurations { get; set; }
        [Required]
        public ITaskItem[] Platforms { get; set; }
        [Output]
        public ITaskItem[] OutputProjects { get; set; }


        ITaskItem CreateProjectMetadata(Project project, ProjectItem projectTaskItem, string configuration, string platform)
        {
            var item = new TaskItem(project.FullPath);
            item.SetMetadata("OriginalItemSpec", projectTaskItem.EvaluatedInclude);
            item.SetMetadata("Configuration", project.GetPropertyValueOrDefault("Configuration", configuration));
            item.SetMetadata("Platform", project.GetPropertyValueOrDefault("Platform", platform));
            item.SetMetadata("AdditionalProperties", projectTaskItem.GetMetadataValue("AdditionalProperties"));
            var projectConfigurations = project.GetPropertyValueOrDefault("Configurations", "");
            var projectPlatforms = project.GetPropertyValueOrDefault("Platforms", "");
            if(string.IsNullOrEmpty(projectConfigurations))
            {
                var vcxConfigurations = project.GetItems("ProjectConfiguration");
                if(vcxConfigurations.Count != 0)
                {
                    var lst = vcxConfigurations.Select(x => (cfg: x.GetMetadataValue("Configuration"), platform: x.GetMetadataValue("Platform"))).ToArray();
                    projectConfigurations = string.Join(";", lst.Select(x => x.cfg).Distinct());
                    projectPlatforms = string.Join(";", lst.Select(x => x.platform).Distinct());
                }
            }
            item.SetMetadata("SolutionFolder", projectTaskItem.GetMetadataValue("SolutionFolder"));
            item.SetMetadata("Configurations", projectConfigurations);
            item.SetMetadata("Platforms", projectPlatforms);
            item.SetMetadata("ProjectGuid", project.GetPropertyValueOrDefault("ProjectGuid", projectTaskItem.GetMetadataValue("ProjectGuid")));
            item.SetMetadata("ProjectTypeGuid", projectTaskItem.GetMetadataValue("ProjectTypeGuid"));
            item.SetMetadata("DependsOn", projectTaskItem.GetMetadataValue("DependsOn"));
            item.SetMetadata("OriginalConfiguration", configuration);
            item.SetMetadata("OriginalPlatform", platform);
            return item;
        }

        IEnumerable<ITaskItem> GetProjectMetadataList()
        {
            foreach (var slnproj in SlnProjects)
            {
                foreach (var configuration in Configurations)
                {
                    foreach (var platform in Platforms)
                    {
                        var slnProjectInstance = ProjectUtil.LoadProject(slnproj, configuration.ItemSpec, platform.ItemSpec);
                        foreach (var projectItem in slnProjectInstance.GetItems("Project"))
                        {
                            var projectConfiguration = projectItem.GetMetadataValue("Configuration");
                            var projectPlatform = projectItem.GetMetadataValue("Platform");
                            var projectInstance = ProjectUtil.LoadProject(projectItem, projectConfiguration, projectPlatform);
                            yield return CreateProjectMetadata(projectInstance, projectItem, configuration.ItemSpec, platform.ItemSpec);
                        }
                    }
                }

            }
        }
        public override bool Execute()
        {
            OutputProjects = GetProjectMetadataList().ToArray();
            return true;
        }
    }
}