using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulkyweb.Areas.Admin.Controllers;
using Bulkyweb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bulkyweb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            this._unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties : "Category");
            return View(products);
        }
        public IActionResult Details(int id)
        {
            Product product = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category");
            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
