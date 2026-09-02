using Microsoft.AspNetCore.Mvc;
using SecureWebApi.DTOs;
using Microsoft.EntityFrameworkCore;
using SecureWebApi.Data;
using SecureWebApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace SecureWebApi.Controllers
{
    /// <summary>
    /// Provides CRUD operations for products.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public ProductsController(ApplicationDbContext context)
        {
            this.context = context;
        }



        /// <summary>
        /// Gets a paginated list of products.
        /// </summary>
        /// <param name="pageNumber">Page number. Default is 1.</param>
        /// <param name="pageSize">Number of products per page. Maximum is 100.</param>
        /// <returns>A paginated list of products.</returns>
        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetProducts(
     int pageNumber = 1,
     int pageSize = 10)
        {
            // Prevent invalid page number
            pageNumber = Math.Max(pageNumber, 1);

            // Maximum 100 records per request
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = context.Products
                .AsNoTracking()
                .OrderBy(x => x.Id);

            var totalRecords = await query.CountAsync();

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            var response = new PagedResponse<Product>
            {
                Data = products,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };

            return Ok(response);
        }


        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <returns>The requested product.</returns>
        // GET: api/products/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            return Ok(product);
        }


        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <param name="request">Product creation details.</param>
        /// <returns>The newly created product.</returns>
        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct(
    CreateProductRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                Category = request.Category
            };

            context.Products.Add(product);

            await context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = product.Id},
                product);
        }

        /// <summary>
        /// Updates an existing product.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <param name="request">Updated product details.</param>
        /// <returns>The updated product.</returns>
        // PUT: api/products/1
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(
     int id,
     UpdateProductRequest request)
        {
            var product = await context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.Category = request.Category;

            await context.SaveChangesAsync();

            return Ok(product);
        }


        /// <summary>
        /// Deletes a product.
        /// </summary>
        /// <param name="id">Product ID.</param>
        /// <returns>No content when deletion is successful.</returns>
        // DELETE: api/products/1
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound(new
                {
                    message = "Product not found."
                });
            }

            context.Products.Remove(product);

            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}