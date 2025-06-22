using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.Core.Helpers
{
    public class NamingService(INamingServiceContext context) : INamingService
    {
        public async Task<ActionResult<string>> GenerateOrderName(User user)
        {
            return await context.GenerateOrderName(user);
        }

        public async Task<ActionResult<string>> GenerateContainerName(Guid userId)
        {
            return await context.GenerateContainerName(userId);
        }
    }
}
