using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Blagodat.Models
{
    public static class ModelExtensions
    {
        
        public static string GetServicesList(this Order order)
        {
            if (order == null || order.OrderServices == null || !order.OrderServices.Any())
                return string.Empty;
                
            return string.Join(", ", order.OrderServices
                .Where(os => os.Service != null)
                .Select(os => os.Service.Name));
        }
        
       
        public static string GetServicesString(this Order order, User21Context context)
        {
            try
            {
                var orderServices = context.OrderServices
                    .Where(os => os.OrderId == order.OrderId)
                    .ToList();

                var serviceIds = orderServices.Select(os => os.ServiceId).ToList();
                
                var services = context.Services
                    .Where(s => serviceIds.Contains(s.ServiceId))
                    .ToList();
                    
                return string.Join(", ", services.Select(s => s.Name));
            }
            catch
            {
                return string.Empty;
            }
        }
        
       
        public static decimal CalculateTotalCost(this OrderService orderService, User21Context context)
        {
            try
            {
                var service = context.Services.FirstOrDefault(s => s.ServiceId == orderService.ServiceId);
                if (service != null)
                {
                    return service.CostPerHour * orderService.Hours;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        
       
        public class OrderServiceViewModel
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public int ServiceId { get; set; }
            public Service Service { get; set; }
            public int Hours { get; set; }
            public decimal TotalCost { get; set; }
        }
        
       
        public static OrderServiceViewModel ToViewModel(this OrderService orderService, User21Context context)
        {
            var service = context.Services.FirstOrDefault(s => s.ServiceId == orderService.ServiceId);
            
            return new OrderServiceViewModel
            {
                Id = orderService.Id,
                OrderId = orderService.OrderId,
                ServiceId = orderService.ServiceId,
                Service = service,
                Hours = orderService.Hours,
                TotalCost = orderService.CalculateTotalCost(context)
            };
        }
        
        
        public static List<OrderServiceViewModel> ToViewModels(this IEnumerable<OrderService> orderServices, User21Context context)
        {
            return orderServices.Select(os => os.ToViewModel(context)).ToList();
        }
    }
}
