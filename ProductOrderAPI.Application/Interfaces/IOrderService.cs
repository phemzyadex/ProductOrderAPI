
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Domain.Entities;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.Interfaces
{
    public interface IOrderService
    {
        Task<Order> PlaceOrderAsync(CreateOrderRequest request, Guid userId);// (Order order, Guid userId);
    }
}
