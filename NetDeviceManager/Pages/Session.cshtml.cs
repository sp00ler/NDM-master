using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using NetDeviceManager.Dal;
using NetDeviceManager.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static NDM_Service;

namespace NetDeviceManager.Pages
{
    [Authorize]
    public class SessionModel : PageModel
    {
        DevicesDBContext context;

        private readonly NDM_ServiceClient _ndmServiceClient;

        [BindProperty(Name = "session_id")]
        public int SessionId { get; set; }

        public Device Device { get; set; }
        public Device_Info DeviceInfo { get; set; }

        public List<gRPC_Interface> Interfaces { get; set; }
        public List<Access_List> AccessLists { get; set; }
        public List<Object_Group> ObjectGroups { get; set; }
        public List<string> IfNames { get; set; }

        public SessionModel(DevicesDBContext context, NDM_ServiceClient ndmServiceClient)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            _ndmServiceClient = ndmServiceClient ?? throw new ArgumentNullException(nameof(ndmServiceClient));
        }

        async public Task<IActionResult> OnGet(int s_id, int d_id)
        {
            if (s_id == 0 || d_id == 0)
                return LocalRedirect("/");

            string username = User.Identity.Name;
            Device = context.Devices.FirstOrDefault(d => d.Id == d_id);
            SessionId = s_id;
            int session = _ndmServiceClient.GetSession(new GetSessionRequest() { User = username, Target = new Target() { Host = Device.IP, Port = Device.Port.ToString() } }).Id;

            if (session != s_id)
            {
                return LocalRedirect("/");
            }


            IfNames = [.. _ndmServiceClient.GetInterfacesNames(new IdRequest { Id = s_id }).Names];
            DeviceInfo = _ndmServiceClient.GetDeviceInfo(new IdRequest { Id = s_id }).DeviceInfo;
            ObjectGroups = [.. _ndmServiceClient.GetObjectGroups(new IdRequest() { Id = s_id }).ObjectGroups];

            return Page();
        }

        public IActionResult OnPostStayAlive(int s_id)
        {

            bool res = _ndmServiceClient.RefreshSession(new IdRequest() { Id = s_id }).Result;

            if (res)
            {
                return StatusCode(200);
            }

            return StatusCode(400);
        }
        private bool SessionUserCheck(int s_id)
        {
            string username = User.Identity.Name;

            return _ndmServiceClient.CheckSession(new CheckSessionRequest() { User = username, Session = s_id }).Result;
        }

