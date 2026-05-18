# MES管理台前端项目

## 技术栈
- Vue 3 + TypeScript
- Element Plus UI组件库
- Pinia 状态管理
- Vue Router 路由
- ECharts 图表
- Vite 构建工具

## 项目结构
```
src/
├── layout/              # 布局组件
├── views/               # 页面组件
│   ├── login/           # 登录页
│   ├── dashboard/       # 首页/仪表盘
│   ├── system/          # 系统管理模块
│   └── error/           # 错误页面
├── router/              # 路由配置
├── stores/              # Pinia状态管理
├── components/          # 公共组件
├── utils/               # 工具类
├── main.ts              # 入口文件
└── style.css            # 全局样式
```

## 开发运行
```bash
# 安装依赖
npm install

# 启动开发服务
npm run dev

# 生产构建
npm run build

# 预览构建产物
npm run preview
```

## 默认账号
- 用户名：admin
- 密码：123456

## 页面说明
- `/login` 登录页
- `/dashboard` 首页/仪表盘
- `/system/user` 用户管理
- `/system/role` 角色管理
- `/system/menu` 菜单管理
- `/system/organization` 组织架构

## UI风格
- 主色调：蓝色系 (#409EFF)
- 布局：左侧菜单 + 顶部导航 + 内容区域
- 设计风格：简约现代，突出业务功能
- 响应式：支持1366*768及以上分辨率
