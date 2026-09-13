using System.ComponentModel.DataAnnotations;

namespace HotelManagementSystem.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required]
        public string? Name { get; set; }

        public decimal BasePrice { get; set; }

        public List<Room>? Rooms { get; set; }
    }
}