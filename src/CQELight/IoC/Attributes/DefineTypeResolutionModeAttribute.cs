using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.IoC.Attributes
{
    /// <summary>
    /// Attribute that can be used with <see cref="Abstractions.IoC.Interfaces.IAutoRegisterType"/>
    /// and <see cref="Abstractions.IoC.Interfaces.IAutoRegisterTypeSingleInstance"/> for specifying
    /// ctor searching scope. If attribute is not specified, a full search is performed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DefineTypeResolutionModeAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Mode to use when searching for ctors.
        /// </summary>
        public TypeResolutionMode Mode { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new <see cref="DefineTypeResolutionModeAttribute"/>.
        /// </summary>
        /// <param name="mode">Mode to use.</param>
        public DefineTypeResolutionModeAttribute(
            TypeResolutionMode mode)
        {
            Mode = mode;
        }
        
        #endregion
    }
}
