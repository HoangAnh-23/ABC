namespace SellApp.Model
{
    public class OrderDetail
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string UnitBill { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice
        {
            get { return Quantity * UnitPrice; }
        }
        public string Customer { get; set; }

    }
}
