using TicketTriage.Core.Models;

namespace TicketTriage.Core.Abstractions;

public interface ITriageService
{
    Task<TriageResult> TriageAsync(TicketRequest ticket, CancellationToken ct = default);
}
