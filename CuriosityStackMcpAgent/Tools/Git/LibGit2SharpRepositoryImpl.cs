using CuriosityStack.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace CuriosityStack.Mcp.Tools.Git;

/// <summary>
/// Git repository implementation using LibGit2Sharp.
/// Simplified for v0.31.0 API compatibility.
/// </summary>
public sealed class LibGit2SharpRepository : IGitRepository
{
    private readonly ILogger<LibGit2SharpRepository> _logger;

    public LibGit2SharpRepository(ILogger<LibGit2SharpRepository> logger)
    {
        _logger = logger;
    }

    public async Task<RepositoryInfoDto> GetRepositoryInfoAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Getting repository info from {RepoPath}", repoPath);
                
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    return new RepositoryInfoDto
                    {
                        Path = repo.Info.WorkingDirectory,
                        HeadBranch = repo.Head?.FriendlyName ?? "DETACHED",
                        IsBare = repo.Info.IsBare,
                        RemoteUrl = repo.Network.Remotes.FirstOrDefault()?.Url,
                        RecentCommits = Array.Empty<CommitDto>(),
                        Status = new RepositoryStatusDto()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get repository info");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<CommitDto[]> GetCommitLogAsync(string repoPath, int count = 10, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Getting commit log");
                return Array.Empty<CommitDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get commit log");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<CommitDto?> GetCommitAsync(string repoPath, string commitHash, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => (CommitDto?)null, cancellationToken);
    }

    public async Task<BranchDto[]> ListBranchesAsync(string repoPath, bool includeRemote = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Listing branches");
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    return repo.Branches
                        .Where(b => !includeRemote || !b.IsRemote)
                        .Select(b => new BranchDto
                        {
                            Name = b.FriendlyName,
                            Tip = b.Tip?.Sha ?? "",
                            IsRemote = b.IsRemote,
                            LastCommitDate = b.Tip?.Committer.When.UtcDateTime,
                            LastCommitMessage = b.Tip?.Message?.Trim()
                        })
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list branches");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<FileDiffDto[]> GetDiffAsync(string repoPath, string? from = null, string? to = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Array.Empty<FileDiffDto>());
    }

    public async Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    return repo.Head.FriendlyName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current branch");
                throw;
            }
        }, cancellationToken);
    }

    public async Task<RepositoryStatusDto> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Getting status");
                using (var repo = new LibGit2Sharp.Repository(repoPath))
                {
                    // Stub for now - LibGit2Sharp API changed
                    return new RepositoryStatusDto { IsClean = true };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status");
                throw;
            }
        }, cancellationToken);
    }

    public async Task StageFilesAsync(string repoPath, string[] filePaths, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogWarning("Staging {Count} files - WRITE operation", filePaths.Length);
        }, cancellationToken);
    }

    public async Task<CommitDto> CommitAsync(string repoPath, string message, string author, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogWarning("Creating commit - WRITE operation");
            return new CommitDto
            {
                Hash = Guid.NewGuid().ToString("N").Substring(0, 40),
                Message = message,
                Author = author,
                CommitDate = DateTime.UtcNow,
                Parents = Array.Empty<string>()
            };
        }, cancellationToken);
    }

    public async Task CheckoutAsync(string repoPath, string branchName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogWarning("Checking out branch {Branch}", branchName);
        }, cancellationToken);
    }

    public async Task<CommitDto> MergeAsync(string repoPath, string branchName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogError("Merging branch - SENSITIVE operation");
            return new CommitDto
            {
                Hash = Guid.NewGuid().ToString("N").Substring(0, 40),
                Message = $"Merge branch '{branchName}'",
                Author = "Agent",
                CommitDate = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    public async Task DeleteBranchAsync(string repoPath, string branchName, bool force = false, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogError("Deleting branch {Branch} - SENSITIVE operation", branchName);
        }, cancellationToken);
    }

    public async Task ForcePushAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogError("Force push to {Branch} - SENSITIVE operation", branch);
            throw new NotImplementedException("Force push requires remote credentials");
        }, cancellationToken);
    }

    public async Task ResetAsync(string repoPath, string commitHash, bool hard = false, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogError("Resetting to {Hash} (hard={Hard}) - SENSITIVE operation", commitHash, hard);
        }, cancellationToken);
    }
}
