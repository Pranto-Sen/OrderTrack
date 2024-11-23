using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderTrack.API.Models
{
    [Table("tblProducts")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ProductName { get; set; }

        public int UnitPrice { get; set; }

        public int Stock { get; set; }

        // Navigation Property
        public ICollection<Order> Orders { get; set; }
    }
}
