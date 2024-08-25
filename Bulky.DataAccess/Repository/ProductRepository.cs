using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulkyweb.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            this._context = context;
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }
    }
}
