using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementSystem.Models
{
    public class Room
    {
        [Key]
        [Column("Id")]
        public int RoomId { get; set; }

        [Required]
        public string? RoomNumber { get; set; }

        public int RoomTypeId { get; set; }

        public RoomType? RoomType { get; set; }

        [Column("PricePerNight", TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}