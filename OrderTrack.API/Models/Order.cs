using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderTrack.API.Models
{
    [Table("tblOrders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CustomerName { get; set; }
        public int Quantity { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
