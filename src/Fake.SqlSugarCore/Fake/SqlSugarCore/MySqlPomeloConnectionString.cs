namespace Fake.SqlSugarCore;

/// <summary>
/// Pomelo.EntityFrameworkCore.MySql 要求被征用的连接串显式包含这两项。
/// 缺任何一项时，<c>SetDbConnection</c> 会改写连接串；连接已打开则会失败。
/// </summary>
internal static class MySqlPomeloConnectionString
{
    public static string Ensure(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString ?? string.Empty;
        }

        var value = connectionString.Trim().TrimEnd(';');
        value = SetFlag(value, "AllowUserVariables", "True");
        value = SetFlag(value, "UseAffectedRows", "False");
        return value;
    }

    private static string SetFlag(string connectionString, string key, string required)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var found = false;
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            if (!parts[i][..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parts[i] = key + "=" + required;
            found = true;
        }

        var joined = string.Join(';', parts);
        return found ? joined : joined + ";" + key + "=" + required;
    }
}
