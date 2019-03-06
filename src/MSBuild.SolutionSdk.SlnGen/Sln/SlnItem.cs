using System;
using System.IO;

namespace MSBuild.SolutionSdk.Tasks.Sln
{
    class SlnItem
    {
        public string FullPath { get; }
        public string Name { get; }
        public SlnFolder Folder { get; }
        public SlnItem(string fullPath, SlnFolder folder)
        {
            FullPath = fullPath;
            Name = Path.GetFileName(fullPath);
            Folder = folder;
        }
    }
}