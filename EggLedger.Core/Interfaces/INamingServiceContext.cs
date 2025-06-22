using EggLedger.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Interfaces
{
    public interface INamingServiceContext
    {
        Task<ActionResult<string>> GenerateOrderName(User user);
        Task<ActionResult<string>> GenerateContainerName(Guid userId);
    }
}
