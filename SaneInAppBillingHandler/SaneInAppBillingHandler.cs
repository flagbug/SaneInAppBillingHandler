using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Xamarin.InAppBilling;

namespace SaneInAppBillingHandler
{
    public class SaneInAppBillingHandler
    {
        private readonly InAppBillingServiceConnection serviceConnection;

        /// <summary>
        /// Creates a new instance of the <see cref="SaneInAppBillingHandler"/> class.
        /// </summary>
        /// <param name="activity">
        /// The activity you're hosting this <see cref="SaneInAppBillingHandler"/> in.
        /// </param>
        /// <param name="publicKey">The key you received from Google for your In-App purchases.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="activity"/> or <paramref name="publicKey"/> is <c>null</c>.
        /// </exception>
        public SaneInAppBillingHandler(Activity activity, string publicKey)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            if (publicKey == null)
                throw new ArgumentNullException(publicKey);

            this.serviceConnection = new InAppBillingServiceConnection(activity, publicKey);
        }

        /// <summary>
        /// Buys the specified <see cref="Product"/>
        /// </summary>
        /// <param name="product">The product to buy.</param>
        /// <returns>
        /// A future with the result of the operation.
        /// 
        /// The result maps to a value in the <see cref="BillingResult"/> class.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="product"/> is <c>null</c></exception>
        public Task<int> BuyProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return Observable.Create<int>(o =>
            {
                InAppBillingHandler.OnProductPurchasedDelegate purchaseCompleted = (response, purchase, data, signature) =>
                {
                    o.OnNext(response);
                    o.OnCompleted();
                };

                InAppBillingHandler.OnProductPurchaseErrorDelegate purchaseError = (response, sku) =>
                {
                    o.OnNext(response);
                    o.OnCompleted();
                };

                InAppBillingHandler.BuyProductErrorDelegate errorDelegate = (code, sku) =>
                {
                    o.OnNext(code);
                    o.OnCompleted();
                };

                InAppBillingHandler.InAppBillingProcessingErrorDelegate errorDelegate2 = message =>
                {
                    o.OnNext(BillingResult.Error);
                    o.OnCompleted();
                };

                InAppBillingHandler.OnUserCanceledDelegate canceledDelegate = () =>
                {
                    o.OnNext(BillingResult.UserCancelled);
                    o.OnCompleted();
                };

                InAppBillingHandler.OnPurchaseFailedValidationDelegate validationErrorDelegate = (purchase, data, signature) =>
                {
                    o.OnNext(BillingResult.Error);
                    o.OnCompleted();
                };

                this.serviceConnection.BillingHandler.OnProductPurchased += purchaseCompleted;
                this.serviceConnection.BillingHandler.OnProductPurchasedError += purchaseError;
                this.serviceConnection.BillingHandler.BuyProductError += errorDelegate;
                this.serviceConnection.BillingHandler.InAppBillingProcesingError += errorDelegate2;
                this.serviceConnection.BillingHandler.OnUserCanceled += canceledDelegate;
                this.serviceConnection.BillingHandler.OnPurchaseFailedValidation += validationErrorDelegate;

                this.serviceConnection.BillingHandler.BuyProduct(product);

                return () =>
                {
                    this.serviceConnection.BillingHandler.OnProductPurchased -= purchaseCompleted;
                    this.serviceConnection.BillingHandler.OnProductPurchasedError -= purchaseError;
                    this.serviceConnection.BillingHandler.BuyProductError -= errorDelegate;
                    this.serviceConnection.BillingHandler.InAppBillingProcesingError -= errorDelegate2;
                    this.serviceConnection.BillingHandler.OnUserCanceled -= canceledDelegate;
                    this.serviceConnection.BillingHandler.OnPurchaseFailedValidation -= validationErrorDelegate;
                };
            }).ToTask();
        }

        /// <summary>
        /// Connects to the billing service.
        /// 
        /// Call this method before making any other requests.
        /// </summary>
        /// <exception cref="InAppBillingException">
        /// (asynchronous) The connection to the billing service couldn't be established.
        /// </exception>
        public Task Connect()
        {
            return Observable.Create<Unit>(o =>
            {
                if (this.serviceConnection.Connected)
                {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                    return () => { };
                }

                InAppBillingServiceConnection.OnConnectedDelegate connectedDelegate = () =>
                {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                };

                InAppBillingServiceConnection.OnInAppBillingErrorDelegate errorDelegate = (error, message) =>
                {
                    string realMessage = $"{message}: {error}";

                    o.OnError(new InAppBillingException(realMessage));
                };

                this.serviceConnection.OnConnected += connectedDelegate;
                this.serviceConnection.OnInAppBillingError += errorDelegate;

                this.serviceConnection.Connect();

                return () =>
                {
                    this.serviceConnection.OnConnected -= connectedDelegate;
                    this.serviceConnection.OnInAppBillingError -= errorDelegate;
                };
            }).ToTask();
        }

        /// <summary>
        /// Consumes the specified <see cref="Purchase"/>.
        /// </summary>
        /// <param name="purchase">The <see cref="Purchase"/> to consume.</param>
        /// <exception cref="ArgumentNullException"><paramref name="purchase"/> is <c>null</c>.</exception>
        /// <exception cref="InAppBillingException">
        /// The <see cref="Purchase"/> couldn't be consumed.
        /// </exception>
        public Task ConsumePurchase(Purchase purchase)
        {
            if (purchase == null)
                throw new ArgumentNullException(nameof(purchase));

            return Observable.Create<Unit>(o =>
            {
                InAppBillingHandler.OnPurchaseConsumedDelegate consumedDelegate = token =>
                {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                };

                InAppBillingHandler.OnPurchaseConsumedErrorDelegate consumedErrorDelegate = (responseCode, token) =>
                {
                    o.OnError(new InAppBillingException("Failed to consume purchase"));
                };

                InAppBillingHandler.InAppBillingProcessingErrorDelegate errorDelegate = message =>
                {
                    o.OnError(new InAppBillingException($"Failed to consume purchase: {message}"));
                };

                this.serviceConnection.BillingHandler.OnPurchaseConsumed += consumedDelegate;
                this.serviceConnection.BillingHandler.OnPurchaseConsumedError += consumedErrorDelegate;
                this.serviceConnection.BillingHandler.InAppBillingProcesingError += errorDelegate;

                this.serviceConnection.BillingHandler.ConsumePurchase(purchase);

                return () =>
                {
                    this.serviceConnection.BillingHandler.OnPurchaseConsumed -= consumedDelegate;
                    this.serviceConnection.BillingHandler.OnPurchaseConsumedError -= consumedErrorDelegate;
                    this.serviceConnection.BillingHandler.InAppBillingProcesingError -= errorDelegate;
                };
            }).ToTask();
        }

        public void Disconnect()
        {
            if (this.serviceConnection.Connected)
            {
                this.serviceConnection.Disconnect();
            }
        }

        /// <summary>
        /// Gets a list of all purchased products.
        /// </summary>
        /// <param name="itemType">
        /// The type of product to retrieve. See the <see cref="ItemType"/> class for a list of types.
        /// </param>
        /// <returns>A list of purchased products.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="itemType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="itemType"/> is not <see cref="ItemType.Product"/> or <see cref="ItemType.Subscription"/>.
        /// </exception>
        /// <exception cref="InAppBillingException">
        /// (asynchronous) Retrieving the purchases failed.
        /// </exception>
        public Task<IReadOnlyList<Purchase>> GetPurchases(string itemType)
        {
            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));

            if (itemType != ItemType.Product || itemType != ItemType.Subscription)
                throw new ArgumentException("Invalid item type.");

            return Observable.Create<IReadOnlyList<Purchase>>(o =>
            {
                InAppBillingHandler.InAppBillingProcessingErrorDelegate errorDelegate = message =>
                {
                    o.OnError(new InAppBillingException($"Getting purchases failed: {message}"));
                };

                this.serviceConnection.BillingHandler.InAppBillingProcesingError += errorDelegate;

                IList<Purchase> purchases = this.serviceConnection.BillingHandler.GetPurchases(itemType);

                o.OnNext(purchases == null ? new List<Purchase>() : purchases.ToList());
                o.OnCompleted();

                return () => this.serviceConnection.BillingHandler.InAppBillingProcesingError -= errorDelegate;
            }).ToTask();
        }

        /// <summary>
        /// Call this method in the <see cref="Activity.OnActivityResult(int, Result, Intent)"/>
        /// method of your activity, or your In-App purchases won't work.
        /// </summary>
        public void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (this.serviceConnection.BillingHandler != null)
            {
                this.serviceConnection.BillingHandler.HandleActivityResult(requestCode, resultCode, data);
            }
        }

        /// <summary>
        /// Retrieves the <see cref="Product"/> s with the specified IDs and item type.
        /// </summary>
        /// <param name="idList">A list of IDs of the products to retrieve.</param>
        /// <param name="itemType">
        /// The type of product to retrieve. See the <see cref="ItemType"/> class for a list of types.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="idList"/> or <paramref name="itemType"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="idList"/> is empty
        /// -- or -- <paramref name="itemType"/> is not <see cref="ItemType.Product"/> or <see cref="ItemType.Subscription"/>.
        /// </exception>
        /// <exception cref="InAppBillingException">
        /// (asynchronous) Retrieving the products failed.
        /// </exception>
        public Task<IReadOnlyList<Product>> QueryInventory(IEnumerable<string> idList, string itemType)
        {
            if (idList == null)
                throw new ArgumentNullException(nameof(idList));

            if (!idList.Any())
                throw new ArgumentException($"{nameof(idList)} can't be empty.");

            if (itemType == null)
                throw new ArgumentNullException(nameof(itemType));

            if (itemType != ItemType.Product || itemType != ItemType.Subscription)
                throw new ArgumentException("Invalid item type.");

            return Observable.Create<IReadOnlyList<Product>>(async o =>
            {
                InAppBillingHandler.QueryInventoryErrorDelegate errorDelegate = (code, skuDetails) =>
                {
                    o.OnError(new InAppBillingException($"Failed to query inventory. Error code: {code}"));
                };

                this.serviceConnection.BillingHandler.QueryInventoryError += errorDelegate;

                IList<Product> products = await this.serviceConnection.BillingHandler.QueryInventoryAsync(idList.ToList(), itemType);

                o.OnNext(products == null ? new List<Product>() : products.ToList());
                o.OnCompleted();

                return () => this.serviceConnection.BillingHandler.QueryInventoryError -= errorDelegate;
            }).ToTask();
        }
    }
}