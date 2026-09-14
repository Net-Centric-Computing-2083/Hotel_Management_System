using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementSystem.Models
{
    public class RoomType
    {
        [Key]
        [Column("Id")]
        public int RoomTypeId { get; set; }

        [Required]
        [Column("TypeName")]
        public string? Name { get; set; }

        public int Capacity { get; set; }

        public List<Room>? Rooms { get; set; }
    }
}