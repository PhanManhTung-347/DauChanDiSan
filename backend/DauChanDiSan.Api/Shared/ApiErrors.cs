using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Shared;

public sealed class ApiFault(int status, string detail) : Exception(detail)
{
    public int Status { get; } = status;

    public static ApiFault Missing(string detail) => new(404, detail);
    public static ApiFault Invalid(string detail) => new(400, detail);
    public static ApiFault Denied() => new(403, "Bạn không có quyền thực hiện thao tác này.");
}

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, detail, code) = exception switch
        {
            ApiFault fault => (fault.Status, fault.Message, (int?)null),
            DbUpdateConcurrencyException => (409, "Dữ liệu vừa thay đổi. Tải lại và thử lại.", (int?)null),
            SqlException sql => Map(sql),
            DbUpdateException { InnerException: SqlException sql } => Map(sql),
            _ => (500, "Không thể xử lý yêu cầu.", (int?)null)
        };
        if (status >= 500)
            logger.LogError(exception, "API lỗi tại {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status, Title = status switch
            {
                400 => "Dữ liệu không hợp lệ", 403 => "Thiếu quyền truy cập", 404 => "Không tìm thấy",
                409 => "Xung đột dữ liệu", 502 => "Dịch vụ ngoài trả lỗi", 503 => "Dịch vụ chưa sẵn sàng", _ => "Lỗi xử lý"
            },
            Detail = detail
        };
        if (code is not null)
            problem.Extensions["sqlCode"] = code;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }

    private static (int, string, int?) Map(SqlException sql) => sql.Number switch
    {
        51201 or 51101 or 51103 or 51109 or 51126 or 51162 or 51171 => (403, sql.Message, sql.Number),
        51204 or 51105 or 51125 or 51134 or 51140 or 51150 or 51173 => (404, sql.Message, sql.Number),
        51202 or 51008 or 51009 or 51104 or 51108 or 51110 or 51121 or 51123 or 51165 => (409, sql.Message, sql.Number),
        51203 or (>= 51000 and < 51190) => (400, sql.Message, sql.Number),
        1205 => (409, "Dữ liệu đang được cập nhật. Hãy gửi lại yêu cầu.", null),
        2601 or 2627 => (409, "Dữ liệu bị trùng.", sql.Number),
        547 or 8152 or 2628 => (400, "Dữ liệu vi phạm ràng buộc hoặc vượt độ dài cho phép.", sql.Number),
        -2 or 53 or 4060 or 18456 => (503, "SQL Server chưa sẵn sàng hoặc cấu hình kết nối chưa đúng.", null),
        _ => (500, "Không thể xử lý dữ liệu.", null)
    };
}
