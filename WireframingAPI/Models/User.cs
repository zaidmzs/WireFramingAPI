using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace WireframingAPI.Models
{
    public class User
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; } 

        [Required]
        [StringLength(20)]
        public string ICNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be 4 digits long.")]
        public string Pin { get; set; }

        public bool Migration { get; set; }


    }


}
