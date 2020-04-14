using System.Collections.Generic;
using System.Linq;

namespace CQELight.DAL.MongoDb.Mapping
{
    public class IndexDetail
    {
        #region Properties

        public IEnumerable<string> Properties { get; set; } = Enumerable.Empty<string>();
        public bool Unique { get; set; }

        #endregion
    }
}
