using Bulkyweb.DataAccess.Data;
using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulkyweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Category> categories = _unitOfWork.Category.GetAll().ToList();
            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (category.Name == category.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "Display Order and name cannot be same");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(category);
                _unitOfWork.Save();
                TempData["success"] = "Category added successfully!!";
                return RedirectToAction("Index");
            }
            else
                return View();
        }
        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            //var category = _context.Categories.Where(x => x.Id == id).FirstOrDefault(); //Works with all kinds of fields
            //var category = _context.Categories.FirstOrDefault(x => x.Id == id); //Works with all kinds of fields
            var category = _unitOfWork.Category.Get(c => c.Id == id); //Works only with Primary key
            if (category == null)
                return NotFound();
            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(category);
                _unitOfWork.Save();
                TempData["success"] = "Category updated successfully!!";
                return RedirectToAction("Index");
            }
            else
                return View();
        }
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            var category = _unitOfWork.Category.Get(c => c.Id == id);
            if (category == null)
                return NotFound();
            return View(category);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int id)
        {
            if (ModelState.IsValid)
            {
                var category = _unitOfWork.Category.Get(c => c.Id == id);
                if (category == null)
                    return NotFound();
                _unitOfWork.Category.Remove(category);
                _unitOfWork.Save();
                TempData["success"] = "Category deleted successfully!!";
                return RedirectToAction("Index");
            }
            else
                return View();
        }
    }
}

