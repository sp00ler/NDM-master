using System.ComponentModel.DataAnnotations;

namespace NetDeviceManager.Models
{
    public class Device
    {
        public int? Id { get; set; }
        public User? User { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public DevType Type { get; set; }
        [Required]
        [IPAddress]
        public string IP { get; set; }

        public uint Port { get; set; }

        public string GetDevType()
        {
            switch (Type){
                case DevType.CiscoASA: return "cisco_asa";
                case DevType.CiscoIOS: return "cisco_ios";
                default: return "unknown_type";
            }
        }
    }
}
