﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Linq;
using Drive.DataAccess.Interfaces;

namespace Drive.DataAccess.Repositories
{
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly DbContext _context;
        private IDbSet<T> _entities;

        public Repository(DbContext context)
        {
            _context = context;
        }

        public T GetById(int id)
        {
            return Query.First(x => x.Id == id);
        }
        public async Task<T> GetByIdAsync(int id)
        {
            return await Query.SingleOrDefaultAsync(i => i.Id == id);
        }

        public async Task<T> GetByIdDeletedAsync(int id)
        {
            return await Deleted.SingleOrDefaultAsync(i => i.Id == id);
        }

        public void Create(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }
                Entities.Add(entity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Update(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }
                _context.Entry(entity).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Delete(int id)
        {
            try
            {
                var entity = GetById(id);
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(id));
                }

                entity.IsDeleted = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task ForceDelete(int id)
        {
            try
            {
                var entity = await GetByIdDeletedAsync(id);
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(id));
                }
                Entities.Remove(entity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task Restore(int id)
        {
            try
            {
                var entity = await GetByIdDeletedAsync(id);
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(id));
                }
                entity.IsDeleted = false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<T> GetAll()
        {
            return Query.ToList();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await Query.ToListAsync();
        }

        protected IDbSet<T> Entities => _entities ?? (_entities = _context.Set<T>());

        public IQueryable<T> Query => Entities.Where(x => !x.IsDeleted);
        public IQueryable<T> Deleted => Entities.Where(x => x.IsDeleted);

    }

}



