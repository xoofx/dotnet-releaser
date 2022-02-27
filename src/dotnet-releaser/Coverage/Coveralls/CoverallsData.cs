using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetReleaser.Coverage.Coveralls;

// https://docs.coveralls.io/api-reference
// POST https://coveralls.io/api/v1/jobs

public class CoverallsData
{
    public CoverallsData() : this(string.Empty)
    {
    }

    public CoverallsData(string repoToken, string serviceName = "github")
    {
        RepoToken = repoToken;
        ServiceName = serviceName;
        SourceFiles = new List<CoverallsSourceFileData>();
    }

    [JsonPropertyName("repo_token")]
    public string RepoToken { get; set; }

    [JsonPropertyName("service_name")]
    public string ServiceName { get; set; }

    [JsonPropertyName("service_number")]
    public string? ServiceNumber { get; set; }

    [JsonPropertyName("service_job_id")]
    public string? ServiceJobId { get; set; }

    [JsonPropertyName("service_pull_request")]
    public string? ServicePullRequest { get; set; }

    [JsonPropertyName("source_files")]
    public List<CoverallsSourceFileData> SourceFiles { get; }
    
    [JsonPropertyName("flag_name")]
    public string? FlagName { get; set; }
    
    [JsonPropertyName("git")]
    public GitData? Git { get; set; }

    [JsonPropertyName("commit_sha")]
    public string? CommitSha { get; set; }
    
    [JsonPropertyName("run_at")]
    public string? RunAt { get; set; }
    
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonConverterNullableInt() }
    };

    public string ToJson()
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, this, DefaultJsonSerializerOptions);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private class JsonConverterNullableInt : JsonConverter<int?>
    {
        public override bool HandleNull => true;

        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value);
            }
        }
    }
}

public class GitData
{
    public GitData()
    {
        Remotes = new List<GitRemoteData>();
    }

    [JsonPropertyName("head")]
    public GitHeadData? Head { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("remotes")]
    public List<GitRemoteData> Remotes { get; }
}

public class GitHeadData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("author_name")]
    public string? AuthorName { get; set; }
    [JsonPropertyName("author_email")]
    public string? AuthorEmail { get; set; }

    [JsonPropertyName("committer_name")]
    public string? CommitterName { get; set; }
    [JsonPropertyName("committer_email")]
    public string? CommitterEmail { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class GitRemoteData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class CoverallsSourceFileData
{
    public CoverallsSourceFileData(string name, string sourceDigest)
    {
        Name = name;
        SourceDigest = sourceDigest;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("source_digest")]
    public string SourceDigest { get; set; }
    
    [JsonPropertyName("coverage")]
    public int?[]? Coverage { get; set; }

    [JsonPropertyName("branches")]
    public int[]? Branches { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}