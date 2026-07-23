import { createRouter, createWebHashHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getCurrentUser } from '@/api/system'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: {
      title: '登录'
    }
  },
  {
    path: '/',
    component: () => import('@/layout/index.vue'),
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: {
          title: '首页',
          icon: 'HomeFilled'
        }
      },
      {
        path: 'no-permission',
        name: 'NoPermission',
        component: () => import('@/views/no-permission/index.vue'),
        meta: {
          title: '无权限',
          icon: 'Lock'
        }
      },
      {
        path: 'system/users',
        name: 'SystemUsers',
        component: () => import('@/views/system/users/index.vue'),
        meta: {
          title: '用户管理',
          icon: 'User'
        }
      },
      {
        path: 'system/roles',
        name: 'SystemRoles',
        component: () => import('@/views/system/roles/index.vue'),
        meta: {
          title: '角色管理',
          icon: 'UserFilled'
        }
      },
      {
        path: 'system/menus',
        name: 'SystemMenus',
        component: () => import('@/views/system/menus/index.vue'),
        meta: {
          title: '菜单管理',
          icon: 'Menu'
        }
      },
      {
        path: 'system/subsystems',
        name: 'SystemSubsystems',
        component: () => import('@/views/system/subsystems/index.vue'),
        meta: {
          title: '子系统管理',
          icon: 'Grid'
        }
      },
      {
        path: 'system/organizations',
        name: 'SystemOrganizations',
        component: () => import('@/views/system/organizations/index.vue'),
        meta: {
          title: '组织架构管理',
          icon: 'OfficeBuilding'
        }
      },
      {
        path: 'system/tenants',
        name: 'SystemTenants',
        component: () => import('@/views/system/tenants/index.vue'),
        meta: {
          title: '租户管理',
          icon: 'School'
        }
      },
      {
        path: 'system/audit-logs',
        name: 'SystemAuditLogs',
        component: () => import('@/views/system/audit-logs/index.vue'),
        meta: {
          title: '审计日志',
          icon: 'Document'
        }
      },
      {
        path: 'system/system-configs',
        name: 'SystemConfigs',
        component: () => import('@/views/system/system-configs/index.vue'),
        meta: {
          title: '系统配置',
          icon: 'Setting'
        }
      },
      {
        path: 'system/profile',
        name: 'SystemProfile',
        component: () => import('@/views/system/profile/index.vue'),
        meta: {
          title: '个人中心',
          icon: 'User'
        }
      },
      {
        path: 'system/messages/sent',
        name: 'SystemMessagesSent',
        component: () => import('@/views/system/messages/sent/index.vue'),
        meta: {
          title: '消息发送记录',
          icon: 'Promotion'
        }
      },
      {
        path: 'store/home',
        name: 'StoreHome',
        component: () => import('@/views/store/home/index.vue'),
        meta: {
          title: '首页',
          icon: 'HomeFilled'
        }
      },
      {
        path: 'store/store/profile',
        name: 'StoreProfile',
        component: () => import('@/views/store/store/profile/index.vue'),
        meta: {
          title: '门店档案',
          icon: 'Management'
        }
      },
      {
        path: 'store/product/profile',
        name: 'StoreProductProfile',
        component: () => import('@/views/store/product/profile/index.vue'),
        meta: {
          title: '商品档案',
          icon: 'Goods'
        }
      },
      {
        path: 'store/pos/quick',
        name: 'StorePosQuick',
        component: () => import('@/views/store/pos/quick/index.vue'),
        meta: {
          title: '快速开单',
          icon: 'Cash'
        }
      },
      {
        path: 'store/marketing/poster',
        name: 'StoreMarketingPoster',
        component: () => import('@/views/store/marketing/poster/index.vue'),
        meta: {
          title: '宣传图片生成',
          icon: 'PictureFilled'
        }
      },
      {
        path: 'store/customer/profile',
        name: 'StoreCustomerProfile',
        component: () => import('@/views/store/customer/profile/index.vue'),
        meta: {
          title: '客户档案',
          icon: 'UserFilled'
        }
      },
      // ========== 商品管理模块 ==========
      {
        path: 'store/product/category',
        name: 'StoreProductCategory',
        component: () => import('@/views/store/product/category/index.vue'),
        meta: { title: '商品分类', icon: 'Files' }
      },
      {
        path: 'store/product/price',
        name: 'StoreProductPrice',
        component: () => import('@/views/store/product/price/index.vue'),
        meta: { title: '价格管理', icon: 'Money' }
      },
      {
        path: 'store/product/inventory',
        name: 'StoreProductInventory',
        component: () => import('@/views/store/product/inventory/index.vue'),
        meta: { title: '库存管理', icon: 'Box' }
      },
      {
        path: 'store/product/inventory/alert',
        name: 'StoreProductInventoryAlert',
        component: () => import('@/views/store/product/inventory/alert/index.vue'),
        meta: { title: '库存预警', icon: 'Warning' }
      },
      {
        path: 'store/product/expiry',
        name: 'StoreProductExpiry',
        component: () => import('@/views/store/product/expiry/index.vue'),
        meta: { title: '效期查询', icon: 'Calendar' }
      },
      {
        path: 'store/product/expiry/alert',
        name: 'StoreProductExpiryAlert',
        component: () => import('@/views/store/product/expiry/alert/index.vue'),
        meta: { title: '效期预警', icon: 'AlarmClock' }
      },
      {
        path: 'store/product/expiry-sales',
        name: 'StoreProductExpirySales',
        component: () => import('@/views/store/product/expiry-sales/index.vue'),
        meta: { title: '效期销售统计', icon: 'TrendCharts' }
      },
      {
        path: 'store/product/supplier',
        name: 'StoreProductSupplier',
        component: () => import('@/views/store/product/supplier/index.vue'),
        meta: { title: '供应商管理', icon: 'Van' }
      },
      // ========== 收银管理模块 ==========
      {
        path: 'store/pos/order',
        name: 'StorePosOrder',
        component: () => import('@/views/store/pos/order/index.vue'),
        meta: { title: '订单管理', icon: 'List' }
      },
      // ========== 客户管理模块 ==========
      {
        path: 'store/customer/level',
        name: 'StoreCustomerLevel',
        component: () => import('@/views/store/customer/level/index.vue'),
        meta: { title: '客户等级', icon: 'Medal' }
      },
      {
        path: 'store/customer/points',
        name: 'StoreCustomerPoints',
        component: () => import('@/views/store/customer/points/index.vue'),
        meta: { title: '积分管理', icon: 'Star' }
      },
      {
        path: 'store/customer/consume',
        name: 'StoreCustomerConsume',
        component: () => import('@/views/store/customer/consume/index.vue'),
        meta: { title: '消费记录', icon: 'Tickets' }
      },
      {
        path: 'store/customer/care',
        name: 'StoreCustomerCare',
        component: () => import('@/views/store/customer/care/index.vue'),
        meta: { title: '客户关怀', icon: 'Bell' }
      },
      // ========== 会员储值模块 ==========
      {
        path: 'store/storedvalue/account',
        name: 'StoreStoredValueAccount',
        component: () => import('@/views/store/storedvalue/account/index.vue'),
        meta: { title: '储值账户', icon: 'CreditCard' }
      },
      {
        path: 'store/storedvalue/rule',
        name: 'StoreStoredValueRule',
        component: () => import('@/views/store/storedvalue/rule/index.vue'),
        meta: { title: '储值规则', icon: 'GoldMedal' }
      },
      {
        path: 'store/storedvalue/log',
        name: 'StoreStoredValueLog',
        component: () => import('@/views/store/storedvalue/log/index.vue'),
        meta: { title: '储值流水', icon: 'Wallet' }
      },
      // ========== 预约管理模块 ==========
      {
        path: 'store/appointment/calendar',
        name: 'StoreAppointmentCalendar',
        component: () => import('@/views/store/appointment/calendar/index.vue'),
        meta: { title: '预约日历', icon: 'Calendar' }
      },
      {
        path: 'store/appointment/list',
        name: 'StoreAppointmentList',
        component: () => import('@/views/store/appointment/list/index.vue'),
        meta: { title: '预约列表', icon: 'Notebook' }
      },
      {
        path: 'store/appointment/tomorrow',
        name: 'StoreAppointmentTomorrow',
        component: () => import('@/views/store/appointment/tomorrow/index.vue'),
        meta: { title: '明日提醒', icon: 'BellFilled' }
      },
      // ========== 疗程卡管理模块 ==========
      {
        path: 'store/treatment/config',
        name: 'StoreTreatmentConfig',
        component: () => import('@/views/store/treatment/config/index.vue'),
        meta: { title: '疗程卡配置', icon: 'Postcard' }
      },
      {
        path: 'store/treatment/sale',
        name: 'StoreTreatmentSale',
        component: () => import('@/views/store/treatment/sale/index.vue'),
        meta: { title: '疗程卡销售', icon: 'Sell' }
      },
      {
        path: 'store/treatment/verify',
        name: 'StoreTreatmentVerify',
        component: () => import('@/views/store/treatment/verify/index.vue'),
        meta: { title: '疗程卡核销', icon: 'CircleCheck' }
      },
      {
        path: 'store/treatment/expire',
        name: 'StoreTreatmentExpire',
        component: () => import('@/views/store/treatment/expire/index.vue'),
        meta: { title: '到期提醒', icon: 'Timer' }
      },
      // ========== 服务人员模块 ==========
      {
        path: 'store/technician/profile',
        name: 'StoreTechnicianProfile',
        component: () => import('@/views/store/technician/profile/index.vue'),
        meta: { title: '商家技师', icon: 'Avatar' }
      },
      {
        path: 'store/technician/statistic',
        name: 'StoreTechnicianStatistic',
        component: () => import('@/views/store/technician/statistic/index.vue'),
        meta: { title: '技师统计', icon: 'DataLine' }
      },
      // ========== 样品赠品模块 ==========
      {
        path: 'store/sample/profile',
        name: 'StoreSampleProfile',
        component: () => import('@/views/store/sample/profile/index.vue'),
        meta: { title: '样品赠品档案', icon: 'Present' }
      },
      {
        path: 'store/sample/receive',
        name: 'StoreSampleReceive',
        component: () => import('@/views/store/sample/receive/index.vue'),
        meta: { title: '样品领用', icon: 'Download' }
      },
      {
        path: 'store/sample/out',
        name: 'StoreSampleOut',
        component: () => import('@/views/store/sample/out/index.vue'),
        meta: { title: '赠品出库', icon: 'Upload' }
      },
      {
        path: 'store/sample/inventory',
        name: 'StoreSampleInventory',
        component: () => import('@/views/store/sample/inventory/index.vue'),
        meta: { title: '库存查询', icon: 'Search' }
      },
      {
        path: 'store/sample/report',
        name: 'StoreSampleReport',
        component: () => import('@/views/store/sample/report/index.vue'),
        meta: { title: '统计报表', icon: 'PieChart' }
      },
      // ========== P1: 仪器设备模块 ==========
      {
        path: 'store/equipment/profile',
        name: 'StoreEquipmentProfile',
        component: () => import('@/views/store/equipment/profile/index.vue'),
        meta: { title: '设备台账', icon: 'Setting' }
      },
      {
        path: 'store/equipment/maintenance',
        name: 'StoreEquipmentMaintenance',
        component: () => import('@/views/store/equipment/maintenance/index.vue'),
        meta: { title: '设备保养记录', icon: 'Tools' }
      },
      // ========== P1: 房间/床位模块 ==========
      {
        path: 'store/room/profile',
        name: 'StoreRoomProfile',
        component: () => import('@/views/store/room/profile/index.vue'),
        meta: { title: '房间/床位管理', icon: 'House' }
      },
      // ========== P1: 耗材BOM + 库存操作模块 ==========
      {
        path: 'store/product/bom',
        name: 'StoreProductBom',
        component: () => import('@/views/store/product/bom/index.vue'),
        meta: { title: '耗材BOM管理', icon: 'Connection' }
      },
      {
        path: 'store/product/inbound',
        name: 'StoreProductInbound',
        component: () => import('@/views/store/product/inbound/index.vue'),
        meta: { title: '入库操作', icon: 'BottomRight' }
      },
      {
        path: 'store/product/outbound',
        name: 'StoreProductOutbound',
        component: () => import('@/views/store/product/outbound/index.vue'),
        meta: { title: '出库操作', icon: 'TopRight' }
      },
      {
        path: 'store/product/check',
        name: 'StoreProductCheck',
        component: () => import('@/views/store/product/check/index.vue'),
        meta: { title: '库存盘点', icon: 'Checked' }
      },
      {
        path: 'store/product/transfer',
        name: 'StoreProductTransfer',
        component: () => import('@/views/store/product/transfer/index.vue'),
        meta: { title: '库存调拨', icon: 'Switch' }
      },
      // ========== P1: 采购管理模块 ==========
      {
        path: 'store/product/purchase-order',
        name: 'StoreProductPurchaseOrder',
        component: () => import('@/views/store/product/purchase-order/index.vue'),
        meta: { title: '采购记录', icon: 'ShoppingCart' }
      },
      {
        path: 'store/product/purchase-return',
        name: 'StoreProductPurchaseReturn',
        component: () => import('@/views/store/product/purchase-return/index.vue'),
        meta: { title: '采购退货', icon: 'RefreshLeft' }
      },
      // ========== P1: 价格变更记录 ==========
      {
        path: 'store/product/price-log',
        name: 'StoreProductPriceLog',
        component: () => import('@/views/store/product/price-log/index.vue'),
        meta: { title: '价格变更记录', icon: 'Document' }
      },
      // ========== P1: 财务日结模块 ==========
      {
        path: 'store/finance/daily-settlement',
        name: 'StoreFinanceDailySettlement',
        component: () => import('@/views/store/finance/daily-settlement/index.vue'),
        meta: { title: '简易日结', icon: 'WalletFilled' }
      },
      // ========== P1: 疗程卡转让 ==========
      {
        path: 'store/treatment/transfer',
        name: 'StoreTreatmentTransfer',
        component: () => import('@/views/store/treatment/transfer/index.vue'),
        meta: { title: '疗程卡转让', icon: 'Switch' }
      },
      // ========== P1: 客户档案增强模块 ==========
      {
        path: 'store/customer/beauty-profile',
        name: 'StoreCustomerBeautyProfile',
        component: () => import('@/views/store/customer/beauty-profile/index.vue'),
        meta: { title: '肤质档案', icon: 'MagicStick' }
      },
      {
        path: 'store/customer/reaction',
        name: 'StoreCustomerReaction',
        component: () => import('@/views/store/customer/reaction/index.vue'),
        meta: { title: '过敏记录', icon: 'WarningFilled' }
      },
      {
        path: 'store/customer/photo',
        name: 'StoreCustomerPhoto',
        component: () => import('@/views/store/customer/photo/index.vue'),
        meta: { title: '对比照片', icon: 'Camera' }
      },
      {
        path: 'store/customer/body-data',
        name: 'StoreCustomerBodyData',
        component: () => import('@/views/store/customer/body-data/index.vue'),
        meta: { title: '身体数据', icon: 'DataAnalysis' }
      },
      // ========== P1: 技能分类配置 ==========
      {
        path: 'store/technician/skill',
        name: 'StoreTechnicianSkill',
        component: () => import('@/views/store/technician/skill/index.vue'),
        meta: { title: '技能分类', icon: 'Grid' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, _from, next) => {
  document.title = `${to.meta.title} - BMS后台管理系统`
  const token = localStorage.getItem('token')
  const userStore = useUserStore()

  if (to.path === '/login') {
    next()
  } else {
    if (!token) {
      next('/login')
    } else {
      // 刷新页面后，如果 token 存在但 userInfo 为空，则重新获取用户信息和权限数据
      if (!userStore.userInfo.id || !userStore.userInfo.roles.length) {
        try {
          const user = await getCurrentUser()

          const roles = user.roles?.map((r: any) => typeof r === 'string' ? r : r.code) || []
          const roleIds = user.roleIds || []

          userStore.userInfo = {
            id: user.id,
            userName: user.userName,
            realName: user.realName,
            avatar: user.avatar || '',
            email: user.email,
            phone: user.phone,
            roles: roles,
            roleIds: roleIds,
            permissions: user.permissions || [],
            tenantId: user.tenantId,
            tenantCode: user.tenantCode,
            maxRoleLevel: user.maxRoleLevel ?? 100
          }

          // 缓存租户信息到 localStorage
          if (user.tenantId) {
            localStorage.setItem('tenantId', String(user.tenantId))
          }
          if (user.tenantCode) {
            localStorage.setItem('tenantCode', user.tenantCode)
          }

          // 加载授权子系统列表
          await userStore.getAuthorizedSubsystems()

          // 加载菜单（会根据当前子系统过滤）
          await userStore.getMenus()

          // 若当前是 store 子系统且门店列表未加载，先加载授权门店列表
          // 注意：刷新后 currentStoreId 会从 localStorage 恢复，但 authorizedStores 是 Pinia state 会重置为空
          // 所以条件必须检查 authorizedStores.length，而非 currentStoreId
          if (userStore.isStoreSubsystem && userStore.authorizedStores.length === 0) {
            await userStore.getAuthorizedStores()
          }
        } catch (error) {
          // 获取用户信息失败，跳转到登录页
          localStorage.removeItem('token')
          next('/login')
          return
        }
      } else {
        // 已有用户信息：若目标路由是 store 子系统且门店列表未加载，兜底加载
        if (to.path.startsWith('/store') && userStore.isStoreSubsystem && userStore.authorizedStores.length === 0) {
          await userStore.getAuthorizedStores()
        }
      }

      // 首页跳转：进入子系统时显示已授权菜单排序第1的页面，而非硬编码 dashboard
      // 同时拦截 / 和 /dashboard，避免登录后或直接访问 dashboard 时绕过授权菜单逻辑
      // 无授权菜单时：跳转到 /no-permission 提示页
      if (to.path === '/' || to.path === '/dashboard') {
        const firstPath = userStore.firstAuthorizedLeafPath
        if (firstPath && firstPath !== to.path) {
          next(firstPath)
          return
        }
        if (!firstPath) {
          next('/no-permission')
          return
        }
      }

      next()
    }
  }
})

export default router
