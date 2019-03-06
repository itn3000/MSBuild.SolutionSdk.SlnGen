// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MSBuild.SolutionSdk.Tasks.Sln
{
    internal sealed class SlnFolder
    {
        public static readonly string SlnFolderDefaultName = "Solution Items";
        public static readonly Guid FolderProjectTypeGuid = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");

        public SlnFolder(string path, Guid folderGuid)
        {
            Name = Path.GetFileName(path);
            FullPath = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            FolderGuid = folderGuid;
        }

        public string FullPath { get; }

        public Guid FolderGuid { get; }

        public string Name { get; }

        public Guid ProjectTypeGuid => FolderProjectTypeGuid;

        public static Dictionary<string, SlnFolder> CreateFromPath(string path)
        {
            IEnumerable<SlnFolder> GetSlnFolderList(string[] paths)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    var guid = Guid.NewGuid();
                    yield return new SlnFolder(string.Join("\\", paths.Take(i + 1)), guid);
                }
            }
            var pathSegments = path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return GetSlnFolderList(pathSegments).ToDictionary(x => x.FullPath, x => x);
        }
        public static Dictionary<string, SlnFolder> MergePath(Dictionary<string, SlnFolder> solutionFolder, string path)
        {
            solutionFolder = solutionFolder ?? new Dictionary<string, SlnFolder>();
            var paths = path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < paths.Length; i++)
            {
                var p = string.Join(Path.DirectorySeparatorChar.ToString(), paths.Take(i + 1));
                if (solutionFolder.ContainsKey(p))
                {
                    continue;
                }
                var guid = Guid.NewGuid();
                solutionFolder[p] = new SlnFolder(p, guid);
            }
            return solutionFolder;
        }
    }
}