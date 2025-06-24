using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("/umbraco/delivery/api/[controller]")]
    public class ProductClicksController : ControllerBase
    {
        private readonly string _connectionString;

        public ProductClicksController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("umbracoDbDSN");
        }

        [HttpPost("increment")]
        public async Task<IActionResult> IncrementClick([FromBody] ProductClickModel model)
        {
            Console.WriteLine($"Incrementing clicks for ProductId: {model.ProductId}");
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var existsCmd = new SqlCommand("SELECT COUNT(*) FROM ProductClicks WHERE ProductId = @ProductId", connection);
            existsCmd.Parameters.AddWithValue("@ProductId", model.ProductId);

            int exists = (int)await existsCmd.ExecuteScalarAsync();

            if (exists > 0)
            {
                var updateCmd = new SqlCommand("UPDATE ProductClicks SET Clicks = Clicks + 1 WHERE ProductId = @ProductId", connection);
                updateCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                var insertCmd = new SqlCommand(
                    "INSERT INTO ProductClicks (ProductId, Title, Clicks) VALUES (@ProductId, @Title, 1)",
                    connection
                );
                insertCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                insertCmd.Parameters.AddWithValue("@Title", model.Title);
                await insertCmd.ExecuteNonQueryAsync();
            }

            return Ok(new { status = "success" });
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int count = 4)
        {
            var topProducts = new List<ProductClickModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var selectCmd = new SqlCommand("SELECT TOP (@Count) ProductId, Title, Clicks FROM ProductClicks ORDER BY Clicks DESC", connection);
            selectCmd.Parameters.AddWithValue("@Count", count);

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                topProducts.Add(new ProductClickModel
                {
                    ProductId = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Clicks = reader.GetInt32(2)
                });
            }

            return Ok(topProducts);
        }
    }

    public class ProductClickModel
    {
        public Guid ProductId { get; set; }
        public string Title { get; set; }
        public int Clicks { get; set; }
    }
}
