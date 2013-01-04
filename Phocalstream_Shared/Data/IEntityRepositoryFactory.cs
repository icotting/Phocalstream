using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data
{
    public interface IEntityRepositoryFactory : IDisposable
    {
        IEntityRepository<T> GetRepository<T>() where T : class;
        void SaveChanges();
    }
}
