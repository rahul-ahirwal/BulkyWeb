using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Bulkyweb.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulkyweb.Areas.Admin.Controllers
{		
    [Area("Admin")]
    [Authorize(Roles = StaticDetails.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            this._unitOfWork = unitOfWork;
            this._webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return View(products);
        }

        public IActionResult Details(int id)
        {
            Product product = _unitOfWork.Product.Get(p => p.Id == id);
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll()
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
            Product product = _unitOfWork.Product.Get(p => p.Id == id);
            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = (product != null) ? product : new Product()
            };
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productvm, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if(productvm.Product.ImageUrl!=null)
                    {
                        var oldImage = Path.Combine(wwwRootPath, productvm.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImage))
                        {
                            System.IO.File.Delete(oldImage);
                        }
                    }

                    using(var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    productvm.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if(productvm.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productvm.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productvm.Product);
                }
                _unitOfWork.Save();
                TempData["success"] = "Product added successfully!!";
                return RedirectToAction("Index");
            }
            else
            {
                productvm.CategoryList = _unitOfWork.Category.GetAll()
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                return View(productvm);
            }
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
                return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (ModelState.IsValid)
            {
                var product = _unitOfWork.Product.Get(p => p.Id == id);
                if (product == null)
                    return NotFound();
                _unitOfWork.Product.Remove(product);
                _unitOfWork.Save();
                TempData["success"] = "Product deleted successfully!!";
                return RedirectToAction("Index");
            }
            else
                return View();
        }
        #region API
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = products });
        }
        #endregion
    }
}
