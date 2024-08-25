using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulkyweb.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _context;
        public CompanyRepository(ApplicationDbContext context) : base(context)
        {
            this._context = context;
        }

        public void Update(Company company)
        {
            _context.Companies.Update(company);
        }
    }
}
