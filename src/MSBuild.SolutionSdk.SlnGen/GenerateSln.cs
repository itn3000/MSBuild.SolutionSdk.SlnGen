using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuild.SolutionSdk.Tasks.Sln;

namespace MSBuild.SolutionSdk.Tasks
{
    public class GenerateSln : Task
    {
        // [Required]
        // public ITaskItem[] Projects { get; set; }
        [Required]
        public ITaskItem ProjectName { get; set; }
        [Required]
        public ITaskItem ProjectDirectory { get; set; }
        [Required]
        public ITaskItem[] ProjectMetaData { get; set; }
        // [Required]
        public ITaskItem[] Configurations { get; set; }
        // [Required]
        public ITaskItem[] Platforms { get; set; }
        public ITaskItem[] AdditionalProperties { get; set; }
        public ITaskItem[] SolutionItems { get; set; }
        public ITaskItem VisualStudioVersion { get; set; }
        public ITaskItem MinVisualStudioVersion { get; set; }
        static Dictionary<string, string> ExtractMap(string str, char delimiterElement, char delimiterKeyValue)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new Dictionary<string, string>();
            }
            return str.Split(delimiterElement)
                .Select(x => x.Split(new char[1] { delimiterKeyValue }, 2))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x[0], x => x[1])
                ;
        }
        string[] SplitOrDefault(string str, char[] delim, string[] defaultValues = null)
        {
            defaultValues = defaultValues ?? Array.Empty<string>();
            if (!string.IsNullOrEmpty(str))
            {
                return str.Split(delim, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return defaultValues;
            }
        }
        (string[] configurations, string[] platforms) ExtractProjectConfiguration(string projectConfigurationString)
        {
            var cfgs = SplitOrDefault(projectConfigurationString, new char[] { ';' });
            return (cfgs.Select(x => x.Split('|').First()).Distinct().ToArray(), cfgs.Select(x => x.Split('|').Last()).Distinct().ToArray());
        }
        string[] ExtractConfigurationsFromProject(ITaskItem project)
        {
            var ret = SplitOrDefault(project.GetMetadata("Configurations"), new char[] { ';' });
            if (ret.Length == 0)
            {
                var (cfg, _) = ExtractProjectConfiguration(project.GetMetadata("ProjectConfiguration"));
                ret = cfg;
            }
            return ret.Distinct().ToArray();
        }
        string[] ExtractPlatformsFromProject(ITaskItem project)
        {
            var ret = SplitOrDefault(project.GetMetadata("Platforms"), new char[] { ';' });
            if (ret.Length == 0)
            {
                var (_, p) = ExtractProjectConfiguration(project.GetMetadata("ProjectConfiguration"));
                ret = p;
            }
            return ret.Distinct().ToArray();
        }
        (SlnProject project, string configurationMap, string platformMap) GetProject(ITaskItem[] projects, string[] defaultConfigurations, string[] defaultPlatforms)
        {
            Log.LogMessage("arraynum: {0}", projects.Length);
            foreach (var x in projects)
            {
                Log.LogMessage("x: {0}", x.ItemSpec);
            }
            var proj = projects.First();
            var guidString = proj.GetMetadata("ProjectGuid");
            Guid guid;
            if (string.IsNullOrEmpty(guidString) || !Guid.TryParse(guidString, out guid))
            {
                guid = Guid.NewGuid();
            }
            Guid typeguid;
            var typeguidString = proj.GetMetadata("ProjectTypeGuid");
            if (string.IsNullOrEmpty(typeguidString) || !Guid.TryParse(typeguidString, out typeguid))
            {
                typeguid = SlnProject.GetKnownProjectTypeGuid(Path.GetExtension(proj.ItemSpec), true, new Dictionary<string, Guid>());
            }
            Log.LogMessage("typeguid={0}", typeguid);
            string[] projectConfigurations = SplitOrDefault(proj.GetMetadata("Configurations"), new char[] { ';' });
            string[] projectPlatforms = SplitOrDefault(proj.GetMetadata("Platforms"), new char[] { ';' });
            if (!projectPlatforms.Any() && !projectConfigurations.Any())
            {
                (projectConfigurations, projectPlatforms) = ExtractProjectConfiguration(proj.GetMetadata("ProjectConfiguration"));
            }
            if (!projectConfigurations.Any())
            {
                projectConfigurations = defaultConfigurations;
            }
            if (!projectPlatforms.Any())
            {
                projectPlatforms = defaultPlatforms;
            }
            Log.LogMessage("project configurations = {0}", string.Join("|", projectConfigurations));
            Log.LogMessage("project platforms = {0}", string.Join("|", projectPlatforms));
            return (project: new SlnProject(
                proj.GetMetadata("OriginalItemSpec"),
                Path.GetFileNameWithoutExtension(proj.ItemSpec),
                guid,
                typeguid,
                projectConfigurations,
                projectPlatforms,
                false,
                proj.GetMetadata("SolutionFolder"), proj.GetMetadata("DependsOn")),
                    configurationMap: string.Join(";", projects.Select(x => $"{x.GetMetadata("OriginalConfiguration")}={x.GetMetadata("Configuration")}").Distinct()),
                    platformMap: string.Join(";", projects.Select(x => $"{x.GetMetadata("OriginalPlatform")}={x.GetMetadata("Platform")}").Distinct())
                );
        }
        (SlnProject[] projects, Dictionary<string, string> configurationMap, Dictionary<string, string> platformMap) GetProjects(string[] defaultConfigurations, string[] defaultPlatforms)
        {
            Log.LogMessage("projmetadata num = {0}", ProjectMetaData.Length);
            var grp = ProjectMetaData
                .Where(x => !string.IsNullOrEmpty(x.GetMetadata("Configuration")))
                .OrderBy(x => x.ItemSpec)
                .GroupBy(x => x.ItemSpec)
                .ToDictionary(x => x.Key, x => x.ToArray());
            Log.LogMessage("grp {0}", grp.Count);

            // var ret = grp.OrderBy(x => int.TryParse(x.First().GetMetadata("BuildOrder"), out var order) ? order : 0)
            var ret = grp.Select(kv =>
                {
                    return GetProject(kv.Value.ToArray(), defaultConfigurations, defaultPlatforms);
                }).ToArray();
            return (projects: ret.Select(x => x.project).ToArray(),
                configurationMap: ret.ToDictionary(x => x.project.FullPath, x => x.configurationMap),
                platformMap: ret.ToDictionary(x => x.project.FullPath, x => x.platformMap));
        }
        (SlnItem[], Dictionary<string, SlnFolder>) GetSolutionItems(Dictionary<string, SlnFolder> slnFolders)
        {
            return (SolutionItems.Select(x =>
            {
                string slnFolderName = x.GetMetadata("SolutionFolder");
                if (string.IsNullOrEmpty(slnFolderName))
                {
                    slnFolderName = SlnFolder.SlnFolderDefaultName;
                }
                else
                {
                    slnFolderName = slnFolderName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                }
                slnFolders = SlnFolder.MergePath(slnFolders, slnFolderName);
                Log.LogMessage("slnfolders={0}", string.Join("|", slnFolders.Select(y => $"{y.Key} = {y.Value.FullPath}")));
                return new SlnItem(x.ItemSpec, slnFolders[slnFolderName]);
            }).ToArray(), slnFolders);
        }
        public override bool Execute()
        {
            if (ProjectMetaData != null)
            {
                var configurations = ProjectMetaData.SelectMany(x => ExtractConfigurationsFromProject(x)).Distinct().ToArray();
                var platforms = ProjectMetaData.SelectMany(x => ExtractPlatformsFromProject(x)).Distinct().ToArray();
                if (Platforms != null && Platforms.Length != 0)
                {
                    platforms = Platforms.Select(x => x.ItemSpec).ToArray();
                }
                if (Configurations != null && Configurations.Length != 0)
                {
                    configurations = Configurations.Select(x => x.ItemSpec).ToArray();
                }
                Log.LogMessage("configurations='{0}', platforms='{1}'",
                    string.Join(";", configurations),
                    string.Join(";", platforms));
                var slnFile = new SlnFile("12.0", VisualStudioVersion?.ItemSpec, MinVisualStudioVersion?.ItemSpec, configurations, platforms);
                var slnFileName = Path.GetFileNameWithoutExtension(ProjectName.ItemSpec) + ".sln";
                var (projects, configurationMap, platformMap) = GetProjects(configurations, platforms);
                foreach (var proj in projects)
                {
                    Log.LogMessage("projName = {0}", proj.Name);
                }
                foreach (var kv in platformMap)
                {
                    Log.LogMessage("platformmap = {0}={1}", kv.Key, kv.Value);
                }
                foreach (var kv in configurationMap)
                {
                    Log.LogMessage("configurationmap = {0}={1}", kv.Key, kv.Value);
                }
                slnFile.AddProjects(projects);
                SlnItem[] solutionItems;
                Dictionary<string, SlnFolder> slnFolders = new Dictionary<string, SlnFolder>();
                (solutionItems, slnFolders) = GetSolutionItems(slnFolders);
                slnFile.UpdateSolutionFolder(slnFolders.Values);
                Log.LogMessage("foldernum is {0}, itemsnum is {1}", slnFolders.Count, solutionItems.Length);
                foreach (var solutionItem in solutionItems)
                {
                    Log.LogMessage("{0} = {1}, {2}", solutionItem.FullPath, solutionItem.Folder.FolderGuid, solutionItem.Folder.FullPath);
                }
                foreach (var slnFolder in slnFolders)
                {
                    Log.LogMessage("{0} = {1}", slnFolder.Key, slnFolder.Value.FolderGuid);
                }
                slnFile.AddSolutionItems(solutionItems);
                slnFile.Save(Path.Combine(ProjectDirectory.ItemSpec, slnFileName), false, configurationMap, platformMap);
            }
            else
            {
                Log.LogWarning("Projects is null");
            }
            return true;
        }
    }
}