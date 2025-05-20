
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using NetDeviceManager.Models;


public class IPAddressAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        Device yourviewmodel = (Device)validationContext.ObjectInstance;

        const string regexPattern = @"^([\d]{1,3}\.){3}[\d]{1,3}$";
        var regex = new Regex(regexPattern);
        if (string.IsNullOrEmpty(yourviewmodel.IP))
        {
            return new ValidationResult("IP address  is null");
        }
        if (!regex.IsMatch(yourviewmodel.IP) || yourviewmodel.IP.Split('.').SingleOrDefault(s => int.Parse(s) > 255) != null)
            return new ValidationResult("Invalid IP Address");


        return ValidationResult.Success;
    }
}
