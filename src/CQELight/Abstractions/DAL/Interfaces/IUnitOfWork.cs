using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.Abstractions.DAL.Interfaces
{
    /// <summary>
    /// Implementation of UnitOfWork pattern
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Asynchronously saves all modifications into repository.
        /// </summary>
        /// <returns>Number of modifications performed.</returns>
        Task<int> SaveAsync();
    }
}
