using CuriosityStack.Mcp.Configuration;
using CuriosityStack.Mcp.Infrastructure.Governance;
using CuriosityStack.Mcp.Models;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Tools.Git;

/// <summary>
/// Git/Version Control domain tools.
/// Exposes repository operations via MCP protocol.
/// 
/// Tools are categorized by scope:
/// - ReadOnly: Commit history, branch listing, diff analysis
/// - Write: Commit, stage, checkout
/// - Sensitive: Merge, delete branch, force push, reset
/// </summary>
[McpServerToolType]
public sealed class GitTools
{
    private readonly IGitRepository _gitRepo;
    private readonly IApprovalGateManager _approvalGate;
    private readonly IGovernancePolicy _governance;
    private readonly IOptions<McpAgentSettings> _settings;
    private readonly ILogger<GitTools> _logger;

    public GitTools(
        IGitRepository gitRepo,
        IApprovalGateManager approvalGate,
        IGovernancePolicy governance,
        IOptions<McpAgentSettings> settings,
        ILogger<GitTools> logger)
    {
        _gitRepo = gitRepo;
        _approvalGate = approvalGate;
        _governance = governance;
        _settings = settings;
        _logger = logger;
    }

    // ======================================================================
    // READ-ONLY OPERATIONS (Scope: ReadOnly)
    // ======================================================================

