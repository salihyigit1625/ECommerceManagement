namespace ECommerceManagement.Domain.Enums;

public enum MovementType
{
    Entry,
    Exit,
    Reservation
}

public enum OrderStatus
{
    Pending,
    Invoiced,
    Shipped,
    Delivered,
    Canceled
}

public enum InvoiceStatus
{
    Waiting,
    Confirmed,
    Canceled
}

public enum AxIntegrationStatus
{
    Pending,
    Sent,
    Failed
}