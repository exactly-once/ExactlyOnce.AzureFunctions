using System;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public class PlaceOrder
    {
        public string OrderId { get; set; }
    }

    public class ApproveOrder
    {
        public string OrderId { get; set; }
    }

    public class PrepareShipment
    {
        public string OrderId { get; set; }
    }

    public class PrepareShipmentResponse
    {
        public string OrderId { get; set; }
    }
}