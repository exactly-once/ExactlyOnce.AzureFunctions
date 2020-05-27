namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public class PlaceOrder : Message
    {
        public string OrderId { get; set; }
    }

    public class ApproveOrder : Message
    {
        public string OrderId { get; set; }
    }

    public class PrepareShipment : Message
    {
    }

    public class PrepareShipmentResponse : Message
    {

    }
}