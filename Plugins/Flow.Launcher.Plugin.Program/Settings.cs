﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Flow.Launcher.Plugin.Program
{
    public class Settings
    {
        public DateTime LastIndexTime { get; set; }
        public List<ProgramSource> ProgramSources { get; set; } = new List<ProgramSource>();
        public List<DisabledProgramSource> DisabledProgramSources { get; set; } = new List<DisabledProgramSource>();
        public string[] ProgramSuffixes { get; set; } = {"appref-ms", "exe", "lnk"};

        public bool EnableStartMenuSource { get; set; } = true;

        public bool EnableDescription { get; set; } = false;
        public bool HideAppsPath { get; set; } = true;
        public bool EnableRegistrySource { get; set; } = true;
        public string CustomizedExplorer { get; set; } = Explorer;
        public string CustomizedArgs { get; set; } = ExplorerArgs;

        internal const char SuffixSeperator = ';';

        internal const string Explorer = "explorer";

        internal const string ExplorerArgs = "%s";

        /// <summary>
        /// Contains user added folder location contents as well as all user disabled applications
        /// </summary>
        /// <remarks>
        /// <para>Win32 class applications set UniqueIdentifier using their full file path</para>
        /// <para>UWP class applications set UniqueIdentifier using their Application User Model ID</para>
        /// <para>Custom user added program sources set UniqueIdentifier using their location</para>
        /// </remarks>
        public class ProgramSource
        {
            private string name;

            public string Location { get; set; }
            public string Name { get => name ?? new DirectoryInfo(Location).Name; set => name = value; }
            public bool Enabled { get; set; } = true;
            public string UniqueIdentifier { get; set; }
        }

        public class DisabledProgramSource : ProgramSource { }
    }
}
