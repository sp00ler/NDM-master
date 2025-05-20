using Microsoft.AspNetCore.Mvc.Rendering;

namespace NetDeviceManager.Models
{
    public static class ListItemsTools
    {
        public static List<SelectListItem> SelectListItems
        {
            get
            {
                var masks = new List<SelectListItem>();
                for (var i = 32; i >= 0; i--)
                {

                    string str = new string('1', i) + new string('0', 32 - i);

                    List<int> parts = new(4);

                    for (int j = 0; j < 4; j++)
                    {
                        parts.Add(Convert.ToInt32(str.Substring(8 * j, 8), 2));
                    }

                    masks.Add(new SelectListItem { Value = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}", Text = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]} - {i}" });
                }


                return masks;
            }


        }

        public static List<SelectListItem> ProtoSelectListItems = new() {
            new SelectListItem { Value = "0", Text = $"ip" },
        new SelectListItem { Value = $"1", Text = $"tcp" },
        new SelectListItem { Value = $"2", Text = $"udp" },
        };

        public static List<SelectListItem> SelectFromObjectGroup(Object_Group obj)
        {
            var res = new List<SelectListItem>();

            for(int i = 0; i < obj.Ipv4.Count; i++)
            {
                res.Add(new SelectListItem { Value = i.ToString(), Text = $"{obj.Ipv4[i].IpAddress}/{new IpNetmask(obj.Ipv4[i].Netmask).IntMask}" });
            }

            return res;
        }

        public static List<SelectListItem> SelectFromObjectGroups(List<Object_Group> objs)
        {
            var res = new List<SelectListItem>();

            foreach (Object_Group obj in objs)
            {
                res.Add(new SelectListItem { Value = obj.Name, Text = obj.Name });
            }

            return res;
        }
    }
}
