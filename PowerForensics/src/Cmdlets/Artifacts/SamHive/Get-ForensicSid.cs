﻿using System.Management.Automation;
using PowerForensics.Artifacts;

namespace PowerForensics.Cmdlets
{
    #region GetSidCommand

    /// <summary> 
    /// This class implements the Get-Sid cmdlet. 
    /// </summary> 
    [Cmdlet(VerbsCommon.Get, "ForensicSid", DefaultParameterSetName = "ByVolume")]
    public class GetSidCommand : PSCmdlet
    {
        #region Parameters

        /// <summary> 
        /// 
        /// </summary> 
        [Parameter(Position = 0, ParameterSetName = "ByVolume")]
        [ValidatePattern(@"^(\\\\\.\\)?[A-Zaz]:$")]
        public string VolumeName
        {
            get { return volume; }
            set { volume = value; }
        }
        private string volume;

        /// <summary> 
        /// This parameter provides the the path of the Registry Hive to parse.
        /// </summary> 
        [Alias("Path")]
        [Parameter(Mandatory = true, ParameterSetName = "ByPath")]
        public string HivePath
        {
            get { return hivePath; }
            set { hivePath = value; }
        }
        private string hivePath;

        #endregion Parameters

        #region Cmdlet Overrides

        /// <summary> 
        /// 
        /// </summary>  
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "ByVolume":
                    WriteObject(Sid.Get(volume), true);
                    break;
                case "ByPath":
                    WriteObject(Sid.GetByPath(hivePath), true);
                    break;
            }
        }

        #endregion Cmdlet Overrides
    }

    #endregion GetSidCommand
}
