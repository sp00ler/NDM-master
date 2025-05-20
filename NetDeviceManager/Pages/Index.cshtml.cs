using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using NetDeviceManager.Dal;
using NetDeviceManager.Models;

using Grpc.Net.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Humanizer;
using System.Configuration;
using static NDM_Service;
using Grpc.Core;



namespace NetDeviceManager.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        DevicesDBContext context;

        private readonly NDM_ServiceClient _ndmServiceClient;
        public List<Tuple<Device, int>> Devices { get; private set; } = new();

        public string ErrorText { get; set; }
        public IndexModel(ILogger<IndexModel> logger, DevicesDBContext db, NDM_ServiceClient client)
        {
            _logger = logger;
            context = db;
            _ndmServiceClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        [BindProperty]
        public Device Device { get; set; } = default!;

        [BindProperty]
        public Creds Creds { get; set; } = default!;

        public IActionResult OnPostOpenSession()
        {
            var dev_id = Convert.ToInt16(Request.Form["device_id"]);
            if (dev_id == 0 || Creds == null)
                return LocalRedirect("/");

            Device dev = context.Devices.FirstOrDefault(d => d.Id == dev_id);

            if (dev == null)
                return LocalRedirect("/");
 

            OpenSessionResponse response = new OpenSessionResponse();
            var data = new gRPC_Device { DeviceType = dev.GetDevType(), Target = new Target { Host = dev.IP, Port = dev.Port.ToString() }, Creds = Creds };
            try
            {
                response = _ndmServiceClient.OpenSession(new OpenSessionRequest { Device = data, User = User.Identity.Name });
            }
            catch (Exception ex)
            {

                response.Id = 0;
                response.Error = "Не удалось подключиться";
            }

            return new OkObjectResult(response);
        }

        public void OnGet()
        {
            try
            {
                User user = context.Users.FirstOrDefault(x => x.Login == User.Identity.Name);
                var devs = context.Devices.Where(x => x.User == user).AsNoTracking().ToList();

                Devices.Clear();

                foreach (var d in devs)
                {
                    var request = new GetSessionRequest() { Target = new Target() { Host = d.IP, Port = d.Port.ToString() }, User = user.Login };
                    int session = _ndmServiceClient.GetSession(request).Id;
                    Devices.Add(new Tuple<Device, int>(d, session));
                }
            }
            catch (RpcException ex) {
                if (ex.StatusCode.ToString() == "Unavailable")
                {
                    ErrorText = "Сервер для взаимодействия с конечными устройствами недоступен!";
                }
            }
        }


        
        public async Task<IActionResult> OnPostAsync()
        {

            if (!ModelState.IsValid || context.Devices == null || Device == null)
            {
                return LocalRedirect("/");
            }
            Device.User = context.Users.FirstOrDefault(u => u.Login == User.Identity.Name);
            context.Devices.Add(Device);
            await context.SaveChangesAsync();

            return LocalRedirect("/");

        }

        public async Task<IActionResult> OnPostDeleteDeviceAsync()
        {
            Device dev = context.Devices.Include(d => d.User).FirstOrDefault(d => d.Id == Device.Id);
            if (Device.Id == null || dev.User.Login != User.Identity.Name)
            {
                return LocalRedirect("/");
            }

            context.Devices.Remove(dev);

            await context.SaveChangesAsync();

            return LocalRedirect("/");

        }





    }
}