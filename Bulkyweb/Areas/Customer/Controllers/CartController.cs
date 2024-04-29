using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace Bulkyweb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		public CartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			IEnumerable<ShoppingCart> shoppingCart = _unitOfWork.ShoppingCart.GetAll(includeProperties: "Product").Where(i => i.ApplicationUserId == UserId).ToList();
			double orderTotal = 0;

            HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            foreach (var cartItem in shoppingCart)
			{
				orderTotal += GetPriceBasedOnQuantity(cartItem) * cartItem.Count;
			}

			ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
			{
				CartItems = shoppingCart,
				OrderHeader = new()
			};
			shoppingCartVM.OrderHeader.OrderTotal = orderTotal;
			return View(shoppingCartVM);
		}
		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			IEnumerable<ShoppingCart> shoppingCart = _unitOfWork.ShoppingCart.GetAll(includeProperties: "Product")
				.Where(i => i.ApplicationUserId == UserId).ToList();
			double orderTotal = 0;

			foreach (var cartItem in shoppingCart)
			{
				orderTotal += GetPriceBasedOnQuantity(cartItem) * cartItem.Count;
			}
			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(au => au.Id == UserId);
			ShoppingCartVM shoppingCartVM = new ShoppingCartVM
			{
				CartItems = shoppingCart,
				OrderHeader = new OrderHeader
				{
					ApplicationUserId = UserId,
					OrderTotal = orderTotal,
					ApplicationUser = applicationUser,
					Name = applicationUser.Name,
					StreetAddress = applicationUser.StreetAddress,
					City = applicationUser.City,
					State = applicationUser.State,
					PostalCode = applicationUser.PostalCode,
					PhoneNumber = applicationUser.PhoneNumber
				}
			};
			return View(shoppingCartVM);
		}
		[HttpPost]
		public IActionResult Summary(ShoppingCartVM shoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(au => au.Id == UserId);
			IEnumerable<ShoppingCart> shoppingCart = _unitOfWork.ShoppingCart.GetAll(includeProperties: "Product")
				.Where(i => i.ApplicationUserId == UserId).ToList();
			double orderTotal = 0;
			foreach (var cartItem in shoppingCart)
			{
				orderTotal += GetPriceBasedOnQuantity(cartItem) * cartItem.Count;
			}
			shoppingCartVM.OrderHeader.OrderTotal = orderTotal;
			shoppingCartVM.OrderHeader.ApplicationUserId = UserId;
			shoppingCartVM.CartItems = shoppingCart;
			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				shoppingCartVM.OrderHeader.OrderStatus = StaticDetails.OrderStatus_Pending;
				shoppingCartVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatus_Pending;
			}
			else
			{
				shoppingCartVM.OrderHeader.OrderStatus = StaticDetails.OrderStatus_Approved;
				shoppingCartVM.OrderHeader.PaymentStatus = StaticDetails.PaymentStatus_DelayedPayment;
				shoppingCartVM.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

			_unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			foreach (var order in shoppingCartVM.CartItems)
			{
				OrderDetail orderDetail = new OrderDetail
				{
					ProductId = order.ProductId,
					Price = orderTotal,
					Count = order.Count,
					OrderHeaderId = shoppingCartVM.OrderHeader.Id,
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				string basePath = "https://localhost:7000";
				var options = new SessionCreateOptions
				{
					SuccessUrl = basePath + $"/Customer/Cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
					CancelUrl = basePath + $"/Customer/Cart/Index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach(var item in shoppingCartVM.CartItems)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Product.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentID(shoppingCartVM.OrderHeader.Id, 
					session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}
			return RedirectToAction(nameof(OrderConfirmation), new { id = shoppingCartVM.OrderHeader.Id });
		}

		public IActionResult OrderConfirmation(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(au => au.Id == UserId);
			OrderHeader order = new OrderHeader();
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
			    order = _unitOfWork.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser");
				var service = new SessionService();
				Session session = service.Get(order.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, StaticDetails.OrderStatus_Approved, StaticDetails.PaymentStatus_Approved);
					_unitOfWork.Save();
				} 
			}
			List<ShoppingCart> shoppingCart = _unitOfWork.ShoppingCart.GetAll()
				.Where(s => s.ApplicationUserId == order.ApplicationUserId).ToList();
			_unitOfWork.ShoppingCart.RemoveRange(shoppingCart);
			_unitOfWork.Save();
			return View(id);
		}

		public IActionResult IncreaseCount(int id)
		{
			ShoppingCart item = _unitOfWork.ShoppingCart.Get(sc => sc.Id == id);
			item.Count++;
			_unitOfWork.ShoppingCart.Update(item);
			_unitOfWork.Save();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            return RedirectToAction(nameof(Index));
		}

		public IActionResult DecreaseCount(int id)
		{
			ShoppingCart item = _unitOfWork.ShoppingCart.Get(sc => sc.Id == id);
			if (item.Count <= 1)
			{
				_unitOfWork.ShoppingCart.Remove(item);
			}
			else
			{
				item.Count--;
				_unitOfWork.ShoppingCart.Update(item);
			}
			_unitOfWork.Save();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            return RedirectToAction(nameof(Index));
		}

		public IActionResult RemoveItem(int id)
		{
			ShoppingCart item = _unitOfWork.ShoppingCart.Get(sc => sc.Id == id);
			_unitOfWork.ShoppingCart.Remove(item);
			_unitOfWork.Save();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            HttpContext.Session.SetInt32(StaticDetails.SessionCart, _unitOfWork.ShoppingCart
                .GetAll().Where(u => u.ApplicationUserId == UserId).Count());
            return RedirectToAction(nameof(Index));
		}

		#region Private methods
		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count > 100)
			{
				return shoppingCart.Product.Price100;
			}
			else if (shoppingCart.Count > 50)
			{
				return shoppingCart.Product.Price50;
			}
			else
			{
				return shoppingCart.Product.Price;
			}
		}
		#endregion
	}
}
