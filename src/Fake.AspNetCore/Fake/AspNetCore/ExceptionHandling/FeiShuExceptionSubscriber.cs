using System.Text;
using Fake.Application;
using Fake.DependencyInjection;
using Fake.ExceptionHandling;
using Fake.FeiShu;
using Fake.Security.Claims;
using Fake.Users;

namespace Fake.AspNetCore.ExceptionHandling;

public class FeiShuExceptionSubscriber(
    IFeiShuNotificationService notificationService,
    ICurrentUser currentUser,
    IOptionsMonitor<FeiShuNoticeOptions> optionsMonitor) : IExceptionSubscriber, ITransientDependency
{
    private readonly FeiShuNoticeOptions _options = optionsMonitor.CurrentValue;

    public async Task HandleAsync(ExceptionNotificationContext context)
    {
        if (!_options.EnableFeiShuExceptionSubscribe) return;

        var httpContext = context.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;

        if (httpContext is null)
        {
            notificationService.Enqueue($@"error: {context.Exception}", LogLevel.Error);
        }

        var request = httpContext!.Request;

        // 1. 读取请求路径
        var path = request.Path; // 例如: /api/users
        var queryString = request.QueryString; // 例如: ?id=123
        var fullPath = $"{path}{queryString}";

        // 2. 读取请求方法
        var method = request.Method; // GET, POST, PUT, etc.

        var message =
            $@"{method} {fullPath} by {currentUser.UserName}[{currentUser.FindClaimOrNull(FakeClaimTypes.UserId)?.Value ?? "未登录"}]";

        // 3. 读取请求头
        if (_options.WriteHeader)
        {
            message += $"\nheaders: {request.Headers}";
        }

        // 4. 读取 Body (重要：需要启用缓冲)
        if (_options.WriteBody)
        {
            request.EnableBuffering();

            // 如果 Body 已经被其他中间件读取过，需要先重置：
            request.Body.Position = 0;

            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // 重置流位置，以便后续中间件可以读取

            message += $"\nbody: {body}";
        }

        message += $"\nexception: {context.Exception.Message}";

        if (_options.WriteStack && context.Exception is not BusinessException)
        {
            message += $"\nstack: {context.Exception.StackTrace}";
        }

        notificationService.Enqueue(message,
            context.Exception is BusinessException ? LogLevel.Warning : LogLevel.Error);
    }
}