namespace AmeWatches.Api.Data.Entities;

public enum WatchGender
{
    Men = 1,
    Women = 2,
    Unisex = 3
}

public enum WatchType
{
    Digital = 1,
    Analog = 2,
    AnalogDigital = 3
}

public enum OrderStatus
{
    New = 1,
    Confirmed = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    CashOnDelivery = 1,
    Card = 2
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3
}
