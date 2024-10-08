using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Utility;
using Bulkyweb.Areas.Admin.Controllers;
using Bulkyweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Security.Claims;

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
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = "";
            if (claimsIdentity.IsAuthenticated == true)
            {

                UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            }
            IEnumerable <Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(products);
        }
        public IActionResult Details(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCart cart = new ShoppingCart
            {
                Product = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category"),
                Count = 1,
                ProductId = id
            };
            var existingItem = _unitOfWork.ShoppingCart.Get(c => (c.ApplicationUserId == UserId) && (c.ProductId == id));
            if(existingItem != null) 
            {
                cart.Count = existingItem.Count;
            }
            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cart.ApplicationUserId = UserId;
            var existingItem = _unitOfWork.ShoppingCart.Get(c => (c.ApplicationUserId == UserId) && (c.ProductId == cart.ProductId));
            if (existingItem == null)
            {
                cart.Id = 0;
                _unitOfWork.ShoppingCart.Add(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            }
            else
            {
                cart.Id = existingItem.Id;
                _unitOfWork.ShoppingCart.Update(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            }
            TempData["success"] = "Product added successfully!!";
            return RedirectToAction(nameof(Index));
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
