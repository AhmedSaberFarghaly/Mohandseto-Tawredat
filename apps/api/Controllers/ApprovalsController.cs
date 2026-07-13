using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Approvals;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/approvals")]
public sealed class ApprovalsController(ApprovalService approvals) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<IReadOnlyList<ApprovalListItemDto>> Inbox([FromQuery] string? status, CancellationToken ct) => approvals.InboxAsync(UserId, status, ct);
    [HttpGet("users")] public Task<List<ApprovalUserDto>> Users(CancellationToken ct) => approvals.UsersAsync(ct);
    [HttpGet("{id:guid}")] public Task<ApprovalDetailDto> Detail(Guid id, CancellationToken ct) => approvals.DetailAsync(UserId, id, ct);
    [HttpPost("{id:guid}/approve")] public Task<ApprovalDetailDto> Approve(Guid id, ApprovalDecisionDto dto, CancellationToken ct) => approvals.ApproveAsync(UserId, id, dto.Comment, ct);
    [HttpPost("{id:guid}/reject")] public Task<ApprovalDetailDto> Reject(Guid id, ApprovalDecisionDto dto, CancellationToken ct) => approvals.RejectAsync(UserId, id, dto.Comment, ct);
    [HttpPost("{id:guid}/request-changes")] public Task<ApprovalDetailDto> RequestChanges(Guid id, ApprovalDecisionDto dto, CancellationToken ct) => approvals.RequestChangesAsync(UserId, id, dto.Comment, ct);
    [HttpPost("{id:guid}/delegate")] public Task<ApprovalDetailDto> Delegate(Guid id, ApprovalDelegateDto dto, CancellationToken ct) => approvals.DelegateAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/comments")] public Task<ApprovalDetailDto> Comment(Guid id, ApprovalCommentDto dto, CancellationToken ct) => approvals.CommentAsync(UserId, id, dto.Comment, ct);
    [HttpPost("{id:guid}/resubmit")] public Task<ApprovalDetailDto> Resubmit(Guid id, ApprovalDecisionDto dto, CancellationToken ct) => approvals.ResubmitAsync(UserId, id, dto.Comment, ct);
    [HttpPost("{id:guid}/attachments"), RequestSizeLimit(10 * 1024 * 1024 + 1024)] public Task<ApprovalAttachmentDto> Upload(Guid id, IFormFile file, CancellationToken ct) => approvals.UploadAsync(UserId, id, file, ct);
    [HttpGet("attachments/{id:guid}")] public async Task<IActionResult> Attachment(Guid id, CancellationToken ct) { var file = await approvals.FileAsync(UserId, id, ct); return PhysicalFile(file.Path, file.Type, file.Name, enableRangeProcessing: true); }
}
