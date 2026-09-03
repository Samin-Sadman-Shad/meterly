using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Model
{
    public class ApplicationUser:IdentityUser<Guid>
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
}
