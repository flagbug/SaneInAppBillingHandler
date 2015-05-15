using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Xamarin.InAppBilling;

namespace SaneInAppBillingHandler
{
    internal class SaneInAppBillingHandler
    {
        private readonly InAppBillingServiceConnection serviceConnection;

        public SaneInAppBillingHandler(Activity activity, string publicKey)
        {
            this.serviceConnection = new InAppBillingServiceConnection(activity, publicKey);
        }

        public IObservable<int> BuyProduct(Product product)
        {
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
            });
        }

        public IObservable<Unit> Connect()
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
            });
        }

        public IObservable<Unit> ConsumePurchase(Purchase purchase)
        {
            return Observable.Create<Unit>(o =>
            {
                InAppBillingHandler.OnPurchaseConsumedDelegate consumedDelegate = token =>
                {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                };

                InAppBillingHandler.OnPurchaseConsumedErrorDelegate consumedErrorDelegate = (responeCode, token) =>
                {
                    o.OnError(new InAppBillingException("Failed to consume purchase"));
                };

                InAppBillingHandler.InAppBillingProcessingErrorDelegate errorDelegate = message =>
                {
                    o.OnError(new InAppBillingException($"Consuming purchase failed: {message}"));
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
            });
        }

        public void Disconnect()
        {
            if (this.serviceConnection.Connected)
            {
                this.serviceConnection.Disconnect();
            }
        }

        public IObservable<IReadOnlyList<Purchase>> GetPurchases()
        {
            return Observable.Create<IReadOnlyList<Purchase>>(o =>
            {
                InAppBillingHandler.InAppBillingProcessingErrorDelegate errorDelegate = message =>
                {
                    o.OnError(new InAppBillingException($"Getting purchases failed: {message}"));
                };

                this.serviceConnection.BillingHandler.InAppBillingProcesingError += errorDelegate;

                IList<Purchase> purchases = this.serviceConnection.BillingHandler.GetPurchases(ItemType.Product);

                if (purchases != null)
                {
                    o.OnNext(purchases.ToList());
                    o.OnCompleted();
                }

                return () => this.serviceConnection.BillingHandler.InAppBillingProcesingError -= errorDelegate;
            });
        }

        public void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            this.serviceConnection.BillingHandler?.HandleActivityResult(requestCode, resultCode, data);
        }

        public IObservable<IReadOnlyList<Product>> QueryInventoryAsync(IEnumerable<string> skuList)
        {
            return Observable.Create<IReadOnlyList<Product>>(async o =>
            {
                InAppBillingHandler.QueryInventoryErrorDelegate errorDelegate = (code, skuDetails) =>
                {
                    o.OnError(new InAppBillingException($"Failed to query inventory. Error code: {code}"));
                };

                this.serviceConnection.BillingHandler.QueryInventoryError += errorDelegate;

                IList<Product> products = await this.serviceConnection.BillingHandler.QueryInventoryAsync(skuList.ToList(), ItemType.Product);

                if (products != null)
                {
                    o.OnNext(products.ToList());
                    o.OnCompleted();
                }

                return () =>
                {
                    this.serviceConnection.BillingHandler.QueryInventoryError -= errorDelegate;
                };
            });
        }
    }
}