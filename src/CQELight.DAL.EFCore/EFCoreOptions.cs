using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.DAL.EFCore
{
    /// <summary>
    /// Options for using EF Core as DAL.
    /// </summary>
    public class EFCoreOptions
    {

        #region Properties

        /// <summary>
        /// Flag that indicates if logicial deletion is globally disabled.
        /// Note that setting this option to true will remove logical deletion
        /// and this CANNOT be overriden. 
        /// </summary>
        public bool DisableLogicalDeletion { get; set; }

        /// <summary>
        /// Model assembly name to dynamically retrieve models.
        /// </summary>
        public string ModelAssembly { get; set; }

        #endregion
        
    }
}
