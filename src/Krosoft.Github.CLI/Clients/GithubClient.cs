using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Krosoft.Github.CLI.Interfaces;
using Krosoft.Github.CLI.Models;

namespace Krosoft.Github.CLI.Clients;

internal sealed class GithubClient : IGithubClient
{
    private const string DefaultApiUrl = "https://api.github.com";
    private const string ApiVersion = "2022-11-28";
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _owner;

    public GithubClient(GithubProfile profile)
    {
        _apiUrl = (profile.ApiUrl ?? DefaultApiUrl).TrimEnd('/');
        _owner = profile.Owner.Trim();

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", profile.Pat);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Krosoft.Github.CLI", "1.0"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", ApiVersion);
    }

    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        // Un owner peut être une organisation ou un utilisateur : on tente l'endpoint org puis on bascule sur user.
        var orgUrl = $"{_apiUrl}/orgs/{Uri.EscapeDataString(_owner)}/repos?per_page={PageSize}&type=all";

        using var probe = await _httpClient.GetAsync(orgUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var firstUrl = probe.StatusCode == HttpStatusCode.NotFound
            ? $"{_apiUrl}/users/{Uri.EscapeDataString(_owner)}/repos?per_page={PageSize}&type=owner"
            : orgUrl;

        var repositories = await GetPagedAsync<Repository>(firstUrl, cancellationToken);

        return repositories.Where(r => !r.Archived)
                           .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                           .ToList();
    }

    public async Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string repository, PullRequestState state, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(repository)}/pulls" +
                  $"?state={state.ToString().ToLowerInvariant()}&per_page={PageSize}";

        return await GetPagedAsync<PullRequest>(url, cancellationToken);
    }

    public async Task<string> GetCurrentUserLoginAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{_apiUrl}/user", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var user = await response.Content.ReadFromJsonAsync<User>(JsonOptions, cancellationToken);
        if (user is { Login.Length: > 0 })
        {
            return user.Login;
        }

        throw new InvalidOperationException("Impossible de déterminer l'utilisateur associé au PAT.");
    }

    public async Task<IReadOnlyList<Review>> GetReviewsAsync(PullRequest pullRequest, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(pullRequest.RepositoryName)}" +
                  $"/pulls/{pullRequest.Number}/reviews?per_page={PageSize}";

        return await GetPagedAsync<Review>(url, cancellationToken);
    }

    public async Task ApproveAsync(PullRequest pullRequest, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(pullRequest.RepositoryName)}" +
                  $"/pulls/{pullRequest.Number}/reviews";

        using var response = await _httpClient.PostAsJsonAsync(url, new { @event = "APPROVE" }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task MergeAsync(PullRequest pullRequest, string mergeMethod, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(pullRequest.RepositoryName)}" +
                  $"/pulls/{pullRequest.Number}/merge";

        using var response = await _httpClient.PutAsJsonAsync(url, new { merge_method = mergeMethod }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowRun>> GetWorkflowRunsForCommitAsync(PullRequest pullRequest, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(pullRequest.RepositoryName)}" +
                  $"/actions/runs?head_sha={Uri.EscapeDataString(pullRequest.Head.Sha)}&per_page={PageSize}";

        return await GetWorkflowRunsAsync(url, cancellationToken);
    }

    public async Task RerunFailedJobsAsync(PullRequest pullRequest, long runId, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(pullRequest.RepositoryName)}" +
                  $"/actions/runs/{runId}/rerun-failed-jobs";

        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowRun>> GetActiveWorkflowRunsAsync(string repository, CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{_apiUrl}/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(repository)}/actions/runs";

        var inProgress = await GetWorkflowRunsAsync($"{baseUrl}?status=in_progress&per_page={PageSize}", cancellationToken);
        var queued = await GetWorkflowRunsAsync($"{baseUrl}?status=queued&per_page={PageSize}", cancellationToken);

        return inProgress.Concat(queued).ToList();
    }

    public void Dispose() => _httpClient.Dispose();

    // GitHub encapsule les runs dans un objet { total_count, workflow_runs }.
    private async Task<IReadOnlyList<WorkflowRun>> GetWorkflowRunsAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var page = await response.Content.ReadFromJsonAsync<WorkflowRunsResponse>(JsonOptions, cancellationToken);
        return page?.WorkflowRuns ?? [];
    }

    // Endpoints renvoyant un tableau JSON, paginés via l'en-tête Link (rel="next").
    private async Task<List<T>> GetPagedAsync<T>(string firstUrl, CancellationToken cancellationToken)
    {
        var result = new List<T>();
        var url = firstUrl;

        while (url is not null)
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var page = await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken);
            if (page is { Count: > 0 })
            {
                result.AddRange(page);
            }

            url = NextLink(response);
        }

        return result;
    }

    private static string? NextLink(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        // Format : <https://api.github.com/...&page=2>; rel="next", <...>; rel="last"
        foreach (var part in string.Join(",", values).Split(','))
        {
            var segments = part.Split(';');
            if (segments.Length < 2 || !segments[1].Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var raw = segments[0].Trim().TrimStart('<').TrimEnd('>');
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }

        return null;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = TryExtractMessage(body) ?? body;
        throw new HttpRequestException($"Erreur HTTP {(int)response.StatusCode} ({response.StatusCode}) : {detail}");
    }

    private static string? TryExtractMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
