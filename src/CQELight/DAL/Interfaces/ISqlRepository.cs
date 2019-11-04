using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQELight.DAL.Interfaces
{
    /// <summary>
    /// Contract interface for repositories that can handle SQL requests.
    /// </summary>
    [Obsolete("This ISqlRepository is not supported anymore")]
    public interface ISqlRepository
    {
        /// <summary>
        /// Executes an SQL non-query command and gets the number of modified objects.
        /// </summary>
        /// <param name="sql">Raw SQL to execute.</param>
        /// <returns>Number of modified objects.</returns>
        Task<int> ExecuteSQLCommandAsync(string sql);
        /// <summary>
        /// Executes a SQL query to retrieve a scalar value.
        /// </summary>
        /// <typeparam name="TResult">Type of expected scalar value.</typeparam>
        /// <param name="sql">Raw SQL to execute.</param>
        /// <returns>Scalar value or default value.</returns>
        Task<TResult> ExecuteScalarAsync<TResult>(string sql);
    }
}
