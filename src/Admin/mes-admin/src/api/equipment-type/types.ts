// ==========================================
// 设备类型类型定义
// ==========================================

/**
 * 设备类型（租户级共享，所有门店共用一套）
 * 支持父子级自引用：父级节点为分类/分组（如"激光类"），子级节点为具体型号（如"飞顿激光"）
 */
export interface EquipmentType {
  /** 类型ID */
  id: number
  /** 类型名称（如"激光类"、"飞顿激光"、"热玛吉"） */
  name: string
  /** 类型编码（租户内唯一） */
  code: string
  /** 父级类型ID（null=顶级分类/分组） */
  parentId?: number | null
  /** 父级类型名称（冗余展示字段） */
  parentName?: string
  /** 规格/型号 */
  spec?: string
  /** 备注说明 */
  description?: string
  /** 是否启用（停用后设备档案不可再选择该类型） */
  isActive: boolean
  /** 关联的设备实例数量 */
  equipmentCount: number
  /** 子级类型列表（树形结构） */
  children?: EquipmentType[]
  /** 创建时间 */
  createdAt?: string
}

/**
 * 设备类型查询参数
 */
export interface EquipmentTypeQuery {
  /** 类型名称（模糊匹配） */
  name?: string
  /** 类型编码（模糊匹配） */
  code?: string
  /** 启用状态筛选 */
  isActive?: boolean
  /** 父级类型ID筛选 */
  parentId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 新建设备类型请求
 */
export interface EquipmentTypeCreate {
  name: string
  code: string
  /** 父级类型ID（null=顶级分类/分组） */
  parentId?: number | null
  spec?: string
  description?: string
  isActive: boolean
}

/**
 * 更新设备类型请求
 */
export interface EquipmentTypeUpdate extends EquipmentTypeCreate {
  id: number
}
