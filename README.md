# Overview
SaneInAppBillingHandler is a sane wrapper over the [Xamarin.InAppBilling](http://components.xamarin.com/gettingstarted/xamarin.inappbilling) component. Note that you have to install the Xamarin.InAppBilling component manually from the Xamarin component store, and the [SaneInAppBillingHandler package](https://www.nuget.org/packages/SaneInAppBillingHandler/) from NuGet

## Why

The Xamarin.InAppBilling API is, let's say, a bit strange to use.

For example: to buy a product through the API, you have to handle the following events:

- `OnProductPurchased`
- `OnProductPurchasedError``
- `BuyProductError``
- `InAppBillingProcesingError`
- `OnUserCanceled`
- `OnPurchaseFailedValidation`

SaneInAppBillingHandler exposes a convenient asynchronous method instead.

## Quickstart

### Creating the billing handler

```csharp
using SaneInAppBillingHandler;

var handler = new SaneInAppBillingHandler(yourActivity, "API key that you receive from Google");

// Call this method when creating your activity
try
{
  await handler.Connect();
}

catch (InAppBillingException ex)
{
  // Thrown if the commection fails for whatever reason (device doesn't support In-App billing, etc.)
  // All methods (except for Disconnect()) may throw this exception, handling it is omitted for brevity in the rest of the samples
}
```

### Getting available purchases

```csharp
// Retrieve the product infos for "myItem1" and "myItem2" (these are the IDs that you give your products in the Google Play Developer Console)
// The second argument specifies if those products are subscriptions or normal one-time purchases
IReadOnlyList<Product> products = await handler.QueryInventory(new[]{"myItem1", "myItem2", ItemType.Product);
```

### Getting a list of products that the user has purchased

```csharp
IReadOnlyList<Purchase> purchases = await handler.GetPurchases(ItemType.Product);
```

### Buying a product

```csharp
// Buys the product and returns a billing result. Look this up in the BillingResult class.
int result = await handler.BuyProduct(product);
```

### Consuming a purchase

```csharp
await handler.ConsumePurchase(purchase);
```

### Disconnecting the handler

```csharp
// Call this method when destroying your activity
handler.Disconnect();
```
