using MediatR;
using Microsoft.EntityFrameworkCore;
using SmsDispatch.Api.Data;
using SmsDispatch.Api.Models;

namespace SmsDispatch.Api.Queries;

/// <summary>
/// Handles GetMessagesQuery:
///   Applies optional text search, sort, and pagination against dbo.Messages.
/// </summary>
public class GetMessagesQueryHandler(SmsDbContext db)
    : IRequestHandler<GetMessagesQuery, PagedResult<MessageResponse>>
{
    public async Task<PagedResult<MessageResponse>> Handle(GetMessagesQuery qry, CancellationToken ct)
    {
        var query = db.Messages.AsQueryable();

        // ── filter ────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(qry.Search) && qry.Search.Length >= 3)
        {
            query = qry.Criteria.ToLower() switch
            {
                "equals" => query.Where(m =>
                                m.Message == qry.Search ||
                                (m.RecipientName != null && m.RecipientName == qry.Search)),
                _ => query.Where(m =>
                         m.Message.Contains(qry.Search) ||
                         (m.RecipientName != null && m.RecipientName.Contains(qry.Search)))
            };
        }

        var total = await query.CountAsync(ct);

        // ── sort ──────────────────────────────────────────────────────
        query = (qry.SortBy.ToLower(), qry.SortDir.ToLower()) switch
        {
            ("modifieddatetime", "asc")  => query.OrderBy(m => m.ModifiedDatetime),
            ("modifieddatetime",    _)   => query.OrderByDescending(m => m.ModifiedDatetime),
            ("status",           "asc")  => query.OrderBy(m => m.Status),
            ("status",              _)   => query.OrderByDescending(m => m.Status),
            ("recipient",        "asc")  => query.OrderBy(m => m.RecipientName),
            ("recipient",           _)   => query.OrderByDescending(m => m.RecipientName),
            (_,                  "asc")  => query.OrderBy(m => m.CreatedDateTime),
            _                            => query.OrderByDescending(m => m.CreatedDateTime)
        };

        // ── page ──────────────────────────────────────────────────────
        var items = await query
            .Skip((qry.Page - 1) * qry.PageSize)
            .Take(qry.PageSize)
            .Select(m => new MessageResponse(
                (long)m.Id,
                m.To.ToString("F0"),
                m.From.ToString("F0"),
                m.Message,
                m.RecipientName,
                m.Status,
                m.ErrorMessage,
                m.CreatedDateTime,
                m.ModifiedDatetime))
            .ToListAsync(ct);

        return new PagedResult<MessageResponse>(items, total, qry.Page, qry.PageSize);
    }
}
