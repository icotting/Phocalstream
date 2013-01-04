using Phocalstream_Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Phocalstream_Web.Application.Data
{
    public class EntityRepository<T> : IEntityRepository<T> where T : class
    {
        private DbContext _dbContext;
        private DbSet<T> _dbSet;

        public EntityRepository(DbContext dbContext) 
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException("The db context cannot be null for an EntityRepository");
            }
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
            if (_dbSet == null)
            {
                throw new ArgumentException(string.Format("The type {0} does not exist in the provided context", typeof(T).FullName));
            }
        }

        public IQueryable<T> Fetch()
        {
            return _dbSet.AsQueryable<T>();
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet.ToList<T>();
        }

        public IEnumerable<T> Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where<T>(predicate);
        }

        public T Single(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _dbSet.SingleOrDefault<T>(predicate);
        }

        public T First(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return _dbSet.FirstOrDefault<T>(predicate);
        }

        public T FindById(object id)
        {
            return _dbSet.Find(id);
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _dbContext.Entry<T>(entity).State = EntityState.Modified;
        }

        public void Delete(T entity)
        {
            if (_dbContext.Entry<T>(entity).State == EntityState.Deleted)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }
    }
}