using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.IRepositories;
using Bms.System.Infrastructure;

namespace Bms.System.Application.Services;

/// <summary>
/// 消息应用服务实现
/// 负责消息发送、接收者解析、撤回、收件箱查询等业务逻辑。
/// 跨租户消息：平台管理员发送时为每个目标租户创建一条 Message，接收者在目标租户范围内解析。
/// </summary>
public class MessageAppService : IMessageAppService
{
    /// <summary>
    /// 平台租户ID（固定值）
    /// 平台租户角色（如 super_admin、tenant_admin）归属此租户，
    /// 但绑定该角色的用户散落在各个普通租户下，按角色发送消息时需跨租户查询。
    /// </summary>
    private const long PlatformTenantId = 1;

    private readonly IMessageRepository _messageRepository;
    private readonly IMessageRecipientRepository _recipientRepository;
    private readonly IMessagePusher _messagePusher;
    private readonly ICurrentUser _currentUser;
    private readonly SystemDbContext _context;
    private readonly IOrganizationRepository _organizationRepository;

    public MessageAppService(
        IMessageRepository messageRepository,
        IMessageRecipientRepository recipientRepository,
        IMessagePusher messagePusher,
        ICurrentUser currentUser,
        SystemDbContext context,
        IOrganizationRepository organizationRepository)
    {
        _messageRepository = messageRepository;
        _recipientRepository = recipientRepository;
        _messagePusher = messagePusher;
        _currentUser = currentUser;
        _context = context;
        _organizationRepository = organizationRepository;
    }

    // ========================================================
    // 用户端
    // ========================================================

    public async Task<ApiResponseDto<PagedResponseDto<MessageInboxDto>>> GetInboxAsync(MessageInboxQueryDto query)
    {
        var userId = _currentUser.UserId ?? 0;
        var tenantId = _currentUser.TenantId ?? 0;
        if (userId == 0 || tenantId == 0)
        {
            return ApiResponseDto<PagedResponseDto<MessageInboxDto>>.Fail("用户未登录", 401);
        }

        var recipients = await _recipientRepository.GetInboxAsync(
            userId, tenantId,
            query.PageIndex, query.PageSize,
            query.Category, query.IsRead, query.StartTime, query.EndTime);

        var total = await _recipientRepository.GetInboxCountAsync(
            userId, tenantId,
            query.Category, query.IsRead, query.StartTime, query.EndTime);

        // 批量获取关联 Message 信息
        var messageIds = recipients.Select(r => r.MessageId).Distinct().ToList();
        var messages = await _context.Set<Message>()
            .Where(m => messageIds.Contains(m.Id) && !m.IsRecalled)
            .ToDictionaryAsync(m => m.Id);

        var list = recipients
            .Where(r => messages.ContainsKey(r.MessageId)) // 排除已撤回的消息
            .Select(r =>
            {
                var msg = messages[r.MessageId];
                return new MessageInboxDto
                {
                    Id = r.Id,
                    MessageId = r.MessageId,
                    Title = msg.Title,
                    Content = msg.Content,
                    Category = (int)msg.Category,
                    CategoryName = GetCategoryName(msg.Category),
                    SenderName = msg.SenderName,
                    TargetUrl = msg.TargetUrl,
                    IsRead = r.IsRead,
                    ReadTime = r.ReadTime,
                    CreatedTime = r.CreatedTime
                };
            })
            .ToList();

        var result = new PagedResponseDto<MessageInboxDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };

