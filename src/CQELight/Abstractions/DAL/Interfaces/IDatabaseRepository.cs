using CQELight.DAL.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Abstractions.DAL.Interfaces
{
    /// <summary>
    /// Contract interface for repository upon database.
    /// </summary>
    public interface IDatabaseRepository : IDataReaderRepository, IDataUpdateRepository, IDisposable
    {
    }
}
