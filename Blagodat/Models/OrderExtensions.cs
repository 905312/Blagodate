using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blagodat.Models
{
    
    public static class OrderExtensions
    {
        
        public static string ServicesList(this Order order)
        {
            if (order == null || order.OrderServices == null || !order.OrderServices.Any())
                return string.Empty;
                
            return string.Join(", ", order.OrderServices
                .Where(os => os.Service != null)
                .Select(os => os.Service.Name));
        }
    }
}
