# MinIO 部署说明

本文档是操作手册。按第二章把 MinIO 跑起来，按第四章接进项目。**开发环境与生产环境使用同一套部署流程**，差异集中在第五章列出的取值上。

设计背景与代码改造内容见 [图片对象存储改造方案](../图片对象存储改造方案.md)。

---

## 一、它在系统里做什么

系统的图片（客户服务对比照片等）不再以 base64 存进 PostgreSQL，改为存进 MinIO。数据库只保存对象标识（objectKey），浏览器凭后端签发的预签名 URL **直接从 MinIO 加载图片**，不经过网关和业务服务。

因此 MinIO 有两个端口，用途完全不同：

| 端口 | 名称 | 谁在访问 | 说明 |
|---|---|---|---|
| **9000** | S3 API | Store 服务 + **浏览器** | 程序上传/删除走这里；浏览器加载图片也走这里 |
| **9001** | Web 控制台 | 人 | 仅供人工查看管理，程序完全不用。不开放也不影响功能 |

**部署形态**：MinIO 以 **Windows 服务**方式常驻，开机自启——与本机 PostgreSQL 同一个模型，装好之后不需要在任何启动脚本里管它。`minio.exe` 本身是普通控制台程序、不支持服务协议，故用 [nssm](https://nssm.cc/download) 包装。

> **为什么不用 Docker**：本项目本机环境未使用 Docker，PostgreSQL 也是原生 Windows 服务。`deploy/` 下的 `docker-compose.infrastructure.yml` 与 `start-infrastructure.cmd` 是为 Docker 环境准备的，本机不使用。

---

## 二、部署步骤

### 步骤 1：准备三个可执行文件

均为官方单文件发布，**无需安装**，放在**仓库目录之外**：

```
D:\软件\MinIO\
  ├── minio.exe   (约 108 MB)   服务端
  ├── mc.exe      (约 30 MB)    官方命令行客户端，用于验证
  └── nssm.exe    (约 330 KB)   服务管理器，取 zip 包内 win64 版
```

官方来源：

- `https://dl.min.io/server/minio/release/windows-amd64/minio.exe`
- `https://dl.min.io/client/mc/release/windows-amd64/mc.exe`
- `https://nssm.cc/download` —— 下载 zip 解压，取 `win64\nssm.exe`

> 刻意放在仓库外：140MB+ 二进制不应进入 Git 历史（一旦提交极难移除），也不该被 `git status` 反复列为未跟踪文件。

### 步骤 2：注册服务

数据目录、端口、凭据全部由服务配置持有，脚本已封装好。**右键以管理员身份运行**：

```
deploy\注册MinIO服务.bat
```

换机器或换环境时，改脚本开头四个变量即可（生产环境取值见第五章）：

```bat
set "MINIO_HOME=D:\软件\MinIO"
set "MINIO_DATA=D:\minio-data"
set "MINIO_USER=mes"
set "MINIO_PASSWORD=mes@2026"
```

脚本执行的动作与每一项的用意：

| 配置项 | 值 | 为什么这么配 |
|---|---|---|
| `AppParameters` | `server "数据目录" --address :9000 --console-address :9001` | **数据目录就在这里设置**，它是 minio.exe 的命令行参数 |
| `AppEnvironmentExtra` | `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | 凭据经环境变量注入。**不能写成命令行参数**——那会出现在进程列表里，本机任何用户可见 |
| `AppStdout` / `AppStderr` | `logs\minio.log` / `minio-err.log`，10MB 轮转 | 服务没有控制台窗口，不落盘则出问题无从查起 |
| `AppExit Default` | `Restart` | 进程崩溃自动重启 |
| `Start` | `SERVICE_AUTO_START` | 开机自启，无需登录 |

> **凭据用 nssm 注入，而不是 `setx`。** 这是刻意的：服务默认以 `SYSTEM` 账户运行，而 `setx` 写的是**当前用户**的环境变量，`SYSTEM` 读不到——此时 MinIO 会**静默退回默认凭据 `minioadmin`，不报任何错**。用 nssm 注入则凭据与服务绑定，不存在作用域问题，也不污染全局环境。步骤 3.2 的验证专门用来拦这个问题。

### 步骤 3：验证

四项全过才算部署成功。

**3.1 服务状态**

```cmd
sc query MinIO
```

预期 `STATE : 4  RUNNING`。

**3.2 凭据真的生效了（关键，不要跳过）**

先确认新凭据可用：

```cmd
cd /d D:\软件\MinIO
mc.exe alias set local http://localhost:9000 mes mes@2026
mc.exe admin info local
```

预期：

```
Added `local` successfully.
●  localhost:9000
   Network: 1/1 OK
   Drives: 1/1 OK
```

再确认**默认凭据已失效**：

```cmd
mc.exe alias set tmp http://localhost:9000 minioadmin minioadmin
```

预期**报错**：`The Access Key Id you provided does not exist in our records`。

> 这一步必须做。若默认凭据仍然可用，说明 MinIO 没读到你配的凭据、正跑在默认值上——服务状态照样 RUNNING，业务功能照样正常，唯一症状就是这里能登进去。缺了这步验证，问题会一路潜伏到生产环境。

**3.3 端口监听**

```cmd
netstat -ano | findstr "LISTENING" | findstr ":9000 :9001"
```

预期 9000 与 9001 均 `LISTENING`，且 PID 相同。

> 若报 `find: '/I': No such file or directory` 之类的 Unix 风格错误，是 PATH 中 Git 自带工具遮蔽了系统工具，改用全路径 `%SystemRoot%\System32\findstr.exe`。

**3.4 日志落盘**

```cmd
type D:\软件\MinIO\logs\minio-err.log
```

预期看到启动横幅，含 `API: http://...:9000` 与 `WebUI: http://...:9001`。

> **注意 MinIO 把启动信息输出到 stderr**，所以在 `minio-err.log` 而不是 `minio.log` 里，排查时别看错文件。

Web 控制台 `http://localhost:9001` 也可以登（同一套凭据），但**验证以 `mc` 为准**：社区版控制台的对象浏览能力随版本变化，`mc` 是官方命令行工具、行为稳定。

> **桶不需要手工创建。** Store 服务启动时会自动检查并创建 `bms-store` 桶，默认为 private（不开放匿名读）。手工建桶反而可能建出权限不一致的桶。

---

## 三、日常运维

### 不需要启动它

MinIO 开机自启，`runLocal.bat` 里**不含** MinIO 步骤，日常无需关心。同理也没有启动顺序要求——微服务什么时候启都行。

### 常用命令

```cmd
nssm restart MinIO        rem 改完配置后重启生效
nssm stop MinIO           rem 停止（关窗口那套不再适用）
nssm edit MinIO           rem 图形界面查看/修改全部配置
nssm get MinIO AppParameters    rem 查看单项配置
```

### 修改数据目录

```cmd
nssm set MinIO AppParameters "server \"D:\minio-data\" --address :9000 --console-address :9001"
nssm restart MinIO
```

> **改参数只是换存储位置，旧目录里的对象不会自动迁移。** 真要迁移：先 `nssm stop MinIO` → 整个目录拷到新位置 → 改参数 → 启动。目录内是 MinIO 自有布局，**只能整体搬，不能挑文件**，也不要手工增删其中内容，否则对象损坏。

### 重新注册

`nssm install` 在服务已存在时会失败。需要重来时先跑 `deploy\卸载MinIO服务.bat`（以管理员身份），它只移除服务、不动数据目录。

---

## 四、接入项目

> 本章对应改造方案的阶段 1，**代码尚未实现**。类库 `Bms.BuildingBlocks.Storage` 落地后按此配置。

在 `src/Services/Store/Bms.Store.Api/appsettings.json` 中新增顶层节：

```json
"ObjectStorage": {
  "Endpoint": "http://localhost:9000",
  "PublicEndpoint": "http://localhost:9000",
  "AccessKey": "mes",
  "SecretKey": "mes@2026",
  "BucketName": "bms-store",
  "PresignExpireMinutes": 30,
  "MaxFileSizeBytes": 10485760,
  "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".webp" ],
  "AllowedBizTypes": [ "customer-photo", "product-image", "technician-avatar", "purchase-voucher" ]
}
```

| 配置项 | 含义 | 要点 |
|---|---|---|
| `Endpoint` | **Store 服务**访问 MinIO 的地址 | 服务进程可达即可 |
| `PublicEndpoint` | 写进预签名 URL、交给**浏览器**的地址 | 必须浏览器可达，见下方警告 |
| `AccessKey` / `SecretKey` | 访问凭据 | 必须与服务里 `AppEnvironmentExtra` 配的一致，改一处就要改两处 |
| `BucketName` | 桶名 | 服务启动时自动创建 |
| `PresignExpireMinutes` | 预签名 URL 有效期（分钟） | 30。过短会导致页面停留后图片失效 |
| `MaxFileSizeBytes` | 单文件大小上限 | 10485760 = 10MB |
| `AllowedBizTypes` | 允许的业务类型前缀 | 业务概念放配置，类库不认识它；其他服务接入时写自己的值 |

### ⚠️ Endpoint 与 PublicEndpoint 的区别

这是此类改造最常见的故障点。本机开发时（服务与浏览器同一台机器）两者相同，容易误以为其中一个是冗余配置。**一旦服务部署到服务器而浏览器在别的机器上，`PublicEndpoint` 必须改成服务器实际域名或 IP**，否则浏览器会去访问「自己的」localhost:9000，图片全部加载失败。

### 联调验证

阶段 3 完成后，在客户服务档案上传一张对比照片，然后：

```cmd
mc.exe ls local/bms-store --recursive
```

能看到形如 `1/1/customer-photo/2026/08/xxxxx.jpg` 的对象，说明写入链路通了；刷新页面能正常显示图片，说明读取（预签名）链路也通了。

---

## 五、生产环境

**部署流程与第二章完全相同**——同样用 `注册MinIO服务.bat`，同样由 nssm 注入凭据，同样开机自启。差异只在下表五项取值与两项网络配置。

### 5.1 需要调整的取值

| 项 | 开发环境 | 生产环境 | 原因 |
|---|---|---|---|
| `MINIO_DATA` | `D:\minio-data`（非系统盘） | **独立数据盘**，如 `E:\minio-data` | 不能放系统盘。系统盘写满会同时拖垮操作系统与 PostgreSQL，故障范围远大于图片不可用；且 MinIO 在磁盘剩余空间低于阈值时会直接拒绝写入（报 `minimum free drive threshold`） |
| `MINIO_PASSWORD` | `mes@2026`（随项目惯例） | **独立强密码** | 见 5.2 |
| `PublicEndpoint` | `http://localhost:9000` | 服务器域名或 IP | 见 5.3 |
| 数据备份 | 可不做（数据可重建） | **必须做** | 见 5.5 |
| 凭据存放 | 明文在 `appsettings.json` | 环境变量注入 | 见 5.4 |

容量估算：按单张原图 2MB 计，1 万张约 20GB。

### 5.2 凭据必须是强密码

MinIO root 凭据的权限是**全量管理**。任何能访问到 9000 的人，用 `mc alias set` 就能下载全部租户、全部门店的客户照片（面部服务对比照片属敏感个人信息），也能删除或覆盖所有对象——而这些图片没有第二份。

而按本方案设计，**9000 必须对所有浏览器客户端开放**（图片由浏览器直连加载，不经过网关），无法只放通给应用服务器。因此这里不存在「内网所以无所谓」的余地。

MinIO 要求用户名 ≥3 字符、密码 ≥8 字符，否则启动直接失败。**绝不能保留默认的 `minioadmin`** —— 它是自动化扫描器的第一发子弹。改完务必执行步骤 3.2 的反向验证。

### 5.3 网络可达性

| 事项 | 要求 |
|---|---|
| `PublicEndpoint` | 改为浏览器可达的域名或 IP，如 `http://192.168.1.10:9000`。**生产环境最易漏的一项** |
| 防火墙 9000 入站 | **必须放通**给浏览器所在网段 |
| 防火墙 9001 入站 | 建议**不放通**，或仅限运维网段。控制台是管理入口，业务功能完全不依赖它 |
| HTTPS 站点 | 若站点以 `https` 访问，图片走 `http://...:9000` 会被浏览器按混合内容拦截、全部不显示。必须给 9000 配 TLS 证书，或经反向代理转发并把 `PublicEndpoint` 改为 `https://域名/前缀` |

### 5.4 凭据不写进 appsettings

开发环境凭据明文写在 `appsettings.json` 是随项目现有惯例（数据库密码亦如此）。生产环境改为环境变量注入，ASP.NET Core 用双下划线表示配置层级：

```cmd
setx /M ObjectStorage__AccessKey 生产用户名
setx /M ObjectStorage__SecretKey 生产强密码
```

这样 `appsettings.json` 中不出现生产凭据，且无需改动任何代码。

> 此处用 `setx /M`（机器级）而非 `setx`：读取方是 Store 服务进程，若它也以服务方式运行、账户为 `SYSTEM`，用户级变量同样读不到。

### 5.5 备份必须落实

单机单目录部署**没有任何冗余**，数据盘损坏即丢失全部图片，数据库里只剩指向不存在对象的 key。

| 事项 | 要求 |
|---|---|
| 备份 | **必须**将数据目录纳入服务器现有备份策略。这是本部署方式下最需要落实的一条 |
| 目录内容 | 不要手工增删其中文件 |
| 外链图片 | 「输入图片URL」录入的外部直链不在 MinIO 中，**无法纳入备份** |
| 提升可用性 | 要求提高时改多节点纠删码模式，需重新规划部署 |

---

## 六、端口占用核对

| 端口 | 占用方 |
|---|---|
| 5000 – 5031 | Gateway 与各微服务 |
| 5432 | PostgreSQL（原生 Windows 服务） |
| **9000** | **MinIO S3 API（新增）** |
| **9001** | **MinIO 控制台（新增）** |

均不冲突。开发机本机访问无需配置防火墙。

---

## 七、故障排查

| 现象 | 原因与处理 |
|---|---|
| 服务 RUNNING 但默认凭据 `minioadmin` 仍能登入 | MinIO 没读到配置的凭据。检查 `nssm get MinIO AppEnvironmentExtra`，改完 `nssm restart MinIO`。**这是最危险的一种情况：功能全正常，只有安全性没了** |
| `mc alias set` 报 `Access Denied` | `mc` 命令里的凭据与服务实际生效的不一致。改凭据后必须重设别名 |
| 服务启动即停止 | 用户名 <3 字符或密码 <8 字符；或数据目录路径不存在/无权限。看 `logs\minio-err.log` |
| `sc query MinIO` 报服务不存在 | 未注册或已被卸载。以管理员身份跑 `deploy\注册MinIO服务.bat` |
| `nssm install` 报服务已存在 | 先跑 `deploy\卸载MinIO服务.bat`（管理员），再重新注册 |
| 日志文件是空的 | 启动信息在 `minio-err.log`（stderr），不在 `minio.log` |
| 程序启动报「桶不存在」 | `AccessKey`/`SecretKey` 与服务配的不一致导致建桶失败。用 `mc ls local` 确认桶是否真的存在 |
| 图片加载全部失败，URL 里是 localhost | `PublicEndpoint` 仍是 `localhost` 而浏览器在另一台机器。见 5.3 |
| 图片被浏览器拦截，控制台报混合内容 | 站点是 https 而图片是 http。见 5.3 |
| 图片加载 403 且 URL 带签名参数 | 预签名 URL 已过期（默认 30 分钟）。刷新页面重新签发；仍 403 则检查 `PresignExpireMinutes` 与服务器系统时间偏移 |
| 重启机器后图片全挂 | 服务未自启。`sc query MinIO` 查状态，`nssm get MinIO Start` 应为 `SERVICE_AUTO_START` |
| `findstr` / `find` 报 Unix 风格错误 | PATH 中 Git 自带工具遮蔽了系统工具，改用 `%SystemRoot%\System32\` 全路径 |
