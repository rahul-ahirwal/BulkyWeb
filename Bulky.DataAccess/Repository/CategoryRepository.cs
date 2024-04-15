﻿using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulkyweb.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            this._context = context;
        }

        public void Update(Category category)
        {
            _context.Categories.Update(category);
        }
    }
}
