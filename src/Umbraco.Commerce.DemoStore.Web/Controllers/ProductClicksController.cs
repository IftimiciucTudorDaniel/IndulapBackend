using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Linq;

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
            try
            {
                if (model == null || model.ProductId == Guid.Empty)
                {
                    return BadRequest(new { status = "error", message = "ProductId este obligatoriu" });
                }

                Console.WriteLine($"Incrementing clicks for ProductId: {model.ProductId}");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var todayStart = DateTime.Today;
                var todayEnd = DateTime.Today.AddDays(1);

                // Verifică dacă există un click pentru acest produs ASTĂZI
                var existsTodayCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ProductClicks WHERE ProductId = @ProductId AND ClickDate >= @TodayStart AND ClickDate < @TodayEnd", 
                    connection);
                existsTodayCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                existsTodayCmd.Parameters.AddWithValue("@TodayStart", todayStart);
                existsTodayCmd.Parameters.AddWithValue("@TodayEnd", todayEnd);

                int existsToday = (int)await existsTodayCmd.ExecuteScalarAsync();

                if (existsToday > 0)
                {
                    // Update click-ul de astăzi
                    var updateCmd = new SqlCommand(
                        "UPDATE ProductClicks SET Clicks = Clicks + 1 WHERE ProductId = @ProductId AND ClickDate >= @TodayStart AND ClickDate < @TodayEnd", 
                        connection);
                    updateCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    updateCmd.Parameters.AddWithValue("@TodayStart", todayStart);
                    updateCmd.Parameters.AddWithValue("@TodayEnd", todayEnd);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Inserează un nou click pentru astăzi
                    var insertCmd = new SqlCommand(
                        "INSERT INTO ProductClicks (ProductId, Title, Clicks, ClickDate) VALUES (@ProductId, @Title, 1, @ClickDate)",
                        connection
                    );
                    insertCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    insertCmd.Parameters.AddWithValue("@Title", model.Title ?? "");
                    insertCmd.Parameters.AddWithValue("@ClickDate", DateTime.Now);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error incrementing clicks: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodayTopProducts([FromQuery] int top = 4)
        {
            try
            {
                var topProducts = new List<ProductClickModel>();

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var todayStart = DateTime.Today;
                var todayEnd = DateTime.Today.AddDays(1);

                var todayCmd = new SqlCommand(@"
                    SELECT TOP (@Top) ProductId, Title, SUM(Clicks) as TotalClicks 
                    FROM ProductClicks 
                    WHERE ClickDate >= @TodayStart AND ClickDate < @TodayEnd
                    GROUP BY ProductId, Title
                    ORDER BY TotalClicks DESC", connection);
                
                todayCmd.Parameters.AddWithValue("@Top", top);
                todayCmd.Parameters.AddWithValue("@TodayStart", todayStart);
                todayCmd.Parameters.AddWithValue("@TodayEnd", todayEnd);

                using var reader = await todayCmd.ExecuteReaderAsync();
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting today's top products: {ex.Message}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("alltime")]
        public async Task<IActionResult> GetAllTimeTopProducts([FromQuery] int top = 4)
        {
            try
            {
                var topProducts = new List<ProductClickModel>();

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var allTimeCmd = new SqlCommand(@"
                    SELECT TOP (@Top) ProductId, Title, SUM(Clicks) as TotalClicks 
                    FROM ProductClicks 
                    GROUP BY ProductId, Title
                    ORDER BY TotalClicks DESC", connection);
                
                allTimeCmd.Parameters.AddWithValue("@Top", top);

                using var reader = await allTimeCmd.ExecuteReaderAsync();
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all-time top products: {ex.Message}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupDeletedProducts([FromBody] List<Guid> activeProductIds)
        {
            try
            {
                if (activeProductIds == null || !activeProductIds.Any())
                {
                    return BadRequest(new { status = "error", message = "Lista de produse active este obligatorie" });
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Creează lista de parametri pentru query
                var parameterNames = activeProductIds.Select((id, index) => $"@ProductId{index}").ToArray();
                var inClause = string.Join(",", parameterNames);

                var cleanupCmd = new SqlCommand($"DELETE FROM ProductClicks WHERE ProductId NOT IN ({inClause})", connection);
                
                // Adaugă parametrii
                for (int i = 0; i < activeProductIds.Count; i++)
                {
                    cleanupCmd.Parameters.AddWithValue($"@ProductId{i}", activeProductIds[i]);
                }

                int deletedRows = await cleanupCmd.ExecuteNonQueryAsync();

                return Ok(new { status = "success", deletedRows = deletedRows });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up deleted products: {ex.Message}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetClickStats()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var statsCmd = new SqlCommand(@"
                    SELECT 
                        COUNT(DISTINCT ProductId) as UniqueProducts,
                        SUM(Clicks) as TotalClicks,
                        COUNT(*) as TotalRecords
                    FROM ProductClicks", connection);

                using var reader = await statsCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new 
                    {
                        uniqueProducts = reader.GetInt32(0),
                        totalClicks = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        totalRecords = reader.GetInt32(2)
                    });
                }

                return Ok(new { uniqueProducts = 0, totalClicks = 0, totalRecords = 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting stats: {ex.Message}");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }

    public class ProductClickModel
    {
        public Guid ProductId { get; set; }
        public string Title { get; set; }
        public int Clicks { get; set; }
        public DateTime ClickDate { get; set; }
    }
}