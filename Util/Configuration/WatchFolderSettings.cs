﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Util.Configuration
{
    [DataContract(Namespace = "Util.Configuration")]
    public class WatchFolderSettings
    {
        public WatchFolderSettings()
        {
            TargetNewsgroups = new List<string>();
        }

        [DataMember(Order = 0)]
        public String ShortName { get; set; }

        [DataMember(Order = 1, Name = "Path")]
        public String PathString { get; set; }
        public DirectoryInfo Path
        {
            get { return Settings.GetOrCreateFolder(PathString); }
            set { PathString = value.FullName; }
        }


        [DataMember(Order = 2)]
        public Boolean UseObfuscation { get; set; }

        [DataMember(Order = 3)]
        public Boolean CleanName { get; set; }
        
        [DataMember]
        public String PreTag { get; set; }

        [DataMember(Order = 4)]
        public String PostTag { get; set; }

        [DataMember(Order = 5)]
        public List<String> TargetNewsgroups { get; set; }

        [DataMember(Order = 6)]
        public Boolean StripFileMetadata { get; set; }

        [DataMember(Order = 7)]
        public String FromAddress { get; set; }

        [DataMember(Order = 8)]
        public Boolean ApplyRandomPassword { get; set; }

        [DataMember(Order = 9)]
        public String RarPassword { get; set; }

        [DataMember(Order = 10)]
        public Int32 Priority { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            //This is redundant, but added for clarity.
            ApplyRandomPassword = false;
            RarPassword = null;
        }
    }
}
