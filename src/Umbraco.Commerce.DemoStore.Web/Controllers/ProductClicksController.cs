// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Configuration;
// using System;
// using System.Collections.Generic;
// using System.Data.SqlClient;
// using System.Threading.Tasks;
// using Microsoft.Data.SqlClient;
//
// namespace YourNamespace.Controllers
// {
//     [ApiController]
//     [Route("/umbraco/delivery/api/[controller]")]
//     public class ProductClicksController : ControllerBase
//     {
//         private readonly string _connectionString;
//
//         public ProductClicksController(IConfiguration configuration)
//         {
//             _connectionString = configuration.GetConnectionString("umbracoDbDSN");
//         }
//
//         [HttpPost("increment")]
//         public async Task<IActionResult> IncrementClick([FromBody] ProductClickModel model)
//         {
//             Console.WriteLine($"Incrementing clicks for ProductId: {model.ProductId}");
//             using var connection = new SqlConnection(_connectionString);
//             await connection.OpenAsync();
//
//             var existsCmd = new SqlCommand("SELECT COUNT(*) FROM ProductClicks WHERE ProductId = @ProductId", connection);
//             existsCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
//
//             int exists = (int)await existsCmd.ExecuteScalarAsync();
//
//             if (exists > 0)
//             {
//                 var updateCmd = new SqlCommand("UPDATE ProductClicks SET Clicks = Clicks + 1 WHERE ProductId = @ProductId", connection);
//                 updateCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
//                 await updateCmd.ExecuteNonQueryAsync();
//             }
//             else
//             {
//                 var insertCmd = new SqlCommand(
//                     "INSERT INTO ProductClicks (ProductId, Title, Clicks) VALUES (@ProductId, @Title, 1)",
//                     connection
//                 );
//                 insertCmd.Parameters.AddWithValue("@ProductId", model.ProductId);
//                 insertCmd.Parameters.AddWithValue("@Title", model.Title);
//                 await insertCmd.ExecuteNonQueryAsync();
//             }
//
//             return Ok(new { status = "success" });
//         }
//
//         [HttpGet("top")]
//         public async Task<IActionResult> GetTopProducts([FromQuery] int count = 4)
//         {
//             var topProducts = new List<ProductClickModel>();
//
//             using var connection = new SqlConnection(_connectionString);
//             await connection.OpenAsync();
//
//             var selectCmd = new SqlCommand("SELECT TOP (@Count) ProductId, Title, Clicks FROM ProductClicks ORDER BY Clicks DESC", connection);
//             selectCmd.Parameters.AddWithValue("@Count", count);
//
//             using var reader = await selectCmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 topProducts.Add(new ProductClickModel
//                 {
//                     ProductId = reader.GetGuid(0),
//                     Title = reader.GetString(1),
//                     Clicks = reader.GetInt32(2)
//                 });
//             }
//
//             return Ok(topProducts);
//         }
//     }
//
//     public class ProductClickModel
//     {
//         public Guid ProductId { get; set; }
//         public string Title { get; set; }
//         public int Clicks { get; set; }
//     }
// }

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
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int top = 4, [FromQuery] string period = "today")
        {
            try
            {
                var topProducts = new List<ProductClickModel>();

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (period.ToLower() == "today")
                {
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

                    using var todayReader = await todayCmd.ExecuteReaderAsync();
                    while (await todayReader.ReadAsync())
                    {
                        topProducts.Add(new ProductClickModel
                        {
                            ProductId = todayReader.GetGuid(0),
                            Title = todayReader.GetString(1),
                            Clicks = todayReader.GetInt32(2)
                        });
                    }
                    todayReader.Close();

                    
                    if (topProducts.Count < top)
                    {
                        var yesterdayStart = DateTime.Today.AddDays(-1);
                        var yesterdayEnd = DateTime.Today;
                        var remainingCount = top - topProducts.Count;

                        string yesterdayQuery = @"
                            SELECT TOP (@Remaining) ProductId, Title, SUM(Clicks) as TotalClicks 
                            FROM ProductClicks 
                            WHERE ClickDate >= @YesterdayStart AND ClickDate < @YesterdayEnd";

                        // Excluzi produsele care sunt deja în lista de astăzi
                        if (topProducts.Any())
                        {
                            var excludeIds = string.Join("','", topProducts.Select(p => p.ProductId.ToString()));
                            yesterdayQuery += $" AND ProductId NOT IN ('{excludeIds}')";
                        }

                        yesterdayQuery += @"
                            GROUP BY ProductId, Title
                            ORDER BY TotalClicks DESC";

                        var yesterdayCmd = new SqlCommand(yesterdayQuery, connection);
                        yesterdayCmd.Parameters.AddWithValue("@Remaining", remainingCount);
                        yesterdayCmd.Parameters.AddWithValue("@YesterdayStart", yesterdayStart);
                        yesterdayCmd.Parameters.AddWithValue("@YesterdayEnd", yesterdayEnd);

                        using var yesterdayReader = await yesterdayCmd.ExecuteReaderAsync();
                        while (await yesterdayReader.ReadAsync())
                        {
                            topProducts.Add(new ProductClickModel
                            {
                                ProductId = yesterdayReader.GetGuid(0),
                                Title = yesterdayReader.GetString(1),
                                Clicks = yesterdayReader.GetInt32(2)
                            });
                        }
                        yesterdayReader.Close();
                    }

                    // Dacă încă nu sunt suficiente, completează cu toate produsele (all-time)
                    if (topProducts.Count < top)
                    {
                        var remainingCount = top - topProducts.Count;
                        
                        string allTimeQuery = @"
                            SELECT TOP (@Remaining) ProductId, Title, SUM(Clicks) as TotalClicks 
                            FROM ProductClicks";

                        if (topProducts.Any())
                        {
                            var excludeIds = string.Join("','", topProducts.Select(p => p.ProductId.ToString()));
                            allTimeQuery += $" WHERE ProductId NOT IN ('{excludeIds}')";
                        }

                        allTimeQuery += @"
                            GROUP BY ProductId, Title
                            ORDER BY TotalClicks DESC";

                        var allTimeCmd = new SqlCommand(allTimeQuery, connection);
                        allTimeCmd.Parameters.AddWithValue("@Remaining", remainingCount);

                        using var allTimeReader = await allTimeCmd.ExecuteReaderAsync();
                        while (await allTimeReader.ReadAsync())
                        {
                            topProducts.Add(new ProductClickModel
                            {
                                ProductId = allTimeReader.GetGuid(0),
                                Title = allTimeReader.GetString(1),
                                Clicks = allTimeReader.GetInt32(2)
                            });
                        }
                    }
                }
                else // alltime
                {
                    // Produsele cele mai clickate DE LA ÎNCEPUT
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
                }

                return Ok(topProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting top products: {ex.Message}");
                return BadRequest(new { status = "error", message = ex.Message });
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