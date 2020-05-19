using System;
using ExactlyOnce.AzureFunctions.CosmosDb;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public class OrderWorkflow : Manages<OrderWorkflow.Order>, IHandler<PlaceOrder>, IHandler<ApproveOrder>
    {
        public class Order : CosmosDbE1Content
        {
            public DateTime PlacedAt { get; set; }
            public DateTime ApprovedAt { get; set; }
        }

        public Guid Map(PlaceOrder m) => m.OrderId.ToGuid();

        public Guid Map(ApproveOrder m) => m.OrderId.ToGuid();


        public void Handle(IHandlerContext context, PlaceOrder message)
        {
            Data.PlacedAt = DateTime.Now;
        }

        public void Handle(IHandlerContext context, ApproveOrder message)
        {
            Data.ApprovedAt = DateTime.Now;
        }
    }
}