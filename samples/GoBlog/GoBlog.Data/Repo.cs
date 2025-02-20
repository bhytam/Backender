using GoBlog.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GoBlog.Core;
namespace GoBlog.Data{
    public class Repo<TEntity> : IRepo<TEntity> where TEntity : BaseEntity
    {
        private ApplicationDbContext _context;
        private DbSet<TEntity> _dbset;
        public Repo(ApplicationDbContext context)
        {
            _context = context;
            _dbset = _context.Set<TEntity>();
        }

        public IEnumerable<TEntity> GetAll(Expression<Func<TEntity, bool>> where = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderby = null, string includes = "", char includesSplitChar = ',')
        {
            IQueryable<TEntity> query = _dbset;

            if (where != null)
            {
                query = query.Where(where);
            }

            if (orderby != null)
            {
                query = orderby(query);
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt_);
            }

            if (includes != "")
            {
                foreach (var include in includes.Split(includesSplitChar))
                {
                    query = query.Include(include);
                }
            }

            return query.ToList();
        }
        public virtual TEntity GetById(string id)
        {
            return _dbset.Find(id);
        }
        public virtual bool Insert(TEntity model)
        {
            try
            {   
                _dbset.Add(model);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public virtual bool Update(TEntity model)
        {
            try
            {
                model.ModifiedAt_ = DateTime.Now;
                if (_context.Entry(model).State == EntityState.Detached)
                {
                    _dbset.Attach(model);
                }
                _context.Entry(model).State = EntityState.Modified;
                return true;
            }
            catch (Exception)
            {
               throw;
            }
        }
        public virtual bool Delete(TEntity model)
        {
            try
            {
                if (_context.Entry(model).State == EntityState.Detached)
                {
                    _dbset.Attach(model);
                }
                _dbset.Remove(model);
                return true;
            }
            catch (Exception)
            {
               throw;
            }
        }
        public virtual bool Delete(string id)
        {
            var entity = GetById(id);
            return Delete(entity);
        }
        public bool Save()
        {
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
               throw;
            }
        }

        public bool Any(Expression<Func<TEntity, bool>> predicate = null)
        {
            IQueryable<TEntity> query = _dbset;
            var any = query.Any(predicate);
            return any;
        }
    }
}