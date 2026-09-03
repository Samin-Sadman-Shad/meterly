using Identity.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Context
{
    public class MeterlyIdentityDbContext:IdentityDbContext<ApplicationUser,ApplicationRole, Guid >
    {
        public MeterlyIdentityDbContext(DbContextOptions options):base(options)
        {
            
        }
    }
}
