using Microsoft.OpenApi.Models;
using System.Net.Http.Json;

namespace OpenFgaDemo.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenFGA Demo API", Version = "v1" });
        });

        builder.Services.Configure<OpenFgaOptions>(builder.Configuration.GetSection("OpenFGA"));
        builder.Services.AddHttpClient<OpenFgaHttpService>();
        builder.Services.AddSingleton<OpenFgaHttpService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapPost("/stores", async (OpenFgaHttpService svc, StoreCreateRequest body) =>
        {
            var res = await svc.CreateStoreAsync(body.Name ?? "demo-store");
            return Results.Ok(res);
        });

        app.MapPost("/model/configure", async (OpenFgaHttpService svc, ModelConfigureRequest req) =>
        {
            var res = await svc.WriteAuthorizationModelAsync(req.SchemaVersion, req.TypeDefinitions);
            return Results.Ok(res);
        });

        app.MapPost("/tuples/write", async (OpenFgaHttpService svc, TupleWriteRequest req) =>
        {
            var res = await svc.WriteTuplesAsync(req.Writes, req.Deletes);
            return Results.Ok(res);
        });

        app.MapPost("/check", async (OpenFgaHttpService svc, CheckRequestDto body) =>
        {
            var res = await svc.CheckAsync(body.User, body.Relation, body.Object);
            return Results.Ok(res);
        });

        app.MapPost("/list-objects", async (OpenFgaHttpService svc, ListObjectsRequest body) =>
        {
            var res = await svc.ListObjectsAsync(body.User, body.Type, body.Relation, body.Context);
            return Results.Ok(res);
        });

        app.MapPost("/list-users", async (OpenFgaHttpService svc, ListUsersRequest body) =>
        {
            var res = await svc.ListUsersAsync(body.Type, body.Relation, body.Object, body.Context);
            return Results.Ok(res);
        });

        app.Run();
    }
}

public record StoreCreateRequest(string? Name);
public record ModelConfigureRequest(string SchemaVersion, object TypeDefinitions);
public record TupleWrite(string User, string Relation, string Object);
public record TupleWriteRequest(IEnumerable<TupleWrite> Writes, IEnumerable<TupleWrite>? Deletes);
public record CheckRequestDto(string User, string Relation, string Object);
public record ListObjectsRequest(string User, string Type, string Relation, object? Context);
public record ListUsersRequest(string Type, string Relation, string Object, object? Context);

public class OpenFgaOptions
{
    public string? ApiUrl { get; set; }
    public string? StoreId { get; set; }
    public string? AuthorizationModelId { get; set; }
}

public class OpenFgaHttpService
{
    private readonly HttpClient _http;
    private readonly OpenFgaOptions _options;

    public OpenFgaHttpService(HttpClient httpClient, IConfiguration configuration)
    {
        _http = httpClient;
        _options = configuration.GetSection("OpenFGA").Get<OpenFgaOptions>() ?? new OpenFgaOptions();
        _http.BaseAddress = new Uri((_options.ApiUrl ?? "http://localhost:8080").TrimEnd('/') + "/");
    }

    public async Task<object> CreateStoreAsync(string name)
    {
        var resp = await _http.PostAsJsonAsync("stores", new { name });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<object>();
        return data!;
    }

    public async Task<object> WriteAuthorizationModelAsync(string schemaVersion, object typeDefinitions)
    {
        EnsureStore();
        var url = $"stores/{_options.StoreId}/authorization-models";
        var resp = await _http.PostAsJsonAsync(url, new { schema_version = schemaVersion, type_definitions = typeDefinitions });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<object>())!;
    }

    public async Task<object> WriteTuplesAsync(IEnumerable<TupleWrite> writes, IEnumerable<TupleWrite>? deletes)
    {
        EnsureStore();
        var url = $"stores/{_options.StoreId}/write";
        var body = new
        {
            writes = new
            {
                tuple_keys = writes.Select(w => new { user = w.User, relation = w.Relation, @object = w.Object })
            },
            deletes = deletes == null ? null : new
            {
                tuple_keys = deletes.Select(w => new { user = w.User, relation = w.Relation, @object = w.Object })
            }
        };
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<object>())!;
    }

    public async Task<object> CheckAsync(string user, string relation, string @object)
    {
        EnsureStore();
        var url = $"stores/{_options.StoreId}/check";
        var resp = await _http.PostAsJsonAsync(url, new { user, relation, @object });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<object>())!;
    }

    public async Task<object> ListObjectsAsync(string user, string type, string relation, object? context)
    {
        EnsureStore();
        var url = $"stores/{_options.StoreId}/list-objects";
        var resp = await _http.PostAsJsonAsync(url, new { user, type, relation, context });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<object>())!;
    }

    public async Task<object> ListUsersAsync(string type, string relation, string @object, object? context)
    {
        EnsureStore();
        var url = $"stores/{_options.StoreId}/list-users";
        var resp = await _http.PostAsJsonAsync(url, new { type, relation, @object, context });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<object>())!;
    }

    private void EnsureStore()
    {
        if (string.IsNullOrWhiteSpace(_options.StoreId))
        {
            throw new InvalidOperationException("OpenFGA: StoreId is required. Set OpenFGA:StoreId in configuration.");
        }
    }
}


