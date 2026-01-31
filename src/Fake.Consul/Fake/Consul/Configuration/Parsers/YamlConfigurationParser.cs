namespace Fake.Consul.Configuration.Parsers;

/// <summary>
/// 轻量级 YAML 配置解析器，支持基本的键值对和嵌套结构
/// </summary>
public class YamlConfigurationParser : IConfigurationParser
{
    private readonly Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _paths = new();

    public IDictionary<string, string?> Parse(Stream stream)
    {
        _data.Clear();
        _paths.Clear();

        using var reader = new StreamReader(stream);
        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            ProcessLine(line, lineNumber);
        }

        return _data;
    }

    private void ProcessLine(string line, int lineNumber)
    {
        // 跳过空行和注释
        var trimmed = line.TrimEnd();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.TrimStart().StartsWith('#'))
        {
            return;
        }

        var indent = GetIndent(line);
        var content = trimmed.TrimStart();

        // 根据缩进调整路径栈
        AdjustPathStack(indent);

        // 解析键值对
        var colonIndex = content.IndexOf(':');
        if (colonIndex <= 0) return;

        var key = content[..colonIndex].Trim();
        var value = colonIndex < content.Length - 1 ? content[(colonIndex + 1)..].Trim() : string.Empty;

        // 移除引号
        value = RemoveQuotes(value);

        // 移除行内注释
        value = RemoveInlineComment(value);

        _paths.Push(key);

        if (!string.IsNullOrEmpty(value))
        {
            // 有值，添加到字典
            var fullKey = string.Join(":", _paths.Reverse());
            _data[fullKey] = value;
            _paths.Pop();
        }
        // 无值说明是父节点，保留在栈中等待子节点
    }

    private int GetIndent(string line)
    {
        var indent = 0;
        foreach (var c in line)
        {
            if (c == ' ') indent++;
            else if (c == '\t') indent += 2; // tab 算 2 空格
            else break;
        }
        return indent / 2; // 每 2 空格算一级
    }

    private void AdjustPathStack(int targetLevel)
    {
        while (_paths.Count > targetLevel)
        {
            _paths.Pop();
        }
    }

    private static string RemoveQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }
        return value;
    }

    private static string RemoveInlineComment(string value)
    {
        // 简单处理行内注释（不在引号内的 #）
        var hashIndex = value.IndexOf(" #", StringComparison.Ordinal);
        return hashIndex > 0 ? value[..hashIndex].TrimEnd() : value;
    }
}
