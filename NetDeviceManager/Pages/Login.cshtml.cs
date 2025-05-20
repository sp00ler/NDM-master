using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NetDeviceManager.Dal;
using NetDeviceManager.Models;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace NetDeviceManager.Pages
{
    public class LoginModel : PageModel
    {
        private readonly NetDeviceManager.Dal.DevicesDBContext _context;

        public LoginModel(NetDeviceManager.Dal.DevicesDBContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public User User { get; set; } = default!;


        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || _context.Users == null || User == null)
            {
                return Page();
            }

            var userFromDB = _context.Users.SingleOrDefault(x => x.Login == User.Login);

            // validate
            if (userFromDB == null || !BCrypt.Net.BCrypt.Verify(User.Password, userFromDB.Password))
                return Page();

            // authentication successful
            //var response = _mapper.Map<AuthenticateResponse>(user);
            //response.Token = _jwtUtils.GenerateToken(user);
            //return response;



            //User.Password = BCrypt.Net.BCrypt.HashPassword(User.Password);

            //_context.Users.Add(User);
            //await _context.SaveChangesAsync();
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userFromDB.Login),
        };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);


            await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

            return RedirectToPage("./Index");
        }
    }
}
