using EggLedger.Core.DTOs.Room;
using EggLedger.Core.Models;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Interfaces
{
    public interface IRoomService
    {
        Task<Result<Room>> CreateRoomAsync(CreateRoomDto dto);
        Task<Result<User>> JoinRoomAsync(JoinRoomDto dto);
    }
}
