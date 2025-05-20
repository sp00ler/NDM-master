using System.ComponentModel.DataAnnotations;

namespace NetDeviceManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Login { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
}
