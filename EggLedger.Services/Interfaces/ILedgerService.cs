using EggLedger.DTO.Ledger;
using FluentResults;

namespace EggLedger.Services.Interfaces;

public interface ILedgerService
{
    Task<Result<RoomLedgerDto>> GetRoomLedgerAsync(int roomCode, CancellationToken cancellationToken = default);
    Task<Result<SettlementDto>> RecordSettlementAsync(Guid receiverId, int roomCode, SettlementCreateDto dto, CancellationToken cancellationToken = default);
    Task<Result<List<SettlementDto>>> GetSettlementHistoryAsync(int roomCode, CancellationToken cancellationToken = default);
    Task<Result> DeleteSettlementAsync(int roomCode, Guid settlementId, Guid callerId, CancellationToken cancellationToken = default);
}
