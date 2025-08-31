using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using InDulap.Data;
using InDulap.Models;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Cms.Web.Common.Controllers;
using Exception = System.Exception;
using InDulap.Web.Services;

namespace InDulap.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductClicksController : UmbracoApiController
    {

        #region Constructor

        public ProductClicksController(IEFCoreScopeProvider<InDulapContext> efCoreScopeProvider,
                                       RedisService redisService)
        {
            _efCoreScopeProvider = efCoreScopeProvider;
            _redisService = redisService;
        }

        #endregion

        #region Properties

        private readonly IEFCoreScopeProvider<InDulapContext> _efCoreScopeProvider;
        private readonly RedisService _redisService;

        #endregion

        #region Public Methods

        /// <summary>
        /// Increment the click count for a product based on ProductId and current date
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("increment")]
        public async Task<IActionResult> IncrementClick([FromBody] ProductClickModel model)
        {
            if (model == null || model.ProductId == Guid.Empty)
            {
                return BadRequest(new { status = "error", message = "ProductId este obligatoriu" });
            }
            
            using IEfCoreScope<InDulapContext> scope = _efCoreScopeProvider.CreateScope();
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);
            var result = await scope.ExecuteWithContextAsync<IActionResult>(async db =>
            {
                try
                {
                    Console.WriteLine($"Incrementing clicks for ProductId: {model.ProductId}");
                    var productClick = await db.ProductClicks.FirstOrDefaultAsync(pc => pc.ProductId == model.ProductId 
                        && pc.ClickDate >= todayStart 
                        && pc.ClickDate < todayEnd);
                    if (productClick != null)
                    {
                        productClick.Clicks += 1;
                    }
                    else
                    {
                        db.ProductClicks.Add(new ProductClickModel
                        {
                            ProductId = model.ProductId,
                            Title = model.Title ?? "",
                            Clicks = 1,
                            ClickDate = DateTime.Now
                        });
                    }
                    await db.SaveChangesAsync();
                    scope.Complete();
                    return Ok(new { status = "success" });
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error incrementing clicks: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    return StatusCode(500, new { status = "error", message = ex.Message });
                }
            });
            return result;
        }

        /// <summary>
        /// Return the top products of today based on clicks, cached for 24 hours
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        [HttpGet("today")]
        public async Task<IActionResult> GetTodayTopProducts([FromQuery] int top = 4)
        {
            var cacheKey = $"TOP_PRODUCTS_TODAY_{top}";
            var cached = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = System.Text.Json.JsonSerializer.Deserialize<List<ProductClickModel>>(cached);
                return Ok(cachedResult);
            }
            using IEfCoreScope<InDulapContext> scope = _efCoreScopeProvider.CreateScope();
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);
            var result = await scope.ExecuteWithContextAsync<IActionResult>(async db =>
            {
                try
                {
                    var topProducts = await db.ProductClicks
                        .Where(pc => pc.ClickDate >= todayStart && pc.ClickDate < todayEnd)
                        .GroupBy(pc => new { pc.ProductId, pc.Title })
                        .Select(g => new ProductClickModel
                        {
                            ProductId = g.Key.ProductId,
                            Title = g.Key.Title,
                            Clicks = g.Sum(pc => pc.Clicks)
                        })
                        .OrderByDescending(pc => pc.Clicks)
                        .Take(top)
                        .ToListAsync();
                    scope.Complete();
                    await _redisService.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(topProducts), TimeSpan.FromHours(24));
                    return Ok(topProducts);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error getting today's top products: {ex.Message}");
                    return StatusCode(500, new { status = "error", message = ex.Message });

                }
            });
            return result;
        }

        /// <summary>
        /// Return the top products of all time based on clicks, cached for 30 minutes
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        [HttpGet("alltime")]
        public async Task<IActionResult> GetAllTimeTopProducts([FromQuery] int top = 4)
        {
            var cacheKey = $"TOP_PRODUCTS_ALLTIME_{top}";
            var cached = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = System.Text.Json.JsonSerializer.Deserialize<List<ProductClickModel>>(cached);
                return Ok(cachedResult);
            }
            using IEfCoreScope<InDulapContext> scope = _efCoreScopeProvider.CreateScope();

            var result = await scope.ExecuteWithContextAsync<IActionResult>(async db =>
            {
                try
                {
                    var topProducts = await db.ProductClicks
                        .GroupBy(pc => new { pc.ProductId, pc.Title })
                        .Select(g => new ProductClickModel
                        {
                            ProductId = g.Key.ProductId,
                            Title = g.Key.Title,
                            Clicks = g.Sum(pc => pc.Clicks)
                        })
                        .OrderByDescending(pc => pc.Clicks)
                        .Take(top)
                        .ToListAsync();
                    scope.Complete();
                    await _redisService.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(topProducts), TimeSpan.FromMinutes(30));
                    return Ok(topProducts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting all-time top products: {ex.Message}");
                    return StatusCode(500, new { status = "error", message = ex.Message });
                }
            });
            return result;
        }

        /// <summary>
        /// Cleanup entries for products that no longer exist in the main product catalog
        /// </summary>
        /// <param name="activeProductIds"></param>
        /// <returns></returns>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupDeletedProducts([FromBody] List<Guid> activeProductIds)
        {
            if (activeProductIds == null || !activeProductIds.Any())
            {
                return BadRequest(new { status = "error", message = "Lista de produse active este obligatorie" });
            }
            using IEfCoreScope<InDulapContext> scope = _efCoreScopeProvider.CreateScope();
            var result = await scope.ExecuteWithContextAsync<IActionResult>(async db =>
            {
                try
                {
                    var productsToDelete = db.ProductClicks
                        .Where(pc => !activeProductIds.Contains(pc.ProductId))
                        .ToList();
                    db.ProductClicks.RemoveRange(productsToDelete);
                    var deletedCount = await db.SaveChangesAsync();
                    scope.Complete();
                    return Ok(new { status = "success", deletedRows = deletedCount });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up deleted products: {ex.Message}");
                    return StatusCode(500, new { status = "error", message = ex.Message });
                }
            });
            return result;
        }

        /// <summary>
        /// Return basic stats about the ProductClicks table
        /// </summary>
        /// <returns></returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetClickStats()
        {
            using IEfCoreScope<InDulapContext> scope = _efCoreScopeProvider.CreateScope();
            var result = await scope.ExecuteWithContextAsync<IActionResult>(async db =>
            {
                try
                {
                    var uniqueProducts = await db.ProductClicks.Select(pc => pc.ProductId).Distinct().CountAsync();
                    var totalClicks = await db.ProductClicks.SumAsync(pc => pc.Clicks);
                    var totalRecords = await db.ProductClicks.CountAsync();
                    scope.Complete();
                    return Ok(new
                    {
                        uniqueProducts,
                        totalClicks,
                        totalRecords
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting stats: {ex.Message}");
                    return StatusCode(500, new { status = "error", message = ex.Message });
                }
            });
            return result;
        }

        #endregion
    }
}