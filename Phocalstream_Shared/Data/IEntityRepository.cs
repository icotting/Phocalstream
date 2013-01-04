using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Phocalstream_Shared.Data
{
    public interface IEntityRepository<T> where T : class
    {
        IQueryable<T> Fetch();
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        T Single(Expression<Func<T, bool>> predicate);
        T First(Expression<Func<T, bool>> predicate);
        T FindById(object id);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
