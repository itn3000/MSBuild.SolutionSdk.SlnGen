// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MSBuild.SolutionSdk.Tasks.Sln
{
    internal sealed class SlnFile
    {
        const string DefaultVsVersion = "16.0.28606.126";
        const string DefaultMinVsVersion = "10.0.40219.1";
        /// <summary>
        /// The solution header
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The file format version
        /// </summary>
        private readonly string _fileFormatVersion;

        /// <summary>
        /// Visual Studio Version
        /// </summary>
        private readonly string _vsVersion;

        /// <summary>
        /// minimum Visual Studio Version
        /// </summary>
        private readonly string _minVsVersion;

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly List<SlnProject> _projects = new List<SlnProject>();

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly List<SlnItem> _solutionItems = new List<SlnItem>();

        private Dictionary<Guid, Dictionary<string, string>> _configurationMap = new Dictionary<Guid, Dictionary<string, string>>();
        private Dictionary<Guid, Dictionary<string, string>> _platformMap = new Dictionary<Guid, Dictionary<string, string>>();

        private string[] _solutionConfigurations;
        private string[] _solutionPlatforms;
        private Dictionary<string, SlnFolder> _solutionFolders = new Dictionary<string, SlnFolder>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SlnFile(string fileFormatVersion, string vsVersion, string minimumVsVersion, string[] configurations, string[] platforms)
        {
            _fileFormatVersion = fileFormatVersion;
            _minVsVersion = string.IsNullOrEmpty(minimumVsVersion) ? DefaultMinVsVersion : minimumVsVersion;
            _vsVersion = string.IsNullOrEmpty(vsVersion) ? DefaultVsVersion : vsVersion;
            _solutionConfigurations = configurations;
            _solutionPlatforms = platforms;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public SlnFile()
            : this("12.00", DefaultVsVersion, DefaultMinVsVersion, null, null)
        {
        }

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<SlnItem> SolutionItems => _solutionItems;

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects);
        }

        public void UpdateConfigurationMap(IEnumerable<KeyValuePair<Guid, Dictionary<string, string>>> configurationMap)
        {
            foreach (var map in configurationMap)
            {
                _configurationMap[map.Key] = map.Value;
            }
        }
        public void UpdatePlatformMap(IEnumerable<KeyValuePair<Guid, Dictionary<string, string>>> maps)
        {
            foreach (var map in maps)
            {
                _platformMap[map.Key] = map.Value;
            }
        }

        /// <summary>
        /// Adds the specified solution items.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        // public void AddSolutionItems(IEnumerable<string> items)
        // {
        //     _solutionItems.AddRange(items.Select(x => new SlnItem(x, null)));
        // }
        /// <summary>
        /// Adds the specified solution items.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(IEnumerable<SlnItem> items)
        {
            _solutionItems.AddRange(items);
            UpdateSolutionFolder(items.Select(x => x.Folder));
        }
        public void UpdateSolutionFolder(IEnumerable<SlnFolder> folders)
        {
            foreach(var folder in folders)
            {
                if(!_solutionFolders.ContainsKey(folder.FullPath))
                {
                    _solutionFolders[folder.FullPath] = folder;
                }
            }
        }
        public void Save(string path, bool folders)
        {
            Save(path, folders, null, null);
        }
        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="path">The full path to the file to write to.</param>
        /// <param name="folders">Specifies if folders should be created.</param>
        public void Save(string path, bool folders, IReadOnlyDictionary<string, string> configurationMap, IReadOnlyDictionary<string, string> platformMap)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!String.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                Save(writer, folders, configurationMap, platformMap);
            }
        }

        public void Save(TextWriter writer, bool folders)
        {
            Save(writer, folders, null, null);
        }

        public void Save(TextWriter writer, bool folders, IReadOnlyDictionary<string, string> configurationMap, IReadOnlyDictionary<string, string> platformMap)
        {
            configurationMap = configurationMap == null ? new Dictionary<string, string>() : configurationMap;
            platformMap = platformMap == null ? new Dictionary<string, string>() : platformMap;
            var vsver = Version.Parse(_vsVersion);
            writer.WriteLine(Header, _fileFormatVersion);
            writer.WriteLine($"# Visual Studio Version {vsver.Major}");
            writer.WriteLine($"VisualStudioVersion = {_vsVersion}");
            writer.WriteLine($"MinimumVisualStudioVersion = {_minVsVersion}");

            var slnFolders = BuildSlnFolderList();

            var projectMap = _projects.ToDictionary(x => x.FullPath);

            foreach (SlnProject project in _projects)
            {
                writer.WriteLine($@"Project(""{project.ProjectTypeGuid.ToSolutionString()}"") = ""{project.Name}"", ""{project.FullPath}"", ""{project.ProjectGuid.ToSolutionString()}""");
                if(project.DependingProjects != null && project.DependingProjects.Length != 0)
                {
                    bool isFound = false;
                    foreach(var dep in project.DependingProjects.Where(x => projectMap.ContainsKey(x)))
                    {
                        if(!isFound)
                        {
                            writer.WriteLine("\t\tProjectSection(ProjectDependencies) = postProject");
                            isFound = true;
                        }
                        writer.WriteLine($"\t\t\t{projectMap[dep].ProjectGuid.ToSolutionString()} = {projectMap[dep].ProjectGuid.ToSolutionString()}");
                    }
                    if(isFound)
                    {
                        writer.WriteLine("\t\tEndProjectSection");
                    }
                }
                writer.WriteLine("EndProject");
            }

            if (SolutionItems.Count > 0)
            {
                foreach (var slnFolder in slnFolders)
                {
                    writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuid.ToSolutionString()}"") = ""{slnFolder.Value.Name}"", ""{slnFolder.Value.Name}"", ""{slnFolder.Value.FolderGuid.ToSolutionString()}"" ");
                    writer.WriteLine("\tProjectSection(SolutionItems) = preProject");
                    foreach (var solutionItem in SolutionItems.Where(x => x.Folder.FolderGuid == slnFolder.Value.FolderGuid))
                    {
                        writer.WriteLine($"\t\t{solutionItem.FullPath} = {solutionItem.FullPath}");
                    }
                    writer.WriteLine("\tEndProjectSection");
                    writer.WriteLine("EndProject");
                }
            }

            SlnHierarchy hierarchy = null;

            if (folders)
            {
                hierarchy = SlnHierarchy.FromProjects(_projects);

                if (hierarchy.Folders.Count > 0)
                {
                    foreach (SlnFolder folder in hierarchy.Folders)
                    {
                        writer.WriteLine($@"Project(""{folder.ProjectTypeGuid.ToSolutionString()}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
                        writer.WriteLine("EndProject");
                    }
                }
            }

            writer.WriteLine("Global");

            var (globalConfigurations, globalPlatforms) = GetSolutionConfigurations();

            WriteSolutionConfigurations(writer, globalConfigurations, globalPlatforms);

            WriteProjectConfigurations(writer, globalConfigurations, globalPlatforms, configurationMap, platformMap);


            if (folders
                && _projects.Count > 1)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");
                foreach (KeyValuePair<Guid, Guid> nestedProject in hierarchy.Hierarchy)
                {
                    writer.WriteLine($@"		{nestedProject.Key.ToSolutionString()} = {nestedProject.Value.ToSolutionString()}");
                }

                writer.WriteLine("	EndGlobalSection");
            }
            {
                bool isFound = false;
                foreach (var slnFolder in _solutionFolders)
                {
                    if (slnFolders.ContainsKey(Path.GetDirectoryName(slnFolder.Key)))
                    {
                        if (!isFound)
                        {
                            writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                            isFound = true;
                        }
                        writer.WriteLine($"\t\t{slnFolder.Value.FolderGuid.ToSolutionString()} = {slnFolders[Path.GetDirectoryName(slnFolder.Key)].FolderGuid.ToSolutionString()}");
                    }
                    var proj = _projects.FirstOrDefault(x => x.SlnFolder != null && x.SlnFolder == slnFolder.Key);
                    if(proj != null)
                    {
                        if (!isFound)
                        {
                            writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                            isFound = true;
                        }
                        writer.WriteLine($"\t\t{proj.ProjectGuid.ToSolutionString()} = {slnFolder.Value.FolderGuid.ToSolutionString()}");
                    }
                }
                if (isFound)
                {
                    writer.WriteLine("\tEndGlobalSection");
                }
            }


            writer.WriteLine("EndGlobal");
        }
        Dictionary<string, SlnFolder> BuildSlnFolderList()
        {
            return _solutionFolders;
        }
        (string[] globalConfigurations, string[] globalPlatforms) GetSolutionConfigurations()
        {
            string[] globalConfigurations = new string[0];
            if (_solutionConfigurations != null && _solutionConfigurations.Length != 0)
            {
                globalConfigurations = _solutionConfigurations;
            }
            else
            {
                globalConfigurations = _projects.SelectMany(p => p.Configurations).Distinct().ToArray();
            }
            string[] globalPlatforms = new string[0];
            if (_solutionPlatforms != null && _solutionPlatforms.Length != 0)
            {
                globalPlatforms = _solutionPlatforms;
            }
            else
            {
                globalPlatforms = _projects.SelectMany(p => p.Platforms).Distinct().ToArray();
            }
            return (globalConfigurations, globalPlatforms);
        }
        void WriteSolutionConfigurations(TextWriter writer, string[] globalConfigurations, string[] globalPlatforms)
        {
            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

            foreach (string configuration in globalConfigurations)
            {
                foreach (string platform in globalPlatforms)
                {
                    if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                    {
                        writer.WriteLine($"\t\t{configuration}|{platform} = {configuration}|{platform}");
                    }
                }
            }

            writer.WriteLine("\tEndGlobalSection");
        }
        void WriteProjectConfigurations(TextWriter writer, string[] globalConfigurations, string[] globalPlatforms,
            IReadOnlyDictionary<string, string> configurationMap, IReadOnlyDictionary<string, string> platformMap)
        {
            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (SlnProject project in _projects)
            {
                var mappedConfigurationMap = new Dictionary<string, string>();
                if (configurationMap.TryGetValue(project.FullPath, out var configurationMapString))
                {
                    mappedConfigurationMap = configurationMapString.Split(';').Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);
                }
                var mappedPlatformMap = new Dictionary<string, string>();
                if (platformMap.TryGetValue(project.FullPath, out var projectPlatformMapString))
                {
                    mappedPlatformMap = projectPlatformMapString.Split(';').Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);
                }
                foreach (var (configuration, globalConfigurationName) in globalConfigurations.Select(x =>
                {
                    if (mappedConfigurationMap.ContainsKey(x))
                    {
                        return (configuration: mappedConfigurationMap[x], globalName: x);
                    }
                    else if (project.Configurations.Contains(x))
                    {
                        return (configuration: x, globalName: x);
                    }
                    else
                    {
                        return (configuration: null, globalName: null);
                    }
                }).Where(x => x.configuration != null))
                {
                    foreach (var (platform, globalPlatformName) in globalPlatforms.Select(x =>
                    {
                        if (mappedPlatformMap.ContainsKey(x))
                        {
                            return (configuration: mappedPlatformMap[x], globalName: x);
                        }
                        else if (project.Platforms.Contains(x))
                        {
                            return (configuration: x, globalName: x);
                        }
                        else
                        {
                            return (configuration: null, globalName: null);
                        }
                    }).Where(x => x.configuration != null))
                    {
                        if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                        {
                            var p = platform != "AnyCPU" ? platform : "Any CPU";
                            writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{globalConfigurationName}|{globalPlatformName}.ActiveCfg = {configuration}|{p}");
                            writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{globalConfigurationName}|{globalPlatformName}.Build.0 = {configuration}|{p}");
                        }
                    }
                }
            }

            writer.WriteLine("\tEndGlobalSection");
        }
    }
}