        public IActionResult OnPostAddObjectGroup(int s_id, string name, string addr, string mask)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || name == null || addr == null || mask == null)
                return new BadRequestObjectResult("Заполните все поля");

            const string regexPattern = @"^([\d]{1,3}\.){3}[\d]{1,3}$";
            var regex = new Regex(regexPattern);
            if (string.IsNullOrEmpty(addr))
                return new BadRequestObjectResult("Адрес некорректный");

            if (!regex.IsMatch(addr) || addr.Split('.').SingleOrDefault(s => int.Parse(s) > 255) != null)
                return new BadRequestObjectResult("Адрес некорректный");


            bool res = _ndmServiceClient.AddObjectGroupItem(new AddObjectGroupItemRequest() { Id = s_id, Name = name, Item = new gRPC_IPv4() { IpAddress = addr, Netmask = mask } }).Result;

            if (res)
                return new OkObjectResult(null);
            return new BadRequestObjectResult("Произошла ошибка");
        }

        public IActionResult OnPostAddAccessList(int s_id, string name, string obj_from, string obj_to, int proto, bool allow, int port)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || name == null || obj_from == null || obj_to == null)
                return new BadRequestObjectResult("Заполните поля");


            bool res = _ndmServiceClient.AddAccessList(new AddAccessListRequest() { Id = s_id, Name = name, Item = new Access_List_Item() { ObjFrom = obj_from, ObjFromType = Obj_Type.Obj, ObjTo = obj_to, ObjToType = Obj_Type.Obj, Services = { new Service_Object { Port = port.ToString(), Proto = (Proto)proto } }, Allow = allow } }).Result;

            if (res)
                return new OkObjectResult(null);
            return new BadRequestObjectResult("Произошла ошибка");
        }

        public IActionResult OnPostDeleteAccessListItem(int s_id, string name, int line)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || name == null || line == 0)
                return BadRequest();


            bool res = _ndmServiceClient.DeleteAccessListItem(new DeleteAccessListItemRequest() { Id = s_id, Line = line, Name = name }).Result;

            if (res)
                return new OkObjectResult(null);
            return new BadRequestObjectResult("Произошла ошибка");
        }

        public IActionResult OnPostDeleteAccessList(int s_id, string name)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || name == null)
                return BadRequest();


            bool res = _ndmServiceClient.DeleteAccessList(new AccessListRequest() { Id = s_id, Name = name }).Result;

            if (res)
                return new OkObjectResult(null);
            return new BadRequestObjectResult("Произошла ошибка");
        }

        public IActionResult OnPostDeleteObjectGroupItem(int s_id, string og_name, string ip_id)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || ip_id == null || og_name == null)
                return new BadRequestObjectResult("Заполните все поля");

            List<Object_Group> ogs = [.. _ndmServiceClient.GetObjectGroups(new IdRequest() { Id = s_id }).ObjectGroups];
            var og = ogs.Find(og => og.Name == og_name);

            gRPC_IPv4 ip = og.Ipv4[Convert.ToInt32(ip_id)];


            bool res = _ndmServiceClient.DeleteObjectGroupItem(new DeleteObjectGroupItemRequest() { Id = s_id, Name = og.Name, Item = ip }).Result;

            if (res)
                return new OkObjectResult(null);
            return new BadRequestObjectResult("Произошла ошибка");
        }




        public IActionResult OnPostEditInterface(int s_id, string ifname, string ifzone, string addr, string mask, string acl_in, string acl_out)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            if (s_id == 0 || ifname == null || ifzone == null || addr == null || mask == null)
                return new BadRequestObjectResult("Заполните все поля");


            const string regexPattern = @"^([\d]{1,3}\.){3}[\d]{1,3}$";
            var regex = new Regex(regexPattern);
            if (string.IsNullOrEmpty(addr))
                return new BadRequestObjectResult("Адрес некорректный");

            if (!regex.IsMatch(addr) || addr.Split('.').SingleOrDefault(s => int.Parse(s) > 255) != null)
                return new BadRequestObjectResult("Адрес некорректный");

            acl_in ??= "";
            acl_out ??= "";

            List <gRPC_Interface> interfaces = [.. _ndmServiceClient.GetInterfaces(new IdRequest() { Id = s_id }).Interfaces];
            var ifs = interfaces.Find(ifs => ifs.IfName == ifname);
            if (ifs == null)
            {
                return new BadRequestObjectResult("Интерфейс не найден");
            }

            if (ifs.IfZone != ifzone)
            {
                var res = _ndmServiceClient.SetInterfaceZone(new SetInterfaceZoneRequest() { Id = s_id, Ifname = ifname, Ifzone = ifzone}).Result;
                if (!res)
                    return BadRequest("Не удалось назначить имя интерфейса");

            }

            if (ifs.Ipv4.IpAddress != addr || ifs.Ipv4.Netmask != mask)
            {
                var res = _ndmServiceClient.SetInterfaceAddress(new SetInterfaceAddressRequest() { Id = s_id, Ifname = ifname, Ipv4 = new gRPC_IPv4() { IpAddress = addr, Netmask = mask } }).Result;
                if (!res)
                    return BadRequest("Не удалось назначить входной список доступа");
            }

            if (ifs.InAccessGroup != acl_in)
            {
                
                var res = _ndmServiceClient.SetAccessGroup(new SetAccessGroupRequest() { Aclname = acl_in, Id = s_id, Ifname = ifzone, InOut = "in", OldAcl = ifs.InAccessGroup }).Result;
                if (!res)
                    return BadRequest("Не удалось назначить входной список доступа");
            }

            if (ifs.OutAccessGroup != acl_out)
            {
                var res = _ndmServiceClient.SetAccessGroup(new SetAccessGroupRequest() { Aclname = acl_out, Id = s_id, Ifname = ifzone, InOut = "out", OldAcl = ifs.OutAccessGroup }).Result;
                if (!res)
                    return BadRequest("Не удалось назначить выходной список доступа");
            }

            return new OkResult();
        }

        public IActionResult OnPostDeleteObjectGroupPartial(int s_id, string obj_grp_name)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");

            List<Object_Group> ogs = [.. _ndmServiceClient.GetObjectGroups(new IdRequest() { Id = s_id }).ObjectGroups];
            var og = ogs.Find(og => og.Name == obj_grp_name);

            if (og != null)
                return Partial("_DeleteObjectGroup", og);
            else
                return BadRequest();
        }

        public IActionResult OnPostEditInterfacePartial(int s_id, string int_name, string ifzone, string addr, string mask, string acl_in, string acl_out)
        {
            if (!SessionUserCheck(s_id))
                return LocalRedirect("/");


            AccessLists ??= [.. _ndmServiceClient.GetAccessLists(new IdRequest() { Id = s_id }).AccessLists];

            acl_in ??= "";
            acl_out ??= "";

            List<string> access_lists_names = AccessLists.Select(l => l.Name).ToList();

            var og = new gRPC_Interface() { IfName = int_name, IfZone =  ifzone, Ipv4 = new gRPC_IPv4() { IpAddress = addr, Netmask = mask}, InAccessGroup = acl_in, OutAccessGroup = acl_out };

            if (og != null)
                return Partial("_EditInterface", new Tuple<gRPC_Interface, List<string>>(og, access_lists_names));
            else
                return BadRequest();
        }

        public PartialViewResult OnPostAccessListsPartial(int s_id)
        {
            if (!SessionUserCheck(s_id))
                return Partial("_AccessListsPartial", new List<Access_List>());

            List<Access_List> AccessLists = [.. _ndmServiceClient.GetAccessLists(new IdRequest() { Id = s_id }, deadline: DateTime.UtcNow.AddMinutes(2)).AccessLists];
            Console.WriteLine($"Было загружено {AccessLists.Count} элементов AccessLists");
            return Partial("_AccessListsPartial", AccessLists);
        }

        public PartialViewResult OnPostInterfacesPartial(int s_id)
        {
            if (!SessionUserCheck(s_id))
                return Partial("_InterfacesPartial", new List<gRPC_Interface>());

            List<gRPC_Interface> Interfaces = [.. _ndmServiceClient.GetInterfaces(new IdRequest() { Id = s_id }).Interfaces];
            return Partial("_InterfacesPartial", Interfaces);
        }

        public PartialViewResult OnPostObjectGroupsPartial(int s_id)
        {
            if (!SessionUserCheck(s_id))
                return Partial("_ObjectGroupsPartial", new List<Object_Group>());

            ObjectGroups = [.. _ndmServiceClient.GetObjectGroups(new IdRequest() { Id = s_id }).ObjectGroups];
            return Partial("_ObjectGroupsPartial", ObjectGroups);
        }

        public IActionResult OnPostPacketTracerPartial(int s_id)
        {
            if (!SessionUserCheck(s_id))
                return BadRequest();
          
            List<string> ifnames = [.. _ndmServiceClient.GetInterfacesNames(new IdRequest() { Id = s_id }).Names];

            return Partial("_PacketTracerPartial", ifnames);
        }

        public IActionResult OnPostPacketTracer(int s_id, string ifname, string proto, string src_addr, int src_port, string dst_addr, int dst_port)
        {

            if (!SessionUserCheck(s_id))
                return BadRequest();


            const string regexPattern = @"^([\d]{1,3}\.){3}[\d]{1,3}$";
            var regex = new Regex(regexPattern);

            if (string.IsNullOrEmpty(src_addr))
                return new BadRequestObjectResult("Введите адрес источника");
            if (!regex.IsMatch(src_addr) || src_addr.Split('.').SingleOrDefault(s => int.Parse(s) > 255) != null)
                return new BadRequestObjectResult("Адрес источника некорректный");
            if (string.IsNullOrEmpty(dst_addr))
                return new BadRequestObjectResult("Введите адрес назначения");
            if (!regex.IsMatch(dst_addr) || dst_addr.Split('.').SingleOrDefault(s => int.Parse(s) > 255) != null)
                return new BadRequestObjectResult("Адрес назначения некорректный");

            PTResponse ptr = _ndmServiceClient.PacketTracer(new PTRequest() { Id = s_id, DstIp = dst_addr, DstPort = dst_port, Ifname = ifname, Proto = proto, SrcIp = src_addr, SrcPort = src_port });

            return Partial("_PacketTracerResult", ptr);

        }
    }
}
