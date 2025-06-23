using EggLedger.Core.DTOs.Order;
using EggLedger.Core.Models;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Interfaces
{
    public interface IUserService
    {
        Task<Result> CreateUser(StockOrderDto dto);
    }
}
