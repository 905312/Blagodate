using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blagodat.Models
{
    
    public static class OrderServiceExtensions
    {
        
        public static decimal TotalCost(this OrderService orderService)
        {
            if (orderService == null || orderService.Service == null)
                return 0;
            
            return orderService.Cost > 0 
                ? orderService.Cost 
                : orderService.Service.CostPerHour * orderService.Hours;
        }
    }
}
