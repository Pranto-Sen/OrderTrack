using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderTrack.API.Data;
using OrderTrack.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrderTrackDbContext _context;

        public OrdersController(OrderTrackDbContext context)
        {
            _context = context;
        }

        // API 01: Create a new order
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder(int productId, string customerName, int quantity)
        {
            try
            {
                // Check if the product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return NotFound("Product not found.");

                // Check if enough stock is available
                if (product.Stock < quantity)
                    return BadRequest("Insufficient stock.");

                // Create the new order
                var order = new Order
                {
                    ProductId = productId,
                    CustomerName = customerName,
                    Quantity = quantity,
                    OrderDate = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                // Update stock
                product.Stock -= quantity;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Order created successfully.", OrderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 02: Update an order's quantity
        [HttpPut("update/{orderId}")]
        public async Task<IActionResult> UpdateOrderQuantity(int orderId, int newQuantity)
        {
            try
            {
                // Fetch the order and related product
                var order = await _context.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return NotFound("Order not found.");

                var product = order.Product;
                var difference = newQuantity - order.Quantity;

                // Check if the new quantity exceeds stock
                if (product.Stock < difference)
                    return BadRequest("Insufficient stock.");

                // Update the order quantity and stock
                order.Quantity = newQuantity;
                product.Stock -= difference;

                await _context.SaveChangesAsync();
                return Ok("Order updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 03: Delete an order
        [HttpDelete("delete/{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return NotFound("Order not found.");

                // Restore stock
                order.Product.Stock += order.Quantity;

                // Remove the order
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return Ok("Order deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        // API 04: Retrieve all orders with product details
        [HttpGet("all-orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.CustomerName,
                        o.Quantity,
                        o.OrderDate,
                        ProductName = o.Product.ProductName,
                        UnitPrice = o.Product.UnitPrice
                    }).ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 05: Summary of total quantity and revenue for each product
        [HttpGet("summary")]
        public async Task<IActionResult> GetProductSummary()
        {
            try
            {
                var summary = await _context.Orders
                    .Include(o => o.Product)
                    .GroupBy(o => new { o.ProductId, o.Product.ProductName, o.Product.UnitPrice })
                    .Select(g => new
                    {
                        g.Key.ProductName,
                        TotalQuantity = g.Sum(o => o.Quantity),
                        TotalRevenue = g.Sum(o => o.Quantity * g.Key.UnitPrice)
                    }).ToListAsync();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 06: Products with stock below a threshold
        [HttpGet("low-stock/{threshold}")]
        public async Task<IActionResult> GetLowStockProducts(int threshold)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Stock < threshold)
                    .Select(p => new
                    {
                        p.ProductName,
                        p.UnitPrice,
                        p.Stock
                    }).ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 07: Top 3 customers by quantity ordered
        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers()
        {
            try
            {
                var topCustomers = await _context.Orders
                    .GroupBy(o => o.CustomerName)
                    .OrderByDescending(g => g.Sum(o => o.Quantity))
                    .Take(3)
                    .Select(g => new
                    {
                        CustomerName = g.Key,
                        TotalQuantity = g.Sum(o => o.Quantity)
                    }).ToListAsync();

                return Ok(topCustomers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // API 08: Products not ordered at all
        [HttpGet("not-ordered")]
        public async Task<IActionResult> GetUnorderedProducts()
        {
            try
            {
                var unorderedProducts = await _context.Products
                    .Where(p => !_context.Orders.Any(o => o.ProductId == p.ProductId))
                    .Select(p => new
                    {
                        p.ProductName,
                        p.UnitPrice,
                        p.Stock
                    }).ToListAsync();

                return Ok(unorderedProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // API 09:

        [HttpPost("bulk-create")]
        public async Task<IActionResult> CreateBulkOrders([FromBody] List<Order> orderRequests)
        {
            // Validate input
            if (orderRequests == null || !orderRequests.Any())
                return BadRequest("Order list cannot be empty.");

            // Start a database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Loop through each order request
                foreach (var orderRequest in orderRequests)
                {
                    // Find the product associated with the order
                    var product = await _context.Products.FindAsync(orderRequest.ProductId);
                    if (product == null)
                    {
                        throw new Exception($"Product with ID {orderRequest.ProductId} not found.");
                    }

                    // Check if sufficient stock is available
                    if (product.Stock < orderRequest.Quantity)
                    {
                        throw new Exception($"Insufficient stock for Product ID {orderRequest.ProductId}. Requested: {orderRequest.Quantity}, Available: {product.Stock}");
                    }

                    // Create a new Order object
                    var order = new Order
                    {
                        ProductId = orderRequest.ProductId,
                        CustomerName = orderRequest.CustomerName,
                        Quantity = orderRequest.Quantity,
                        OrderDate = DateTime.UtcNow
                    };

                    // Add the order to the database and reduce product stock
                    _context.Orders.Add(order);
                    product.Stock -= orderRequest.Quantity;
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                // Return success message
                return Ok("Bulk orders created successfully.");
            }
            catch (Exception ex)
            {
                // Rollback the transaction if any error occurs
                await transaction.RollbackAsync();
                return BadRequest($"Transaction failed: {ex.Message}");
            }
        }

    }
}
