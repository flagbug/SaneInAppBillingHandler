# Overview
SaneInAppBillingHandler is a sane wrapper over the [Xamarin.InAppBilling](http://components.xamarin.com/gettingstarted/xamarin.inappbilling) component.

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