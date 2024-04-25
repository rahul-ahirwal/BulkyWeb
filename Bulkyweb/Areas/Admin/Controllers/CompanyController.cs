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
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> companies = _unitOfWork.Company.GetAll().ToList();
            return View(companies);
        }

        public IActionResult Details(int id)
        {
            Company company = _unitOfWork.Company.Get(p => p.Id == id);
            return View(company);
        }

        public IActionResult Upsert(int? id)
        {
            Company company = _unitOfWork.Company.Get(p => p.Id == id);
            return View(company!=null ? company : new Company()); ;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company company)
        {

            if (company.Id == 0)
            {
                _unitOfWork.Company.Add(company);
            }
            else
            {
                _unitOfWork.Company.Update(company);
            }
            _unitOfWork.Save();
            TempData["success"] = "Company added successfully!!";
            return RedirectToAction("Index");

        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            var company = _unitOfWork.Company.Get(p => p.Id == id);
            if (company == null)
                return NotFound();
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (ModelState.IsValid)
            {
                var company = _unitOfWork.Company.Get(p => p.Id == id);
                if (company == null)
                    return NotFound();
                _unitOfWork.Company.Remove(company);
                _unitOfWork.Save();
                TempData["success"] = "Company deleted successfully!!";
                return RedirectToAction("Index");
            }
            else
                return View();
        }
    }
}