    /// <summary>
    /// Get repository information (HEAD, status, recent commits).
    /// Tool: git_get_repo_info
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get repository information (current branch, status, recent commits)")]
    [McpServerTool(Name = "git_get_repo_info")]
    public async Task<string> GetRepositoryInfoAsync(
        [Description("Repository path (default: current directory)")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogInformation("[GitTools] GetRepositoryInfo for {RepoPath}", repoPath);
            
            var info = await _gitRepo.GetRepositoryInfoAsync(repoPath, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = info,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetRepositoryInfo failed");
            return ErrorResponse("GET_REPO_INFO_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get commit log / history.
    /// Tool: git_get_commit_log
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get commit history")]
    [McpServerTool(Name = "git_get_commit_log")]
    public async Task<string> GetCommitLogAsync(
        [Description("Repository path")] string? repoPath = null,
        [Description("Number of commits to retrieve (default: 10)")] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            count = Math.Max(1, Math.Min(count, 100)); // Limit to 1-100
            
            _logger.LogInformation("[GitTools] GetCommitLog for {RepoPath} (limit: {Count})", repoPath, count);
            
            var commits = await _gitRepo.GetCommitLogAsync(repoPath, count, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = commits.Length,
                data = commits,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetCommitLog failed");
            return ErrorResponse("GET_COMMIT_LOG_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get details of a specific commit.
    /// Tool: git_get_commit
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get details of a specific commit")]
    [McpServerTool(Name = "git_get_commit")]
    public async Task<string> GetCommitAsync(
        [Description("Commit hash (SHA-1)")] string commitHash,
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(commitHash))
                return ErrorResponse("INVALID_COMMIT_HASH", "Commit hash is required");
            
            _logger.LogInformation("[GitTools] GetCommit {Hash}", commitHash);
            
            var commit = await _gitRepo.GetCommitAsync(repoPath, commitHash, cancellationToken);
            
            if (commit == null)
                return ErrorResponse("COMMIT_NOT_FOUND", $"Commit {commitHash} not found");
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = commit,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetCommit failed");
            return ErrorResponse("GET_COMMIT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// List all branches.
    /// Tool: git_list_branches
    /// Scope: ReadOnly
    /// </summary>
    [Description("List all branches in the repository")]
    [McpServerTool(Name = "git_list_branches")]
    public async Task<string> ListBranchesAsync(
        [Description("Repository path")] string? repoPath = null,
        [Description("Include remote branches")] bool includeRemote = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogInformation("[GitTools] ListBranches for {RepoPath}", repoPath);
            
            var branches = await _gitRepo.ListBranchesAsync(repoPath, includeRemote, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = branches.Length,
                data = branches,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] ListBranches failed");
            return ErrorResponse("LIST_BRANCHES_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get current branch name.
    /// Tool: git_get_current_branch
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get the current branch name")]
    [McpServerTool(Name = "git_get_current_branch")]
    public async Task<string> GetCurrentBranchAsync(
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogInformation("[GitTools] GetCurrentBranch for {RepoPath}", repoPath);
            
            var branch = await _gitRepo.GetCurrentBranchAsync(repoPath, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = new { currentBranch = branch },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetCurrentBranch failed");
            return ErrorResponse("GET_CURRENT_BRANCH_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get repository status (staged, unstaged, untracked files).
    /// Tool: git_get_status
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get repository status (staged, unstaged, untracked changes)")]
    [McpServerTool(Name = "git_get_status")]
    public async Task<string> GetStatusAsync(
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogInformation("[GitTools] GetStatus for {RepoPath}", repoPath);
            
            var status = await _gitRepo.GetStatusAsync(repoPath, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = status,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetStatus failed");
            return ErrorResponse("GET_STATUS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get diff between commits or branches.
    /// Tool: git_get_diff
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get diff between two commits or branches")]
    [McpServerTool(Name = "git_get_diff")]
    public async Task<string> GetDiffAsync(
        [Description("From commit/branch (default: HEAD~1)")] string? from = null,
        [Description("To commit/branch (default: HEAD)")] string? to = null,
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogInformation("[GitTools] GetDiff {From}..{To}", from ?? "HEAD~1", to ?? "HEAD");
            
            var diffs = await _gitRepo.GetDiffAsync(repoPath, from, to, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = diffs.Length,
                data = diffs,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] GetDiff failed");
            return ErrorResponse("GET_DIFF_FAILED", ex.Message);
        }
    }

    // ======================================================================
    // WRITE OPERATIONS (Scope: Write, May Require Approval)
    // ======================================================================

    /// <summary>
    /// Stage files for commit.
    /// Tool: git_stage_files
    /// Scope: Write (non-destructive)
    /// </summary>
    [Description("Stage files for commit")]
    [McpServerTool(Name = "git_stage_files")]
    public async Task<string> StageFilesAsync(
        [Description("Array of file paths to stage (JSON array)")] string filesJson,
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            _logger.LogWarning("[GitTools] StageFiles called - WRITE operation");
            
            var files = JsonSerializer.Deserialize<string[]>(filesJson);
            if (files == null || files.Length == 0)
                return ErrorResponse("INVALID_FILES", "Files array is required");
            
            await _gitRepo.StageFilesAsync(repoPath, files, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                stagedCount = files.Length,
                message = $"Staged {files.Length} file(s)",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] StageFiles failed");
            return ErrorResponse("STAGE_FILES_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Create a commit.
    /// Tool: git_commit
    /// Scope: Write
    /// </summary>
    [Description("Create a commit with staged changes")]
    [McpServerTool(Name = "git_commit")]
    public async Task<string> CommitAsync(
        [Description("Commit message")] string message,
        [Description("Author name")] string author,
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(message))
                return ErrorResponse("INVALID_MESSAGE", "Commit message is required");
            
            if (string.IsNullOrWhiteSpace(author))
                return ErrorResponse("INVALID_AUTHOR", "Author name is required");
            
            _logger.LogWarning("[GitTools] Commit called - WRITE operation");
            
            var commit = await _gitRepo.CommitAsync(repoPath, message, author, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = commit,
                message = "Commit created successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] Commit failed");
            return ErrorResponse("COMMIT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Checkout a branch.
    /// Tool: git_checkout
    /// Scope: Write (can lose uncommitted changes)
    /// </summary>
    [Description("Checkout a branch")]
    [McpServerTool(Name = "git_checkout")]
    public async Task<string> CheckoutAsync(
        [Description("Branch name to checkout")] string branchName,
        [Description("Repository path")] string? repoPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(branchName))
                return ErrorResponse("INVALID_BRANCH", "Branch name is required");
            
            _logger.LogWarning("[GitTools] Checkout called - WRITE operation");
            
            await _gitRepo.CheckoutAsync(repoPath, branchName, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = new { checkedOutBranch = branchName },
                message = $"Checked out branch: {branchName}",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] Checkout failed");
            return ErrorResponse("CHECKOUT_FAILED", ex.Message);
        }
    }

    // ======================================================================
    // SENSITIVE OPERATIONS (Scope: Sensitive, Require Approval)
    // ======================================================================

    /// <summary>
    /// Merge a branch into the current branch.
    /// Tool: git_merge
    /// Scope: Sensitive - affects branch history, can cause conflicts
    /// </summary>
    [Description("Merge a branch into current branch (SENSITIVE - requires approval)")]
    [McpServerTool(Name = "git_merge")]
    public async Task<string> MergeAsync(
        [Description("Branch name to merge")] string branchName,
        [Description("Repository path")] string? repoPath = null,
        [Description("Approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(branchName))
                return ErrorResponse("INVALID_BRANCH", "Branch name is required");
            
            _logger.LogError("[GitTools] Merge called - SENSITIVE operation");
            
            // Check if this is a protected branch merge
            var isProtected = _settings.Value.Git.ProtectedBranches.Any(
                p => p.Equals(branchName, StringComparison.OrdinalIgnoreCase));
            
            if (isProtected && !await ValidateApprovalAsync("git", "git_merge", approvalToken, cancellationToken))
            {
                return ErrorResponse("APPROVAL_REQUIRED",
                    $"Merging into protected branch '{branchName}' requires approval");
            }
            
            var commit = await _gitRepo.MergeAsync(repoPath, branchName, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = commit,
                message = $"Merged branch: {branchName}",
                warning = "This operation modified the repository history",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] Merge failed");
            return ErrorResponse("MERGE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Delete a branch.
    /// Tool: git_delete_branch
    /// Scope: Sensitive - destructive
    /// </summary>
    [Description("Delete a branch (SENSITIVE - requires approval)")]
    [McpServerTool(Name = "git_delete_branch")]
    public async Task<string> DeleteBranchAsync(
        [Description("Branch name to delete")] string branchName,
        [Description("Repository path")] string? repoPath = null,
        [Description("Approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(branchName))
                return ErrorResponse("INVALID_BRANCH", "Branch name is required");
            
            _logger.LogError("[GitTools] DeleteBranch called - SENSITIVE operation");
            
            // Protected branches cannot be deleted
            var isProtected = _settings.Value.Git.ProtectedBranches.Any(
                p => p.Equals(branchName, StringComparison.OrdinalIgnoreCase));
            
            if (isProtected)
                return ErrorResponse("BRANCH_PROTECTED",
                    $"Cannot delete protected branch: {branchName}");
            
            if (!await ValidateApprovalAsync("git", "git_delete_branch", approvalToken, cancellationToken))
                return ErrorResponse("APPROVAL_REQUIRED", "Deleting a branch requires approval");
            
            await _gitRepo.DeleteBranchAsync(repoPath, branchName, force: false, cancellationToken: cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = new { deletedBranch = branchName },
                warning = "This operation is destructive",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] DeleteBranch failed");
            return ErrorResponse("DELETE_BRANCH_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Reset repository to a specific commit.
    /// Tool: git_reset
    /// Scope: Sensitive - destructive, loses commits
    /// </summary>
    [Description("Reset repository to a specific commit (SENSITIVE - loses commits)")]
    [McpServerTool(Name = "git_reset")]
    public async Task<string> ResetAsync(
        [Description("Commit hash to reset to")] string commitHash,
        [Description("Hard reset (discard changes)")] bool hard = true,
        [Description("Repository path")] string? repoPath = null,
        [Description("Approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            repoPath ??= _settings.Value.Git.DefaultRepoPath;
            
            if (string.IsNullOrWhiteSpace(commitHash))
                return ErrorResponse("INVALID_COMMIT", "Commit hash is required");
            
            _logger.LogError("[GitTools] Reset called (hard={Hard}) - SENSITIVE operation", hard);
            
            if (!await ValidateApprovalAsync("git", "git_reset", approvalToken, cancellationToken))
                return ErrorResponse("APPROVAL_REQUIRED", "Repository reset requires approval");
            
            await _gitRepo.ResetAsync(repoPath, commitHash, hard, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = new { resetTo = commitHash, hardReset = hard },
                warning = "This operation discards commits and is destructive",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] Reset failed");
            return ErrorResponse("RESET_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Request approval for a sensitive Git operation.
    /// Tool: git_request_approval
    /// </summary>
    [Description("Request approval for a sensitive Git operation")]
    [McpServerTool(Name = "git_request_approval")]
    public async Task<string> RequestApprovalAsync(
        [Description("Operation name (e.g., 'git_merge', 'git_delete_branch')")] string operation,
        [Description("Operation parameters as JSON")] string parametersJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[GitTools] RequestApproval for {Operation}", operation);
            
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson) 
                ?? new Dictionary<string, object>();
            
            var approvalToken = await _approvalGate.RequestApprovalAsync(
                "git",
                operation,
                OperationScope.Sensitive,
                parameters,
                cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                approvalToken = approvalToken,
                message = "Approval granted. Use token with the operation tool.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("[GitTools] Approval denied: {Reason}", ex.Message);
            return ErrorResponse("APPROVAL_DENIED", ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("[GitTools] Approval timeout");
            return ErrorResponse("APPROVAL_TIMEOUT", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GitTools] RequestApproval failed");
            return ErrorResponse("REQUEST_APPROVAL_FAILED", ex.Message);
        }
    }

    // ======================================================================
    // PRIVATE HELPERS
    // ======================================================================

    private async Task<bool> ValidateApprovalAsync(
        string domain,
        string operation,
        string? approvalToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(approvalToken))
        {
            _logger.LogWarning("[GitTools] No approval token provided for {Operation}", operation);
            return false;
        }
        
        var isValid = await _approvalGate.ValidateApprovalTokenAsync(approvalToken);
        
        if (!isValid)
        {
            _logger.LogWarning("[GitTools] Invalid approval token for {Operation}", operation);
            return false;
        }
        
        return true;
    }

    private static string ErrorResponse(string errorCode, string message)
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = errorCode,
            message = message,
            timestamp = DateTime.UtcNow
        });
    }
}