        return ApiResponseDto<PagedResponseDto<MessageInboxDto>>.Success(result);
    }

    public async Task<ApiResponseDto<int>> GetUnreadCountAsync()
    {
        var userId = _currentUser.UserId ?? 0;
        var tenantId = _currentUser.TenantId ?? 0;
        if (userId == 0 || tenantId == 0)
        {
            return ApiResponseDto<int>.Success(0);
        }

        var count = await _recipientRepository.GetUnreadCountAsync(userId, tenantId);
        return ApiResponseDto<int>.Success(count);
    }

    public async Task<ApiResponseDto<MessageInboxDto?>> GetMessageAsync(long recipientId)
    {
        var userId = _currentUser.UserId ?? 0;
        var recipient = await _recipientRepository.GetByIdAsync(recipientId);

        if (recipient == null || recipient.UserId != userId || recipient.IsDeleted)
        {
            return ApiResponseDto<MessageInboxDto?>.Fail("消息不存在", 404);
        }

        var message = await _messageRepository.GetByIdAsync(recipient.MessageId);
        if (message == null || message.IsRecalled)
        {
            return ApiResponseDto<MessageInboxDto?>.Fail("消息已撤回", 404);
        }

        var dto = new MessageInboxDto
        {
            Id = recipient.Id,
            MessageId = recipient.MessageId,
            Title = message.Title,
            Content = message.Content,
            Category = (int)message.Category,
            CategoryName = GetCategoryName(message.Category),
            SenderName = message.SenderName,
            TargetUrl = message.TargetUrl,
            IsRead = recipient.IsRead,
            ReadTime = recipient.ReadTime,
            CreatedTime = recipient.CreatedTime
        };

        return ApiResponseDto<MessageInboxDto?>.Success(dto);
    }

    public async Task<ApiResponseDto<bool>> MarkAsReadAsync(long recipientId)
    {
        var userId = _currentUser.UserId ?? 0;
        await _recipientRepository.MarkAsReadAsync(recipientId, userId);
        return ApiResponseDto<bool>.Success(true);
    }

    public async Task<ApiResponseDto<bool>> BatchMarkAsReadAsync(MessageBatchReadDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        await _recipientRepository.BatchMarkAsReadAsync(dto.Ids, userId);
        return ApiResponseDto<bool>.Success(true);
    }

    public async Task<ApiResponseDto<bool>> MarkAllAsReadAsync()
    {
        var userId = _currentUser.UserId ?? 0;
        var tenantId = _currentUser.TenantId ?? 0;
        await _recipientRepository.MarkAllAsReadAsync(userId, tenantId);
        return ApiResponseDto<bool>.Success(true);
    }

    public async Task<ApiResponseDto<bool>> DeleteAsync(long recipientId)
    {
        var userId = _currentUser.UserId ?? 0;
        await _recipientRepository.SoftDeleteAsync(recipientId, userId);
        return ApiResponseDto<bool>.Success(true);
    }

    public async Task<ApiResponseDto<bool>> BatchDeleteAsync(MessageBatchDeleteDto dto)
    {
        var userId = _currentUser.UserId ?? 0;
        await _recipientRepository.BatchSoftDeleteAsync(dto.Ids, userId);
        return ApiResponseDto<bool>.Success(true);
    }

    // ========================================================
    // 管理端
    // ========================================================

    public async Task<ApiResponseDto<PagedResponseDto<MessageDto>>> GetSentMessagesAsync(MessageQueryDto query)
    {
        // 平台管理员可查所有租户，指定 TenantId 时按该租户筛选；租户管理员仅查本租户（忽略前端传入的 TenantId 防止越权）
        long? tenantId;
        if (_currentUser.IsSuperAdmin)
        {
            tenantId = query.TenantId;
        }
        else
        {
            tenantId = _currentUser.TenantId;
        }

        var messages = await _messageRepository.GetSentListAsync(
            query.PageIndex, query.PageSize, tenantId,
            query.Category, query.SourceType, query.IsRecalled, query.StartTime, query.EndTime);

        var total = await _messageRepository.GetSentCountAsync(
            tenantId, query.Category, query.SourceType, query.IsRecalled, query.StartTime, query.EndTime);

        var subsystemNames = await GetSubsystemNameMapAsync();
        // 批量查询归属租户名称，避免 N+1 查询
        var tenantIds = messages.Select(m => m.TenantId).Distinct().ToList();
        var tenantNames = await _context.Tenants
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);
        var list = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            Title = m.Title,
            Content = m.Content,
            Category = (int)m.Category,
            CategoryName = GetCategoryName(m.Category),
            SourceType = (int)m.SourceType,
            SourceTypeName = GetSourceTypeName(m.SourceType),
            SourceSubsystemCode = m.SourceSubsystemCode,
            SourceSubsystemName = subsystemNames.TryGetValue(m.SourceSubsystemCode, out var subName) ? subName : m.SourceSubsystemCode,
            TargetType = (int)m.TargetType,
            TargetTypeName = GetTargetTypeName(m.TargetType),
            TargetDesc = m.TargetDesc,
            SenderId = m.SenderId,
            SenderName = m.SenderName,
            TargetUrl = m.TargetUrl,
            IsCrossTenant = m.IsCrossTenant,
            TenantName = tenantNames.TryGetValue(m.TenantId, out var tName) ? tName : string.Empty,
            IsRecalled = m.IsRecalled,
            CreatedTime = m.CreatedTime
        }).ToList();

        var result = new PagedResponseDto<MessageDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };

        return ApiResponseDto<PagedResponseDto<MessageDto>>.Success(result);
    }

    public async Task<ApiResponseDto<MessageDto?>> GetSentMessageAsync(long messageId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            return ApiResponseDto<MessageDto?>.Fail("消息不存在", 404);
        }

        // 租户管理员只能查看本租户消息
        if (!_currentUser.IsSuperAdmin && message.TenantId != _currentUser.TenantId)
        {
            return ApiResponseDto<MessageDto?>.Fail("无权查看", 403);
        }

        var subsystemNames = await GetSubsystemNameMapAsync();
        var dto = new MessageDto
        {
            Id = message.Id,
            Title = message.Title,
            Content = message.Content,
            Category = (int)message.Category,
            CategoryName = GetCategoryName(message.Category),
            SourceType = (int)message.SourceType,
            SourceTypeName = GetSourceTypeName(message.SourceType),
            SourceSubsystemCode = message.SourceSubsystemCode,
            SourceSubsystemName = subsystemNames.TryGetValue(message.SourceSubsystemCode, out var subName) ? subName : message.SourceSubsystemCode,
            TargetType = (int)message.TargetType,
            TargetTypeName = GetTargetTypeName(message.TargetType),
            TargetDesc = message.TargetDesc,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            TargetUrl = message.TargetUrl,
            IsCrossTenant = message.IsCrossTenant,
            IsRecalled = message.IsRecalled,
            CreatedTime = message.CreatedTime
        };

        return ApiResponseDto<MessageDto?>.Success(dto);
    }

    public async Task<ApiResponseDto<MessageDto?>> SendAsync(MessageSendDto dto)
    {
        var senderId = _currentUser.UserId;
        var senderName = _currentUser.RealName ?? _currentUser.UserName ?? "管理员";
        var isSuperAdmin = _currentUser.IsSuperAdmin;
        var currentTenantId = _currentUser.TenantId ?? 0;

        // 判断是否跨租户：平台管理员选择按租户发送，或指定了目标租户列表
        var isCrossTenant = isSuperAdmin
            && (dto.TargetType == (int)MessageTargetType.Tenant
                || (dto.TargetTenantIds != null && dto.TargetTenantIds.Count > 0));

        // 确定要创建 Message 的租户列表
        List<long> targetTenantIds;
        var targetType = (MessageTargetType)dto.TargetType;

        if (targetType == MessageTargetType.Tenant)
        {
            // 按租户发送：每个 TargetIds 中的租户创建一条全员消息
            targetTenantIds = dto.TargetIds;
            targetType = MessageTargetType.All;
        }
        else if (isCrossTenant && dto.TargetTenantIds != null && dto.TargetTenantIds.Count > 0)
        {
            // 跨租户 + 角色/组织/用户：在指定租户范围内解析
            targetTenantIds = dto.TargetTenantIds;
        }
        else
        {
            // 租户内消息
            targetTenantIds = new List<long> { currentTenantId };
        }

        // 识别目标角色中归属平台租户的角色（如 super_admin、tenant_admin）。
        // 这类角色归属平台租户（Role.TenantId=1），但绑定该角色的用户散落在各个普通租户下。
        // 处理策略：统一在平台租户的 Message 下跨租户查询平台角色用户，避免在多个目标租户的
        // Message 下为同一用户重复创建接收记录。普通租户角色仍按 Message.TenantId 过滤。
        HashSet<long> platformRoleIds = new();
        List<long> normalRoleIds = dto.TargetIds;
        if (targetType == MessageTargetType.Role)
        {
            var platformRoleIdsInDb = await _context.Roles
                .Where(r => dto.TargetIds.Contains(r.Id) && r.TenantId == PlatformTenantId)
                .Select(r => r.Id)
                .ToListAsync();
            if (platformRoleIdsInDb.Count > 0)
            {
                platformRoleIds = new HashSet<long>(platformRoleIdsInDb);
                normalRoleIds = dto.TargetIds.Except(platformRoleIds).ToList();
                // 确保在平台租户下创建一条 Message，用于跨租户查询所有平台角色用户
                if (!targetTenantIds.Contains(PlatformTenantId))
                {
                    targetTenantIds = new List<long>(targetTenantIds) { PlatformTenantId };
                }
            }
        }

        if (targetTenantIds.Count == 0)
        {
            return ApiResponseDto<MessageDto?>.Fail("目标租户列表不能为空");
        }

        var targetDesc = await BuildTargetDescAsync(targetType, dto.TargetIds, targetTenantIds);

        // 为每个目标租户创建一条 Message 并展开接收者
        var firstMessageId = 0L;
        foreach (var tenantId in targetTenantIds)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            var tenantCode = tenant?.Code ?? string.Empty;

            var message = new Message
            {
                Title = dto.Title,
                Content = dto.Content,
                Category = (MessageCategory)dto.Category,
                SourceType = MessageSourceType.Manual,
                SourceSubsystemCode = "System",
                TargetType = targetType,
                TargetIds = string.Join(",", dto.TargetIds),
                TargetDesc = targetDesc,
                SenderId = senderId,
                SenderName = senderName,
                TargetUrl = dto.TargetUrl ?? string.Empty,
                IsCrossTenant = isCrossTenant,
                IsRecalled = false,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = DateTime.Now
            };

            await _messageRepository.AddAsync(message);
            if (firstMessageId == 0) firstMessageId = message.Id;

            // 按角色发送时，根据 Message 归属租户选择 effectiveTargetIds：
            // - 平台租户的 Message：使用 dto.TargetIds（含平台角色和普通角色），由 ResolveRecipientsAsync
            //   跨租户查询平台角色用户
            // - 普通租户的 Message：使用 normalRoleIds（排除平台角色），避免平台角色用户在此重复创建接收记录
            List<long> effectiveTargetIds = targetType == MessageTargetType.Role && tenantId != PlatformTenantId
                ? normalRoleIds
                : dto.TargetIds;

            // 解析接收者并展开
            var recipients = await ResolveRecipientsAsync(message, effectiveTargetIds);
            if (recipients.Count > 0)
            {
                await _recipientRepository.BatchInsertAsync(recipients);

                // 通过 SignalR 推送给每个接收者
                foreach (var recipient in recipients)
                {
                    var payload = new MessageInboxDto
                    {
                        Id = recipient.Id,
                        MessageId = message.Id,
                        Title = message.Title,
                        Content = message.Content,
                        Category = (int)message.Category,
                        CategoryName = GetCategoryName(message.Category),
                        SenderName = message.SenderName,
                        TargetUrl = message.TargetUrl,
                        IsRead = false,
                        ReadTime = null,
                        CreatedTime = message.CreatedTime
                    };
                    await _messagePusher.PushToUserAsync(recipient.UserId, payload);
                }
            }
        }

        // 返回首条 Message 记录（管理端展示）
        var firstMessage = await _messageRepository.GetByIdAsync(firstMessageId);
        if (firstMessage == null)
        {
            return ApiResponseDto<MessageDto?>.Success(null);
        }

        var subsystemNames = await GetSubsystemNameMapAsync();
        var resultDto = new MessageDto
        {
            Id = firstMessage.Id,
            Title = firstMessage.Title,
            Content = firstMessage.Content,
            Category = (int)firstMessage.Category,
            CategoryName = GetCategoryName(firstMessage.Category),
            SourceType = (int)firstMessage.SourceType,
            SourceTypeName = GetSourceTypeName(firstMessage.SourceType),
            SourceSubsystemCode = firstMessage.SourceSubsystemCode,
            SourceSubsystemName = subsystemNames.TryGetValue(firstMessage.SourceSubsystemCode, out var subName) ? subName : firstMessage.SourceSubsystemCode,
            TargetType = (int)firstMessage.TargetType,
            TargetTypeName = GetTargetTypeName(firstMessage.TargetType),
            TargetDesc = firstMessage.TargetDesc,
            SenderId = firstMessage.SenderId,
            SenderName = firstMessage.SenderName,
            TargetUrl = firstMessage.TargetUrl,
            IsCrossTenant = firstMessage.IsCrossTenant,
            IsRecalled = firstMessage.IsRecalled,
            CreatedTime = firstMessage.CreatedTime
        };

        return ApiResponseDto<MessageDto?>.Success(resultDto);
    }

    public async Task<ApiResponseDto<bool>> RecallAsync(long messageId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            return ApiResponseDto<bool>.Fail("消息不存在", 404);
        }

        // 租户管理员只能撤回本租户消息
        if (!_currentUser.IsSuperAdmin && message.TenantId != _currentUser.TenantId)
        {
            return ApiResponseDto<bool>.Fail("无权撤回", 403);
        }

        message.IsRecalled = true;
        await _messageRepository.UpdateAsync(message);

        // 逻辑删除所有关联的接收记录（收件箱消失）
        await _recipientRepository.SoftDeleteByMessageIdAsync(messageId);

        return ApiResponseDto<bool>.Success(true);
    }

    public async Task<ApiResponseDto<MessageReadStatsDto>> GetReadStatsAsync(long messageId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            return ApiResponseDto<MessageReadStatsDto>.Fail("消息不存在", 404);
        }

        // 租户管理员只能查看本租户消息的统计
        if (!_currentUser.IsSuperAdmin && message.TenantId != _currentUser.TenantId)
        {
            return ApiResponseDto<MessageReadStatsDto>.Fail("无权查看", 403);
        }

        var data = await _recipientRepository.GetReadStatsAsync(messageId);

        var dto = new MessageReadStatsDto
        {
            TotalCount = data.TotalCount,
            ReadCount = data.ReadCount,
            UnreadCount = data.UnreadCount,
            UnreadList = data.UnreadList.Select(u => new MessageReadUserDto
            {
                UserId = u.UserId,
                // RealName 为空时填充"未实名用户"，避免前端显示空白
                RealName = string.IsNullOrWhiteSpace(u.RealName) ? "未实名用户" : u.RealName
            }).ToList()
        };

        return ApiResponseDto<MessageReadStatsDto>.Success(dto);
    }

    // ========================================================
    // 内部调用
    // ========================================================

    public async Task<ApiResponseDto<bool>> NotifyAsync(InternalMessageNotifyDto dto)
    {
        // 内部服务调用时，InternalServiceAuthMiddleware 设置了 super_admin + tenant_id=1
        // 后台服务无 HTTP 上下文，需通过 dto.TenantId 显式指定目标租户
        var tenantId = dto.TenantId ?? _currentUser.TenantId ?? 1;
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var tenantCode = tenant?.Code ?? string.Empty;
        var targetType = (MessageTargetType)dto.TargetType;

        var targetDesc = await BuildTargetDescAsync(targetType, dto.TargetIds, new List<long> { tenantId });

        var message = new Message
        {
            Title = dto.Title,
            Content = dto.Content,
            Category = (MessageCategory)dto.Category,
            SourceType = MessageSourceType.Auto,
            SourceSubsystemCode = dto.SourceSubsystemCode,
            TargetType = targetType,
            TargetIds = string.Join(",", dto.TargetIds),
            TargetDesc = targetDesc,
            SenderId = null,
            SenderName = "系统",
            TargetUrl = dto.TargetUrl ?? string.Empty,
            IsCrossTenant = false,
            IsRecalled = false,
            TenantId = tenantId,
            TenantCode = tenantCode,
            BizType = dto.BizType,
            BizKey = dto.BizKey,
            CreatedTime = DateTime.Now
        };

        await _messageRepository.AddAsync(message);

        var recipients = await ResolveRecipientsAsync(message, dto.TargetIds);
        if (recipients.Count > 0)
        {
            await _recipientRepository.BatchInsertAsync(recipients);

            foreach (var recipient in recipients)
            {
                var payload = new MessageInboxDto
                {
                    Id = recipient.Id,
                    MessageId = message.Id,
                    Title = message.Title,
                    Content = message.Content,
                    Category = (int)message.Category,
                    CategoryName = GetCategoryName(message.Category),
                    SenderName = message.SenderName,
                    TargetUrl = message.TargetUrl,
                    IsRead = false,
                    ReadTime = null,
                    CreatedTime = message.CreatedTime
                };
                await _messagePusher.PushToUserAsync(recipient.UserId, payload);
            }
        }

        return ApiResponseDto<bool>.Success(true);
    }

    /// <summary>
    /// 批量检查指定业务类型下哪些业务键已存在未撤回的消息记录
    /// </summary>
    public async Task<ApiResponseDto<BizKeyCheckResultDto>> CheckBizExistsAsync(BizKeyCheckDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BizType) || dto.BizKeys.Count == 0)
        {
            return ApiResponseDto<BizKeyCheckResultDto>.Success(new BizKeyCheckResultDto());
        }

        var existingKeys = await _context.Set<Message>()
            .Where(m => m.BizType == dto.BizType
                        && !m.IsRecalled
                        && dto.BizKeys.Contains(m.BizKey ?? string.Empty))
            .Select(m => m.BizKey ?? string.Empty)
            .Where(k => k != string.Empty)
            .Distinct()
            .ToListAsync();

        return ApiResponseDto<BizKeyCheckResultDto>.Success(new BizKeyCheckResultDto
        {
            ExistingBizKeys = existingKeys
        });
    }

    // ========================================================
    // 接收者解析（对应设计文档 7.5 节）
    // ========================================================

    /// <summary>
    /// 在 Message.TenantId 范围内按 TargetType 解析接收者
    /// </summary>
    private async Task<List<MessageRecipient>> ResolveRecipientsAsync(Message message, List<long> targetIds)
    {
        var tenantId = message.TenantId;
        List<User> users;

        switch (message.TargetType)
        {
            case MessageTargetType.User:
                users = await _context.Users
                    .Where(u => targetIds.Contains(u.Id)
                                && u.TenantId == tenantId
                                && !u.IsDeleted
                                && u.Status == 1)
                    .ToListAsync();
                break;

            case MessageTargetType.Role:
                // 识别目标角色中归属平台租户的角色（如 super_admin、tenant_admin）。
                // 这类角色归属平台租户（Role.TenantId=1），但绑定该角色的用户散落在
                // 各个普通租户下（User.TenantId=普通租户ID）。
                // 仅在 Message 归属平台租户时跨租户查询所有平台角色用户（由 SendAsync 保证
                // 平台角色只在平台租户的 Message 下解析），避免在多个目标租户的 Message 下
                // 为同一用户重复创建接收记录。普通租户角色始终按 Message.TenantId 过滤。
                var platformRoleIds = await _context.Roles
                    .Where(r => targetIds.Contains(r.Id) && r.TenantId == PlatformTenantId)
                    .Select(r => r.Id)
                    .ToListAsync();
                var normalRoleIds = targetIds.Except(platformRoleIds).ToList();

                var platformRoleUsers = platformRoleIds.Count > 0 && tenantId == PlatformTenantId
                    ? await (from u in _context.Users
                             join ur in _context.UserRoles on u.Id equals ur.UserId
                             where platformRoleIds.Contains(ur.RoleId)
                                   && !u.IsDeleted
                                   && u.Status == 1
                             select u).Distinct().ToListAsync()
                    : new List<User>();

                var normalRoleUsers = normalRoleIds.Count > 0
                    ? await (from u in _context.Users
                             join ur in _context.UserRoles on u.Id equals ur.UserId
                             where normalRoleIds.Contains(ur.RoleId)
                                   && u.TenantId == tenantId
                                   && !u.IsDeleted
                                   && u.Status == 1
                             select u).Distinct().ToListAsync()
                    : new List<User>();

                // 合并并按 UserId 去重（同一用户可能同时拥有平台角色与普通角色）
                users = platformRoleUsers
                    .Concat(normalRoleUsers)
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .ToList();
                break;

            case MessageTargetType.Organization:
                // 按组织发送：包含选中组织及其所有下级组织的有效用户
                var expandedOrgIds = await ExpandWithDescendantOrgIdsAsync(targetIds);
                users = await _context.Users
                    .Where(u => u.OrganizationId.HasValue
                                && expandedOrgIds.Contains(u.OrganizationId.Value)
                                && u.TenantId == tenantId
                                && !u.IsDeleted
                                && u.Status == 1)
                    .ToListAsync();
                break;

            case MessageTargetType.All:
                users = await _context.Users
                    .Where(u => u.TenantId == tenantId
                                && !u.IsDeleted
                                && u.Status == 1)
                    .ToListAsync();
                break;

            default:
                return new List<MessageRecipient>();
        }

        return users.Select(u => new MessageRecipient
        {
            MessageId = message.Id,
            UserId = u.Id,
            UserName = u.UserName,
            IsRead = false,
            IsDeleted = false,
            // 使用接收用户所在租户ID/编码，便于用户侧按自己租户查询收件箱。
            // 跨租户消息（如发给 tenant_admin 角色）时，接收用户所在租户可能与 Message.TenantId 不同。
            TenantId = u.TenantId,
            TenantCode = u.TenantCode,
            CreatedTime = DateTime.Now
        }).ToList();
    }

    /// <summary>
    /// 构建目标描述（用于管理端展示）
    /// </summary>
    private async Task<string> BuildTargetDescAsync(MessageTargetType targetType, List<long> targetIds, List<long> tenantIds)
    {
        switch (targetType)
        {
            case MessageTargetType.User:
                var users = await _context.Users
                    .Where(u => targetIds.Contains(u.Id))
                    .Select(u => u.RealName)
                    .ToListAsync();
                return string.Join(",", users);

            case MessageTargetType.Role:
                var roles = await _context.Roles
                    .Where(r => targetIds.Contains(r.Id))
                    .Select(r => r.Name)
                    .ToListAsync();
                return string.Join(",", roles);

            case MessageTargetType.Organization:
                var orgs = await _context.Organizations
                    .Where(o => targetIds.Contains(o.Id))
                    .Select(o => o.Name)
                    .ToListAsync();
                return string.Join(",", orgs);

            case MessageTargetType.All:
                var tenants = await _context.Tenants
                    .Where(t => tenantIds.Contains(t.Id))
                    .Select(t => t.Name)
                    .ToListAsync();
                return tenantIds.Count > 1 ? $"全员({string.Join(",", tenants)})" : "全员";

            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 获取子系统编码->名称映射
    /// </summary>
    private async Task<Dictionary<string, string>> GetSubsystemNameMapAsync()
    {
        return await _context.Subsystems.ToDictionaryAsync(s => s.Code, s => s.Name);
    }

    /// <summary>
    /// 递归展开选中组织及其所有下级组织 ID（用于按组织发送消息时包含下级组织用户）
    /// </summary>
    private async Task<List<long>> ExpandWithDescendantOrgIdsAsync(List<long> targetIds)
    {
        var result = new List<long>(targetIds);
        foreach (var id in targetIds)
        {
            result.AddRange(await GetAllDescendantOrgIdsAsync(id));
        }
        return result.Distinct().ToList();
    }

    /// <summary>
    /// 递归获取指定组织的所有下级组织 ID
    /// </summary>
    private async Task<List<long>> GetAllDescendantOrgIdsAsync(long organizationId)
    {
        var result = new List<long>();
        var children = await _organizationRepository.GetChildrenAsync(organizationId);
        foreach (var child in children)
        {
            result.Add(child.Id);
            result.AddRange(await GetAllDescendantOrgIdsAsync(child.Id));
        }
        return result;
    }

    private static string GetCategoryName(MessageCategory category) => category switch
    {
        MessageCategory.System => "系统消息",
        MessageCategory.Business => "业务通知",
        MessageCategory.Announcement => "公告信息",
        _ => string.Empty
    };

    private static string GetTargetTypeName(MessageTargetType type) => type switch
    {
        MessageTargetType.User => "指定用户",
        MessageTargetType.Role => "按角色",
        MessageTargetType.Organization => "按组织",
        MessageTargetType.Tenant => "按租户",
        MessageTargetType.All => "全员",
        _ => string.Empty
    };

    private static string GetSourceTypeName(MessageSourceType type) => type switch
    {
        MessageSourceType.Auto => "自动触发",
        MessageSourceType.Manual => "手动推送",
        _ => string.Empty
    };
}
