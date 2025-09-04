using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<Order>> PlaceOrderAsync(CreateOrderRequest request, Guid userId);
    }
}
