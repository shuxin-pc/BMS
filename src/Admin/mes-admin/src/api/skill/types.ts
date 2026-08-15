// ==========================================
// 技能分类配置类型定义
// ==========================================

/**
 * 技能分类信息（支持层级结构）
 */
export interface SkillCategory {
  /** 分类ID */
  id: number
  /** 分类名称 */
  name: string
  /** 分类编码 */
  code: string
  /** 父分类ID（0表示顶级分类） */
  parentId: number
  /** 子分类列表 */
  children?: SkillCategory[]
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 技能分类查询参数
 */
export interface SkillCategoryQuery {
  /** 名称（模糊匹配） */
  name?: string
  /** 编码（模糊匹配） */
  code?: string
}

/**
 * 创建技能分类请求
 */
export interface SkillCategoryCreate {
  name: string
  code: string
  parentId: number
}

/**
 * 更新技能分类请求
 */
export interface SkillCategoryUpdate extends SkillCategoryCreate {
  id: number
}
