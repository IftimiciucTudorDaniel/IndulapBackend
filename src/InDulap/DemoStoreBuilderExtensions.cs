using InDulap.Events;
using InDulap.Web.Extractors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Linq;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Commerce.Cms.Extractors;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Core.Events.Notification.Handlers.Order;
using Umbraco.Commerce.Extensions;
using Umbraco.Extensions;

namespace InDulap
{
    public static class DemoStoreBuilderExtensions
    {
        public static IUmbracoBuilder AddDemoStore(this IUmbracoBuilder umbracoBuilder)
        {
            umbracoBuilder.AddUmbracoCommerce(v =>
            {
                // Enable SQL Server support
                v.AddSqlServer();

                // Replace the umbraco product name extractor with one that supports child variants
                v.Services.AddUnique<IUmbracoProductNameExtractor, CompositeProductNameExtractor>();

                // Register event handlers
                v.WithNotificationEvent<OrderProductAddingNotification>()
                    .RegisterHandler<OrderProductAddingHandler>();

                v.WithNotificationEvent<OrderLineChangingNotification>()
                    .RegisterHandler<OrderLineChangingHandler>();

                v.WithNotificationEvent<OrderLineRemovingNotification>()
                    .RegisterHandler<OrderLineRemovingHandler>();

                v.WithNotificationEvent<OrderPaymentCountryRegionChangingNotification>()
                    .RegisterHandler<OrderPaymentCountryRegionChangingHandler>();

                v.WithNotificationEvent<OrderShippingCountryRegionChangingNotification>()
                    .RegisterHandler<OrderShippingCountryRegionChangingHandler>();

                v.WithNotificationEvent<OrderShippingMethodChangingNotification>()
                    .RegisterHandler<OrderShippingMethodChangingHandler>();
                
                if (v.Config.GetValue<bool>("Umbraco:Commerce:DemoStore:LoadTest"))
                {
                    // If we are running a load test, we don't want to send emails
                    v.WithNotificationEvent<OrderFinalizedNotification>()
                        .RemoveHandler<SendFinalizedOrderEmail>()
                        .RemoveHandler<SendGiftCardEmails>();
                }

            });

            umbracoBuilder.AddNotificationHandler<UmbracoApplicationStartingNotification, TransformExamineValues>();

            umbracoBuilder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();

                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/json" });
            });

            umbracoBuilder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            return umbracoBuilder;
        }
    }
}
