using CuriosityStack.Mcp.Models;

namespace CuriosityStack.Mcp.Tools.Git;

/// <summary>
/// Contract for Git repository operations.
/// Abstracts the underlying Git implementation (LibGit2Sharp, etc).
/// </summary>
public interface IGitRepository
{
    Task<RepositoryInfoDto> GetRepositoryInfoAsync(string repoPath, CancellationToken cancellationToken = default);
    Task<CommitDto[]> GetCommitLogAsync(string repoPath, int count = 10, CancellationToken cancellationToken = default);
    Task<CommitDto?> GetCommitAsync(string repoPath, string commitHash, CancellationToken cancellationToken = default);
    Task<BranchDto[]> ListBranchesAsync(string repoPath, bool includeRemote = false, CancellationToken cancellationToken = default);
    Task<FileDiffDto[]> GetDiffAsync(string repoPath, string? from = null, string? to = null, CancellationToken cancellationToken = default);
    Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default);
    Task<RepositoryStatusDto> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default);
    Task StageFilesAsync(string repoPath, string[] filePaths, CancellationToken cancellationToken = default);
    Task<CommitDto> CommitAsync(string repoPath, string message, string author, CancellationToken cancellationToken = default);
    Task CheckoutAsync(string repoPath, string branchName, CancellationToken cancellationToken = default);
    Task<CommitDto> MergeAsync(string repoPath, string branchName, CancellationToken cancellationToken = default);
    Task DeleteBranchAsync(string repoPath, string branchName, bool force = false, CancellationToken cancellationToken = default);
    Task ForcePushAsync(string repoPath, string branch, CancellationToken cancellationToken = default);
    Task ResetAsync(string repoPath, string commitHash, bool hard = false, CancellationToken cancellationToken = default);
}