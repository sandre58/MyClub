// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MyClub.DatabaseContext.Domain;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : Entity
    {
        private readonly MyTeamup _dbContext;
        private readonly DbSet<TEntity> _dbSet;

        /// <summary>
        /// Initializes a new instance of the GenericRepository{TEntity}.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public GenericRepository(MyTeamup dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<TEntity>();
        }

        #region CREATE

        public virtual void Add(TEntity entity) => _dbSet.Add(entity);

        public virtual void Add(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

        #endregion

        #region READ

        public virtual TEntity? GetById(params object[] keyValues) => _dbSet.Find(keyValues);

        public TEntity? GetFirstOrDefault(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool disableTracking = true) => GetFirstOrDefault(predicate, orderBy, null, disableTracking);

        protected virtual TEntity? GetFirstOrDefault(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool disableTracking = true
        )
        {
            IQueryable<TEntity> query = _dbSet;

            if (disableTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            if (predicate != null)
                query = query.Where(predicate);

            return orderBy != null ? orderBy(query).FirstOrDefault() : query.FirstOrDefault();
        }
        public virtual IQueryable<TEntity> GetAll() => _dbSet;

        public IEnumerable<TEntity> GetMuliple(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool disableTracking = true) => GetMuliple(predicate, orderBy, null, disableTracking);

        protected virtual IEnumerable<TEntity> GetMuliple(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool disableTracking = true
        )
        {
            IQueryable<TEntity> query = _dbSet;

            if (disableTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            if (predicate != null)
                query = query.Where(predicate);

            return orderBy != null ? [.. orderBy(query)] : (IEnumerable<TEntity>)[.. query];
        }

        #endregion

        #region UPDATE
        public virtual void Update(TEntity entity) => _dbSet.Update(entity);

        public virtual void Update(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

        #endregion

        #region DELETE

        public virtual void Delete(object id)
        {
            var entityToDelete = _dbSet.Find(id);
            if (entityToDelete != null)
                _dbSet.Remove(entityToDelete);
        }

        public virtual void Delete(TEntity entityToDelete)
        {
            if (_dbContext.Entry(entityToDelete).State == EntityState.Detached)
                _dbSet.Attach(entityToDelete);

            _dbSet.Remove(entityToDelete);
        }

        public virtual void Delete(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

        #endregion

        #region OTHER

        public virtual int Count(Expression<Func<TEntity, bool>>? predicate = null) => predicate == null ? _dbSet.Count() : _dbSet.Count(predicate);

        public virtual bool Exists(Expression<Func<TEntity, bool>> predicate) => _dbSet.Any(predicate);

        #endregion
    }
}
