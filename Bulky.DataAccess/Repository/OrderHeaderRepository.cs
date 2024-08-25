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
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
            this._context = context;
        }

        public void Update(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Update(orderHeader);
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			var orderFromDb = _context.OrderHeaders.Where(o => o.Id == id).FirstOrDefault();
            if(orderFromDb != null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if(!string.IsNullOrEmpty(paymentStatus)) 
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
			}
			_context.OrderHeaders.Update(orderFromDb);
		}

		public void UpdateStripePaymentID(int id, string sessionId, string paymentIntentId)
		{
            var orderFromDb = _context.OrderHeaders.FirstOrDefault(o => o.Id == id);
			if(!string.IsNullOrEmpty(sessionId))
            {
                orderFromDb.SessionId = sessionId;
            }
            if(!string.IsNullOrEmpty(paymentIntentId))
            {
                orderFromDb.PaymentIntentId = paymentIntentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }
            //_context.OrderHeaders.Update(orderFromDb);
		}
	}
}
