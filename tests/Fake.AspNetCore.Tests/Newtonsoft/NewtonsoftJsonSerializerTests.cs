using System.Text.Json.Serialization;
using Fake.AspNetCore.Newtonsoft;
using Fake.Json;
using Fake.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Fake.AspNetCore.Tests.Newtonsoft;

public class NewtonsoftJsonSerializerTests : AspNetCoreTestBase
{
    private readonly IFakeJsonSerializer _jsonSerializer;

    public NewtonsoftJsonSerializerTests()
    {
        _jsonSerializer = ServiceProvider.GetRequiredService<IFakeJsonSerializer>();
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<FakeJsonSerializerOptions>(options =>
        {
            options.InputDateTimeFormats.Add("yyyy-MM-dd HH:mm:ss");
            options.LongToString = true;
        });
        services.Configure<FakeClockOptions>(options => { options.Kind = DateTimeKind.Utc; });

        services.AddFakeNewtonsoft();
    }

    [Fact]
    public void 应使用Newtonsoft序列化器()
    {
        _jsonSerializer.ShouldBeOfType<FakeNewtonsoftJsonSerializer>();
    }

    [Fact]
    public void 序列化默认使用驼峰命名()
    {
        var student = new Student
        {
            Id = 1293829749328751111,
            Name = "张三",
            Is18 = true,
            IgnoreMember = "IgnoreMember"
        };

        var json = _jsonSerializer.Serialize(student);

        json.ShouldBe("{\"id\":\"1293829749328751111\",\"name\":\"张三\",\"is18\":true}");
    }

    [Fact]
    public void 序列化可关闭驼峰命名()
    {
        var student = new Student
        {
            Id = 1293829749328751111,
            Name = "张三",
            Is18 = true
        };

        var json = _jsonSerializer.Serialize(student, false);

        json.ShouldBe("{\"Id\":\"1293829749328751111\",\"Name\":\"张三\",\"Is18\":true}");
    }

    [Fact]
    public void 序列化可格式化输出()
    {
        var student = new Student
        {
            Id = 1293829749328751111,
            Name = "张三",
            Is18 = true
        };

        var json = _jsonSerializer.Serialize(student, indented: true);

        json.ShouldContain(Environment.NewLine);
        json.ShouldContain("  \"id\": \"1293829749328751111\"");
    }

    [Fact]
    public void 反序列化支持字符串long()
    {
        var json = "{\"id\":\"1293829749328751111\",\"name\":\"张三\",\"is18\":true}";

        var student = _jsonSerializer.Deserialize<Student>(json);

        student.ShouldNotBeNull();
        student.Id.ShouldBe(1293829749328751111);
        student.Name.ShouldBe("张三");
        student.Is18.ShouldBeTrue();
    }

    [Fact]
    public void 反序列化支持指定类型()
    {
        var json = "{\"id\":\"1293829749328751111\",\"name\":\"张三\",\"is18\":true}";

        var student = _jsonSerializer.Deserialize(json, typeof(Student)) as Student;

        student.ShouldNotBeNull();
        student.Id.ShouldBe(1293829749328751111);
        student.Name.ShouldBe("张三");
        student.Is18.ShouldBeTrue();
    }

    [Fact]
    public void 字典Key不使用驼峰命名()
    {
        var json = _jsonSerializer.Serialize(new Dictionary<string, int>
        {
            ["UserName"] = 1
        });

        json.ShouldBe("{\"UserName\":1}");
    }

    [Fact]
    public void JsonPropertyName不影响Newtonsoft序列化()
    {
        var student = new NewStudent
        {
            Sex = "男"
        };

        var json = _jsonSerializer.Serialize(student);

        json.ShouldBe("{\"sex\":\"男\"}");
    }

    [Fact]
    public void DateTime序列化使用统一格式()
    {
        var datetime = new DatetimeClass
        {
            Value = new DateTime(2021, 1, 1, 1, 1, 1)
        };

        var json = _jsonSerializer.Serialize(datetime);

        json.ShouldBe("{\"value\":\"2021-01-01 01:01:01\"}");
    }

    [Fact]
    public void DisableClockNormalization属性不使用统一DateTime格式()
    {
        var datetime = new DisableClockNormalizationDatetimeClass
        {
            Value = new DateTime(2021, 1, 1, 1, 1, 1)
        };

        var json = _jsonSerializer.Serialize(datetime);

        json.ShouldBe("{\"value\":\"2021-01-01 01:01:01\"}");
    }

    [Fact]
    public void DateTime反序列化支持配置格式()
    {
        var json = "{\"value\":\"2021-01-01 01:01:01\"}";

        var datetime = _jsonSerializer.Deserialize<DatetimeClass>(json);

        datetime.ShouldNotBeNull();
        datetime.Value.ShouldBe(new DateTime(2021, 1, 1, 1, 1, 1));
        datetime.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void DateTime反序列化支持Iso格式()
    {
        var json = "{\"value\":\"2021-01-01T01:01:01\"}";

        var datetime = _jsonSerializer.Deserialize<DatetimeClass>(json);

        datetime.ShouldNotBeNull();
        datetime.Value.ShouldBe(new DateTime(2021, 1, 1, 1, 1, 1));
    }

    [Fact]
    public void 可空DateTime序列化()
    {
        var datetime = new NullableDatetimeClass
        {
            Value = new DateTime(2021, 1, 1, 1, 1, 1)
        };

        var json = _jsonSerializer.Serialize(datetime);

        json.ShouldBe("{\"value\":\"2021-01-01 01:01:01\"}");
    }

    [Fact]
    public void 可空DateTime序列化Null()
    {
        var datetime = new NullableDatetimeClass
        {
            Value = null
        };

        var json = _jsonSerializer.Serialize(datetime);

        json.ShouldBe("{\"value\":null}");
    }

    [Fact]
    public void 可空DateTime反序列化Null()
    {
        var json = "{\"value\":null}";

        var datetime = _jsonSerializer.Deserialize<NullableDatetimeClass>(json);

        datetime.ShouldNotBeNull();
        datetime.Value.ShouldBeNull();
    }

    class Student
    {
        public long? Id { get; set; }

        public string Name { get; set; }

        public bool Is18 { get; set; }

        [global::Newtonsoft.Json.JsonIgnore]
        public string IgnoreMember { get; set; }
    }

    class NewStudent
    {
        [JsonPropertyName("gender")]
        public string Sex { get; set; }
    }

    class DatetimeClass
    {
        public DateTime Value { get; set; }
    }

    class NullableDatetimeClass
    {
        public DateTime? Value { get; set; }
    }

    class DisableClockNormalizationDatetimeClass
    {
        [DisableClockNormalization]
        public DateTime Value { get; set; }
    }
}