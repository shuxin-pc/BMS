# MES系统开发规范

**版本**：v1.1
**日期**：2026-03-20
**适用范围**：所有MES系统前后端开发人员

## 目录
1. [全局通用规范](#1-全局通用规范)
2. [前端开发规范](#2-前端开发规范)
3. [后端开发规范](#3-后端开发规范)
4. [环境配置规范](#4-环境配置规范)
5. [数据库设计规范](#5-数据库设计规范)
6. [API接口规范](#6-api接口规范)
7. [测试规范](#7-测试规范)
8. [文档规范](#8-文档规范)

---

## 1. 全局通用规范

### 1.1 命名总原则
- 优先使用英文，禁止使用拼音命名（通用公认缩写除外）
- 尽量避免缩写，除非是广为人知的缩写（如 Id、Xml、Io、DTO、VO、BO）
- 命名应具备描述性，见名知意，避免过度简短
- 同一业务领域的命名保持统一，避免同一概念有多种表述

### 1.2 注释规范
- 注释解释"为什么"而不是"是什么"，代码本身应具备自解释性
- 所有对外暴露的接口、方法、类必须有完整的注释说明
- 复杂业务逻辑必须添加注释说明设计思路和注意事项
- 临时注释（TODO、FIXME、HACK）必须包含责任人或日期，便于跟踪

### 1.3 安全规范
- 所有用户输入必须进行校验，防止SQL注入、XSS攻击
- 敏感信息（密码、密钥、Token）禁止硬编码在代码中
- 接口必须进行权限校验，防止越权访问
- 日志中禁止输出敏感信息（密码、身份证号、手机号等）

### 1.4 API命名规范（前后端统一）
**核心原则**：前后端 API 传输的 JSON 数据必须统一使用 **camelCase**（小驼峰）命名。

| 层面 | 内部代码 | API传输 |
|------|---------|---------|
| 前端 TypeScript | camelCase | camelCase |
| 后端 C# DTO | PascalCase（内部使用） | camelCase（对外接口） |

**具体要求**：

1. **前端**：
   - TypeScript 接口属性全部使用 camelCase，如 `userName`、`createdAt`
   - 发送请求时 JSON 键必须为 camelCase

2. **后端**：
   - 对外暴露的 DTO（Request/Response）属性必须使用 camelCase
   - 使用 `[JsonPropertyName("camelCase")]` 特性显式指定序列化名称
   - 内部实体类仍使用 PascalCase

**示例**：
```csharp
// 后端 DTO - 对外接口使用 camelCase
public class TenantUpdateDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
```

```typescript
// 前端 - 统一使用 camelCase
interface TenantUpdate {
  id: number
  name: string
  contactName: string
  createdAt: string
}
```

**禁止事项**：
- ❌ 前端发送 `Name`、`ContactName`（PascalCase）
- ❌ 后端返回 `name`、`contactName` 但内部属性是 PascalCase 且未配置特性
- ❌ 前后端混用命名风格

**最佳实践**：
- 使用代码生成工具自动生成前后端类型，确保一致性
- 或在后端全局配置 `PropertyNameCaseInsensitive = true` 作为兜底方案

---

## 2. 前端开发规范

### 2.1 技术栈规范
| 技术领域 | 选型 | 版本要求 |
|---------|------|---------|
| 基础框架 | Vue 3 + TypeScript | 3.4.x+ |
| UI组件库 | Element Plus | 2.4.x+ |
| 状态管理 | Pinia | 2.1.x+ |
| 路由 | Vue Router | 4.2.x+ |
| HTTP客户端 | Axios | 1.6.x+ |
| 构建工具 | Vite | 5.x+ |
| 代码规范 | ESLint + Prettier | 最新 |
| 可视化 | ECharts | 5.4.x+ |

### 2.2 命名规范
**文件命名**：
- Vue组件文件：PascalCase（大驼峰），如 `UserList.vue`
- TypeScript/JavaScript文件：camelCase（小驼峰），如 `userApi.ts`
- 样式文件：kebab-case（短横线），如 `user-list.scss`
- 静态资源：kebab-case，如 `background-image.png`

**代码命名**：
- 组件名：PascalCase，如 `export default defineComponent({ name: 'UserList' })`
- 变量/函数：camelCase，如 `const userName = ref('')`
- 常量：UPPER_SNAKE_CASE（大写下划线），如 `const MAX_PAGE_SIZE = 100`
- 自定义事件名：kebab-case，如 `@update-user-info`

### 2.3 设计规范
- **前端设计制作必须使用 `frontend-design` skill**：在制作任何前端界面（组件、页面、应用）之前，必须先调用 frontend-design skill，确保遵循设计规范和最佳实践
- 设计方向需符合《前端深色科技风样式规范》的要求
- 参考现有样式文件 `src/Admin/mes-admin/src/styles/dark-theme.css` 中的变量和规范

### 2.4 Vue代码规范
- 单文件组件代码行数建议不超过500行，超过时考虑拆分
- 组件选项顺序：`name` → `components` → `props` → `emits` → `setup`
- Props必须定义类型、默认值和校验规则
- 优先使用Composition API，避免使用Options API
- 禁止直接修改Props，如需修改请通过emit事件通知父组件

### 2.5 样式规范
- 优先使用Element Plus主题变量，避免硬编码颜色值
- 全局样式统一放在`styles`目录下，页面级样式使用scoped
- 类名使用BEM规范：`block__element--modifier`
- 避免使用`!important`，除非确实需要覆盖第三方组件样式
- 响应式设计需兼容1920×1080、1366×768等主流分辨率

### 2.6 页面功能规范
**所有页面通用要求**：
- 日期时间字段统一格式化展示：
  - 日期：`YYYY-MM-DD`
  - 时间：`HH:mm:ss`
  - 完整时间：`YYYY-MM-DD HH:mm:ss`
- 下拉框初始化必须有默认提示，如"请选择"、"全部"
- 按钮点击后显示loading状态，防止重复提交
- 表单输入要有明确的校验提示，错误信息清晰易懂

**列表页面规范**：
- 仅展示重要业务字段，非关键字段在详情页展示
- 单元格内容过长自动截断，悬停时显示完整内容
- 非树形结构页面必须支持分页，默认每页20/30条(以行数据尽量铺满页面且不出现垂直滚动条为准)，可自定义
- 页面顶部必须包含常用字段的组合筛选功能
- 支持点击列头进行正序/倒序排序
- 操作按钮列固定在最右侧，避免横向滚动

**表单页面规范**：
- 必填项必须有`*`号标识
- 表单校验在失焦时触发，提交前做全量校验
- 表单提交成功/失败必须有明确的提示信息
- 复杂表单支持保存草稿功能
- 重置按钮恢复表单初始状态

---

## 3. 后端开发规范

### 3.1 技术栈规范
| 技术领域 | 选型 | 版本要求 |
|---------|------|---------|
| 开发框架 | .NET 8 + ASP.NET Core | 8.0.200+ |
| ORM框架 | Entity Framework Core 8 | 8.0.0+ |
| 数据库 | PostgreSQL | 16.x |
| 缓存 | Redis + StackExchange.Redis | 7.x |
| 消息队列 | RabbitMQ + MassTransit | 3.11.x |
| 认证授权 | OpenIddict | 5.9.x |
| 任务调度 | Quartz.NET | 3.7.x |
| 日志框架 | Serilog | 3.1.x |
| 单元测试 | xUnit + Moq + FluentAssertions | 最新 |

### 3.2 命名规范
**项目与命名空间**：
- 解决方案名：`Mes.sln`
- 项目名：`Mes.[模块].[层级]`，如 `Mes.System.Api`、`Mes.BuildingBlocks.Core`
- 命名空间与项目结构保持一致，禁止出现与项目结构不匹配的命名空间

**代码命名**：
- 类/接口/方法/属性/常量/枚举：PascalCase（大驼峰）
- 接口名以`I`开头，如 `IUserRepository`、`ITenantService`
- 抽象类以`Base`开头，如 `BaseEntity`、`BaseService`
- 局部变量/方法参数：camelCase（小驼峰）
- 私有字段：`_camelCase`（下划线开头小驼峰）
- 异步方法以`Async`结尾，如 `GetUserAsync`、`CreateOrderAsync`
- 特性类以`Attribute`结尾，如`AuthorizeAttribute`

### 3.3 代码格式规范
**缩进与换行**：
- 使用4个空格缩进，禁止使用制表符（Tab）
- 每行代码最多120个字符，超过时合理换行
- 换行时运算符（`.`、`+`、`&&`、`||`）放在行首，突出连续性

**大括号**：
- 采用K&R风格：左大括号不换行，右大括号独占一行
  ```csharp
  if (condition) {
      DoSomething();
  }
  ```
- 即使只有一行代码，也必须使用大括号，避免后续修改引入错误

**空格**：
- 关键字（`if`、`for`、`foreach`、`while`、`switch`）与左括号之间加一个空格
- 二元运算符（`=`、`+`、`-`、`*`、`/`、`&&`、`||`）两侧加空格
- 逗号、分号后加空格，如 `method(a, b, c);`
- 方法名与左括号之间不加空格
- 泛型尖括号内不加空格，如 `List<int>`、`Dictionary<string, object>`

**空行**：
- 文件末尾保留一个空行
- 方法之间空一行，属性之间可不用空行
- 类成员按以下顺序分组，每组之间空一行：
  1. 常量
  2. 静态字段
  3. 实例字段
  4. 构造函数
  5. 公共属性
  6. 公共方法
  7. 受保护属性/方法
  8. 私有属性/方法
  9. 嵌套类型

### 3.4 分层架构规范
采用DDD四层架构，各层职责清晰，单向依赖：
```
API层 → 应用层 → 领域层 → 基础设施层
```
- **API层**：Controller，负责请求接收、参数校验、响应返回
- **应用层**：Application Service，负责业务流程编排，不包含业务规则
- **领域层**：Domain Entity、Domain Service，包含核心业务规则和逻辑
- **基础设施层**：Repository、第三方服务集成，负责数据访问和外部交互

**依赖规则**：
- 上层依赖下层，下层不依赖上层
- 领域层是核心，不依赖任何其他层
- 应用层依赖领域层，不依赖基础设施层
- 基础设施层实现领域层定义的抽象接口

### 3.5 代码组织规范
**异常处理**：
- 抛出具体类型的异常（`ArgumentException`、`InvalidOperationException`），禁止直接抛出`Exception`
- 捕获异常时只捕获可处理的特定异常，禁止捕获`Exception`后忽略
- 使用`using`语句管理非托管资源，避免内存泄漏
- 在应用边界（Controller、定时任务）统一处理未捕获异常
- 异常日志必须包含堆栈信息和请求上下文，便于排查问题

**异步编程**：
- 所有IO操作必须使用`async/await`，禁止同步阻塞调用
- 异步方法名必须以`Async`结尾
- 避免使用`Task.Run`包装同步方法
- 长时间运行的任务使用后台服务，避免阻塞请求线程

**EF Core规范**：
- 禁止在LINQ查询中使用客户端计算，尽量在数据库端执行
- 批量操作优先使用EF Core扩展方法，避免循环操作
- 关联查询合理使用`Include`和`ThenInclude`，避免N+1查询问题
- 读写分离场景下，查询走从库，写入走主库
- 大数据量查询使用分页，禁止一次性加载全部数据

---

## 4. 环境配置规范

### 4.1 核心原则
开发期间一切功能必须同时满足开发环境和生产环境，确保代码在任何环境中都能正常运行，避免出现"开发环境正常但生产环境出问题"的情况。

### 4.2 功能开关（Feature Flags）规范
所有环境相关的功能开关必须通过配置控制，禁止在代码中硬编码环境判断逻辑。

**配置位置**：`appsettings.json` 的 `FeatureFlags` 节点
**实现示例**：
```csharp
// 读取配置
var useHttpsRedirection = builder.Configuration.GetValue<bool>("FeatureFlags:UseHttpsRedirection");

// 条件启用
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}
```

**必需的功能开关**：
- `UseHttpsRedirection`：是否强制HTTPS重定向
  - 开发环境：`false`（避免HTTP请求被重定向，方便调试）
  - 生产环境：`true`（强制HTTPS，保证传输安全）

### 4.3 多环境配置文件规范
使用 ASP.NET Core 标准的多环境配置体系：

| 配置文件 | 说明 |
|---------|------|
| `appsettings.json` | 基础配置（所有环境共用） |
| `appsettings.Development.json` | 开发环境特定配置（覆盖基础配置） |
| `appsettings.Production.json` | 生产环境特定配置（覆盖基础配置） |

**配置优先级**（从低到高）：
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. 环境变量
4. 命令行参数

### 4.4 CORS 配置规范
跨域配置必须支持环境差异化，禁止硬编码允许的来源。

**配置位置**：`appsettings.json` 的 `Cors:AllowedOrigins` 节点
**实现要求**：
- 开发环境：配置前端开发服务器地址（如 `http://localhost:5173`, `http://localhost:3000`）
- 生产环境：配置生产环境的前端域名
- 空数组时允许任意来源（仅用于临时调试，生产环境必须配置具体来源）

### 4.5 代码注释规范
所有环境相关的配置和逻辑必须添加详细的中文注释，说明：
1. 该配置在不同环境中的推荐值
2. 为什么需要这样配置
3. 配置错误可能导致的问题

**注释示例**：
```csharp
// ==========================================
// 配置功能开关（Feature Flags）
// ==========================================
// UseHttpsRedirection: 是否强制HTTPS重定向
//   - 开发环境: false（避免HTTP请求被重定向，方便调试）
//   - 生产环境: true（强制HTTPS，保证安全）
var useHttpsRedirection = builder.Configuration.GetValue<bool>("FeatureFlags:UseHttpsRedirection");
```

### 4.6 前端环境配置规范
前端必须根据环境自动选择 API 地址，禁止硬编码域名。

**实现示例**（Vite）：
```typescript
// 开发环境直接访问服务端口，生产环境通过网关访问
const API_BASE = import.meta.env.DEV ? 'http://localhost:5000/api/system' : '/api/system'
const IDENTITY_BASE = import.meta.env.DEV ? 'http://localhost:5010' : '/api/identity'
```

### 4.7 敏感信息配置规范
数据库密码、API密钥、签名证书等敏感信息：
- 禁止提交到代码仓库
- 开发环境：使用 `appsettings.Development.json`（已加入 `.gitignore`）
- 生产环境：使用环境变量或密钥管理服务
- 提供示例配置文件（如 `appsettings.Production.json.example`）

### 4.8 服务间通信配置规范
微服务间的通信地址必须支持配置：
- 开发环境：直接访问服务端口（如 `http://localhost:5000`）
- 生产环境：通过服务发现或网关访问
- 配置位置：`appsettings.json` 的 `SystemApi:BaseUrl` 等节点

### 4.9 中间件管道配置规范
中间件启用顺序必须考虑环境差异：
- Swagger：仅在开发环境启用
- HTTPS重定向：根据配置条件启用
- CORS：必须在 `UseAuthentication` 之前配置
- 所有中间件必须添加注释说明环境差异

### 4.10 环境变量规范
支持使用环境变量覆盖配置，格式遵循 ASP.NET Core 约定：
```bash
# Windows
set FeatureFlags__UseHttpsRedirection=true
set Cors__AllowedOrigins__0=https://mes.yourcompany.com

# Linux/Mac
export FeatureFlags__UseHttpsRedirection=true
export Cors__AllowedOrigins__0=https://mes.yourcompany.com
```

---

## 5. 数据库设计规范

### 5.1 表设计规范
**表名规范**：
- 表名前缀区分业务域：
  - `sys_`：系统管理相关表
  - `bus_`：业务功能相关表
  - `cfg_`：配置类相关表
  - `log_`：日志审计相关表
- 表名全部小写，下划线分隔，如 `sys_user`、`bus_production_order`
- 避免使用保留字作为表名或字段名

**必填字段**：所有表必须包含以下字段：
| 字段名 | 类型 | 说明 |
|--------|------|------|
| `id` | bigint | 主键，雪花ID，全局唯一有序 |
| `tenant_id` | uuid | 租户ID，多租户隔离字段 |
| `create_time` | timestamp | 创建时间，默认当前时间 |
| `create_by` | bigint | 创建人ID |
| `update_time` | timestamp | 更新时间，修改时自动更新 |
| `update_by` | bigint | 更新人ID |
| `is_deleted` | boolean | 软删除标记，默认false |

### 5.2 ID类型规范
- **业务表主键**：统一使用`bigint`类型，采用雪花算法生成（Snowflake ID）
  - 格式：64位长整型，包含时间戳（41位）、工作节点ID（10位）、序列号（12位）
  - 特点：全局唯一、有序递增、分布式生成、高性能（单节点每秒4096个ID）
  - 时间基准：2025-01-01 00:00:00，支持69年有效期
- **租户ID**：统一使用`uuid`类型，与多租户框架保持一致
- **外键关联**：与关联表主键类型保持一致
- **前端传输**：ID统一序列化为字符串，避免JavaScript大整数精度丢失

### 5.3 字段设计规范
- 字段名全部小写，下划线分隔，如 `user_name`、`phone_number`
- 布尔类型字段使用`is_`前缀，如 `is_enabled`、`is_deleted`
- 状态字段使用`tinyint`或`smallint`，避免使用字符串类型
- 金额字段使用`decimal(18,6)`，禁止使用浮点类型
- 枚举类型必须添加注释说明每个枚举值的含义
- 禁止使用`text`类型存储长文本，根据实际长度使用`varchar`或`jsonb`

### 5.4 索引规范
- 主键默认创建唯一索引
- 所有查询条件字段必须创建索引
- 多字段联合索引遵循最左前缀匹配原则
- 索引数量适度，单表索引不超过5个
- 大字段（如text、jsonb）不创建普通索引，可根据需要创建GIN索引
- 定期检查慢查询，优化不合理的索引

### 5.5 数据操作规范
- 禁止物理删除数据，统一使用软删除
- 重要数据修改必须记录操作日志
- 数据库连接字符串禁止硬编码，统一在配置中心管理
- 生产环境禁止直接执行DDL语句，必须通过迁移脚本执行
- 批量数据操作必须分批执行，避免锁表和长事务

### 5.6 关联数据删除规范
所有删除操作统一采用软删除（标记`is_deleted=true`），禁止物理删除。关联数据处理策略如下：

#### 强关联依赖（必须同步处理）
| 主表 | 关联表 | 处理策略 |
|------|--------|----------|
| 菜单（sys_menu） | 权限（sys_permission） | 级联软删除 |
| 角色（sys_role） | 角色权限关联（sys_role_permission） | 物理删除关联 |
| 角色（sys_role） | 用户角色关联（sys_user_role） | 物理删除关联 |
| 组织（sys_organization） | 用户（sys_user） | 存在用户时禁止删除，提示先转移用户 |

#### 弱关联依赖（关联数据可保留）
| 主表 | 关联表 | 处理策略 |
|------|--------|----------|
| 用户（sys_user） | 业务单据、历史记录 | 保留数据，显示"已删除"标记 |
| 用户（sys_user） | 审计日志（sys_audit_log） | 完整保留日志 |
| 角色（sys_role） | 历史操作记录 | 保留角色信息快照 |
| 权限（sys_permission） | 角色权限关联 | 自动清理无效关联 |

#### 配置类关联（同步失效）
| 主表 | 关联表 | 处理策略 |
|------|--------|----------|
| 角色（sys_role） | 数据权限配置（sys_data_permission） | 级联软删除 |
| 用户（sys_user） | 个性化配置 | 级联删除 |

#### 实现要求
- 所有删除操作必须先检查依赖关系，存在未处理的关联时禁止删除
- 删除操作必须记录详细审计日志，包含删除人、删除时间、删除原因、影响范围
- 数据库层面禁止设置级联删除，所有关联处理都由应用层控制
- 重要数据删除后提供7天回收站恢复机制

---

## 6. API接口规范

### 6.1 RESTful设计规范
- 接口路径使用名词复数形式，禁止使用动词，如 `/api/users`、`/api/orders`
- HTTP方法对应CRUD操作：
  - `GET`：查询资源
  - `POST`：创建资源
  - `PUT`：更新资源（全量）
  - `PATCH`：更新资源（部分）
  - `DELETE`：删除资源
- 路径参数用于标识资源ID，如 `/api/users/{id}`
- 查询参数用于过滤、分页、排序，如 `/api/users?pageIndex=1&pageSize=30&sort=create_time,desc`

### 6.2 统一返回格式
所有接口返回统一JSON格式：
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {},
  "requestId": "uuid",
  "timestamp": "2026-03-16T10:00:00Z"
}
```

**错误码规范**：
| 错误码 | 含义 | 说明 |
|--------|------|------|
| 200 | 成功 | 请求处理成功 |
| 400 | 参数错误 | 请求参数校验失败 |
| 401 | 未认证 | 未登录或Token无效 |
| 403 | 无权限 | 登录但无操作权限 |
| 404 | 资源不存在 | 请求的资源不存在 |
| 409 | 资源冲突 | 资源已存在或状态冲突 |
| 429 | 请求过于频繁 | 触发限流 |
| 500 | 服务器内部错误 | 服务端异常 |

### 6.3 分页接口规范
分页查询返回格式：
```json
{
  "code": 200,
  "message": "success",
  "data": {
    "list": [],
    "total": 100,
    "pageIndex": 1,
    "pageSize": 30,
    "totalPages": 4
  },
  "requestId": "uuid"
}
```

### 6.4 接口安全规范
- 所有接口（除登录、公共接口外）必须携带Token进行认证
- 接口权限控制到按钮级别，使用权限编码进行校验
- 敏感操作接口（删除、修改、批量操作）必须做防重和幂等处理
- 上传文件接口必须校验文件类型、大小，防止恶意文件上传
- 接口请求参数必须进行合法性校验，长度、格式、范围等

### 6.5 文档规范
- 所有接口必须有完整的Swagger注释
- 接口注释包含功能说明、参数说明、返回值说明、错误码说明
- 复杂请求和响应示例必须提供JSON示例
- 接口版本号放在路径中，如 `/api/v1/users`，避免破坏兼容性

---

## 7. 测试规范
### 7.1 测试文件
- 测试文件创建到特定文件夹，按用途采用中文命名并在文件名末尾标注"可删除"

---

## 8. 文档规范

### 8.1 开发文档风格规范
- **开发文档操作必须使用 superpowers 技能**：所有开发类文档（如开发计划、开发进度、项目结构等）在编写、编辑时，必须使用 superpowers 技能，确保文档质量和一致性

### 8.2 开发文档存储规范
- **开发文档必须存储到 docs 目录下的特定目录内**：所有开发类文档必须统一存放在项目根目录下的 `docs` 文件夹中，并根据文档类型分类存储
- **目录分类建议**：
  ```
  docs/
  ├── 开发计划/        # 开发计划、里程碑文档
  ├── 开发进度/        # 开发进度记录、每日日志
  ├── 技术文档/        # 架构设计、技术方案
  ├── 接口文档/        # API 接口定义
  ├── 测试文档/        # 测试用例、测试报告
  ├── 部署文档/        # 部署指南、运维手册
  └── 其他/            # 其他开发相关文档
  ```
- **文件命名规范**：文档文件名采用中文命名，清晰描述文档内容，便于快速识别

---
