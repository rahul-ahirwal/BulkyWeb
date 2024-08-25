using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Security.Claims;

namespace Bulkyweb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index(string? status = "all")
        {
			IEnumerable<OrderHeader> orderHeader = null;


            if (User.IsInRole(StaticDetails.Role_Admin) || User.IsInRole(StaticDetails.Role_Employee))
			{
                orderHeader = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
			else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                orderHeader = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser")
					.Where(c => c.ApplicationUserId == UserId).ToList();
            }
			if(status !=  "all")
			{
				orderHeader = orderHeader.Where(u => u.OrderStatus.ToLower() == status);
			}
             return View(orderHeader);
		}
		public IActionResult Details(int orderId)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId, includeProperties:"ApplicationUser");
			IEnumerable<OrderDetail> orderDetails = _unitOfWork.OrderDetail.GetAll(includeProperties: "Product").Where(o => o.OrderHeaderId == orderId);

			OrderVM = new OrderVM()
			{ 
				OrderDetail = orderDetails,
				OrderHeader = orderHeader
			};

             return View(OrderVM);
		}
		[HttpPost]
		[Authorize(Roles = StaticDetails.Role_Admin + ", " + StaticDetails.Role_Employee)]
		public IActionResult UpdateOrderDetails()
		{
			var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(h => h.Id == OrderVM.OrderHeader.Id);
			orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

			if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
			if(!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order details updated Successfully!!";
			return RedirectToAction(nameof(Details), new { OrderId = orderHeaderFromDb.Id });
		}

		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + ", " + StaticDetails.Role_Employee)]
        public IActionResult StartProcessing()
		{
			_unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, StaticDetails.OrderStatus_Processing);
			_unitOfWork.Save();
            TempData["Success"] = "Order details updated Successfully!!";
            return RedirectToAction(nameof(Details), new { OrderId = OrderVM.OrderHeader.Id });
		}
		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + ", " + StaticDetails.Role_Employee)]
        public IActionResult ShipOrder()
		{
			if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier) || !string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(h => h.Id == OrderVM.OrderHeader.Id);
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
				orderHeaderFromDb.OrderStatus = StaticDetails.OrderStatus_Shipped;
				orderHeaderFromDb.ShippingDate = DateTime.Now;
				_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                _unitOfWork.Save();
                TempData["Success"] = "Order shipped Successfully!!";
            }
            return RedirectToAction(nameof(Details), new { OrderId = OrderVM.OrderHeader.Id });
		}
		[HttpPost]
        [Authorize(Roles = StaticDetails.Role_Admin + ", " + StaticDetails.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(h => h.Id == OrderVM.OrderHeader.Id);
			if(orderHeaderFromDb.PaymentStatus == StaticDetails.PaymentStatus_Approved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeaderFromDb.PaymentIntentId
				};

				var service = new RefundService();
				Refund refund = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, StaticDetails.OrderStatus_Cancelled, StaticDetails.OrderStatus_Refunded);
			}
			else
			{
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, StaticDetails.OrderStatus_Cancelled, StaticDetails.OrderStatus_Cancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order cancelled Successfully!!";
            return RedirectToAction(nameof(Details), new { OrderId = OrderVM.OrderHeader.Id });
        }

		[ActionName("Details")]
		[HttpPost]
		public IActionResult Details_Pay_Now()
        {

            OrderVM.OrderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == OrderVM.OrderHeader.Id, 
				includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(includeProperties: "Product")
				.Where(o => o.OrderHeaderId == OrderVM.OrderHeader.Id);

            string basePath = "https://localhost:7000";
            var options = new SessionCreateOptions
            {
                SuccessUrl = basePath + $"/Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = basePath + $"/Admin/Order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetail)
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
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id,
                session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader order = _unitOfWork.OrderHeader.Get(o => o.Id == orderHeaderId, includeProperties: "ApplicationUser");
            if (order.PaymentStatus == StaticDetails.PaymentStatus_DelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(order.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, order.OrderStatus, StaticDetails.PaymentStatus_Approved);
                    _unitOfWork.Save();
                }
            }
            return View(orderHeaderId);
        }

    }
}
