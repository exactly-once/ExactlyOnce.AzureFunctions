using System;
using ExactlyOnce.AzureFunctions.CosmosDb;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public class OrderWorkflow : Manages<OrderWorkflow.Order>, IHandler<PlaceOrder>, IHandler<ApproveOrder>,
        IHandler<PrepareShipmentResponse>
    {
        public class Order : CosmosDbE1Content
        {
            public DateTime PlacedAt { get; set; }
            public DateTime ApprovedAt { get; set; }
            public bool ShipmentReady { get; set; }
        }

        public Guid Map(PlaceOrder m) => m.OrderId.ToGuid();

        public Guid Map(ApproveOrder m) => m.OrderId.ToGuid();

        public Guid Map(PrepareShipmentResponse m) => m.OrderId.ToGuid();

        public void Handle(HandlerContext context, PlaceOrder message)
        {
            Data.PlacedAt = DateTime.Now;

            context.Send(new PrepareShipment
            {
                OrderId = message.OrderId
            });
        }

        public void Handle(HandlerContext context, ApproveOrder message)
        {
            Data.ApprovedAt = DateTime.Now;
        }

        public void Handle(HandlerContext context, PrepareShipmentResponse message)
        {
            Data.ShipmentReady = true;
        }
    }
}