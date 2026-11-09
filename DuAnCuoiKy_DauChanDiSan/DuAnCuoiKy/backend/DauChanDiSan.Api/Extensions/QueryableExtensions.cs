using System.Linq.Expressions;
using DauChanDiSan.Api.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Extensions;

public static class QueryableExtensions
{
    public static Task<PageResult<T>> ToPageAsync<T>(
        this IOrderedQueryable<T> query, int trang, int kichThuoc, CancellationToken ct) =>
        query.ToPageAsync(trang, kichThuoc, item => item, ct);

    public static async Task<PageResult<TResult>> ToPageAsync<T, TResult>(
        this IOrderedQueryable<T> query, int trang, int kichThuoc, Expression<Func<T, TResult>> projection, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((trang - 1) * kichThuoc).Take(kichThuoc).Select(projection).ToListAsync(ct);
        return new PageResult<TResult>(total, trang, kichThuoc, items);
    }
}
