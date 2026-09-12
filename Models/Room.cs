using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        public string? RoomNumber { get; set; }

        public int RoomTypeId { get; set; }

        public RoomType? RoomType { get; set; }

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        public string Status { get; set; } = "Available";
    }
}