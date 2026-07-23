// 客户档案增强 - API服务（Mock 数据）
// 包含：肤质档案、过敏记录、服务对比照片、身体数据记录
import type {
  CustomerBeautyProfile,
  BeautyProfileQuery,
  BeautyProfileSave,
  ServiceReaction,
  ServiceReactionQuery,
  ServiceReactionCreate,
  ServiceComparisonPhoto,
  ComparisonPhotoQuery,
  ComparisonPhotoCreate,
  PhotoPair,
  BodyDataRecord,
  BodyDataQuery,
  BodyDataCreate,
  CustomerOption,
  ApiResponse,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  CustomerBeautyProfile,
  BeautyProfileQuery,
  BeautyProfileSave,
  ServiceReaction,
  ServiceReactionQuery,
  ServiceReactionCreate,
  ServiceComparisonPhoto,
  ComparisonPhotoQuery,
  ComparisonPhotoCreate,
  PhotoPair,
  BodyDataRecord,
  BodyDataQuery,
  BodyDataCreate,
  CustomerOption,
  ApiResponse,
  PagedResponse
}

// ==================== 公共 Mock 数据 ====================

const mockCustomers: CustomerOption[] = [
  { id: 1001, name: '张丽华', phone: '13800138001' },
  { id: 1002, name: '李秀英', phone: '13800138002' },
  { id: 1003, name: '王芳', phone: '13800138003' },
  { id: 1004, name: '赵敏', phone: '13800138004' },
  { id: 1005, name: '孙丽', phone: '13800138005' },
  { id: 1006, name: '刘婷', phone: '13800138006' },
  { id: 1007, name: '陈静', phone: '13800138007' },
  { id: 1008, name: '周琳', phone: '13800138008' }
]

/**
 * 获取客户列表（下拉选择用）
 * @returns 客户列表
 */
export async function getCustomerOptions(): Promise<CustomerOption[]> {
  await new Promise(resolve => setTimeout(resolve, 200))
  return mockCustomers.map(item => ({ ...item }))
}

// ==================== 肤质档案 Mock 数据 ====================

const mockBeautyProfiles: CustomerBeautyProfile[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    skinType: 'dry',
    sensitivity: 'medium',
    hairType: '干性发质，发梢分叉',
    allergyHistory: '对酒精过敏，禁用含酒精产品',
    remark: '冬季需加强补水',
    createdAt: '2026-06-01 10:00:00',
    updatedAt: '2026-07-05 14:30:00'
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李秀英',
    customerPhone: '13800138002',
    skinType: 'oily',
    sensitivity: 'low',
    hairType: '油性发质',
    allergyHistory: '无',
    remark: 'T区出油较多',
    createdAt: '2026-06-05 11:20:00'
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王芳',
    customerPhone: '13800138003',
    skinType: 'combination',
    sensitivity: 'medium',
    hairType: '中性发质',
    allergyHistory: '对果酸类产品敏感',
    remark: '',
    createdAt: '2026-06-10 15:00:00',
    updatedAt: '2026-07-08 09:15:00'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '赵敏',
    customerPhone: '13800138004',
    skinType: 'sensitive',
    sensitivity: 'high',
    hairType: '细软发质',
    allergyHistory: '对香料、防腐剂过敏，禁用含苯氧乙醇产品',
    remark: '需使用敏感肌专用产品',
    createdAt: '2026-06-15 13:45:00'
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '孙丽',
    customerPhone: '13800138005',
    skinType: 'normal',
    sensitivity: 'low',
    hairType: '中性发质',
    allergyHistory: '无已知过敏',
    remark: '肤质状况良好',
    createdAt: '2026-06-20 16:30:00'
  },
  {
    id: 6,
    customerId: 1006,
    customerName: '刘婷',
    customerPhone: '13800138006',
    skinType: 'dry',
    sensitivity: 'high',
    hairType: '干性发质，易断',
    allergyHistory: '对水杨酸不耐受',
    remark: '建议温和洁面，避免去角质',
    createdAt: '2026-07-01 10:15:00'
  }
]

let beautyProfileIdCounter = 100

/**
 * 获取肤质档案分页列表
 * @param query 查询参数
 * @returns 分页肤质档案列表
 */
export async function getBeautyProfiles(query?: BeautyProfileQuery): Promise<PagedResponse<CustomerBeautyProfile>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockBeautyProfiles]
  if (query?.customerName) list = list.filter(item => (item.customerName || '').includes(query.customerName!))
  if (query?.skinType) list = list.filter(item => item.skinType === query.skinType)
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 获取客户的肤质档案
 * @param customerId 客户ID
 * @returns 肤质档案（可能不存在）
 */
export async function getBeautyProfileByCustomer(customerId: number): Promise<CustomerBeautyProfile | null> {
  await new Promise(resolve => setTimeout(resolve, 200))
  const item = mockBeautyProfiles.find(p => p.customerId === customerId)
  return item ? { ...item } : null
}

/**
 * 保存肤质档案（创建或更新）
 * @param data 肤质档案信息
 * @returns 保存后的肤质档案
 */
export async function saveBeautyProfile(data: BeautyProfileSave): Promise<CustomerBeautyProfile> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const customer = mockCustomers.find(c => c.id === data.customerId)
  if (!customer) throw new Error('客户不存在')

  const existing = mockBeautyProfiles.find(p => p.customerId === data.customerId)
  const now = new Date().toISOString().replace('T', ' ').substring(0, 19)

  if (existing) {
    existing.skinType = data.skinType
    existing.sensitivity = data.sensitivity
    existing.hairType = data.hairType
    existing.allergyHistory = data.allergyHistory
    existing.remark = data.remark
    existing.updatedAt = now
    return { ...existing }
  }

  const newProfile: CustomerBeautyProfile = {
    id: ++beautyProfileIdCounter,
    customerId: data.customerId,
    customerName: customer.name,
    customerPhone: customer.phone,
    skinType: data.skinType,
    sensitivity: data.sensitivity,
    hairType: data.hairType,
    allergyHistory: data.allergyHistory,
    remark: data.remark,
    createdAt: now
  }
  mockBeautyProfiles.push(newProfile)
  return { ...newProfile }
}

// ==================== 过敏/服务反应记录 Mock 数据 ====================

const mockServiceReactions: ServiceReaction[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    orderId: 2001,
    orderNo: 'OD20260705001',
    serviceItem: '深层补水面部护理',
    reactionDate: '2026-07-05',
    reaction: '面部轻微泛红，约2小时后消退',
    severity: 1,
    remark: '客户表示补水面膜有轻微刺痛感',
    createdAt: '2026-07-05 15:00:00'
  },
  {
    id: 2,
    customerId: 1004,
    customerName: '赵敏',
    customerPhone: '13800138004',
    orderId: 2002,
    orderNo: 'OD20260708002',
    serviceItem: '果酸焕肤',
    reactionDate: '2026-07-08',
    reaction: '面部红肿，持续1天，有轻微脱皮',
    severity: 2,
    remark: '已建议客户暂停使用果酸类产品',
    createdAt: '2026-07-08 17:30:00'
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王芳',
    customerPhone: '13800138003',
    orderId: 2003,
    orderNo: 'OD20260710003',
    serviceItem: '光子嫩肤',
    reactionDate: '2026-07-10',
    reaction: '局部轻微发红，无不适感',
    severity: 1,
    remark: '属于正常术后反应',
    createdAt: '2026-07-10 16:00:00'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '赵敏',
    customerPhone: '13800138004',
    orderId: 2004,
    orderNo: 'OD20260703004',
    serviceItem: '水光针注射',
    reactionDate: '2026-07-03',
    reaction: '注射部位严重红肿，伴有瘙痒，持续3天',
    severity: 3,
    remark: '已就医，确诊为过敏反应，后续禁用水光针',
    createdAt: '2026-07-03 18:00:00'
  },
  {
    id: 5,
    customerId: 1006,
    customerName: '刘婷',
    customerPhone: '13800138006',
    orderId: 2005,
    orderNo: 'OD20260712005',
    serviceItem: '肩颈疏通按摩',
    reactionDate: '2026-07-12',
    reaction: '按摩后轻微酸痛，次日缓解',
    severity: 1,
    remark: '客户首次按摩，属正常反应',
    createdAt: '2026-07-12 14:00:00'
  },
  {
    id: 6,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    orderId: 2006,
    orderNo: 'OD20260711006',
    serviceItem: '精华导入',
    reactionDate: '2026-07-11',
    reaction: '无异常反应',
    severity: 1,
    remark: '客户适应良好',
    createdAt: '2026-07-11 15:30:00'
  },
  {
    id: 7,
    customerId: 1002,
    customerName: '李秀英',
    customerPhone: '13800138002',
    serviceItem: '全身精油按摩',
    reactionDate: '2026-07-09',
    reaction: '背部出现少量红疹，疑似精油过敏',
    severity: 2,
    remark: '已更换精油品牌，后续观察',
    createdAt: '2026-07-09 17:00:00'
  }
]

let reactionIdCounter = 100

/**
 * 获取服务反应分页列表
 * @param query 查询参数
 * @returns 分页服务反应列表
 */
export async function getServiceReactions(query?: ServiceReactionQuery): Promise<PagedResponse<ServiceReaction>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockServiceReactions]
  if (query?.customerName) list = list.filter(item => (item.customerName || '').includes(query.customerName!))
  if (query?.startDate) list = list.filter(item => item.reactionDate >= query.startDate!)
  if (query?.endDate) list = list.filter(item => item.reactionDate <= query.endDate!)
  if (query?.severity !== undefined) list = list.filter(item => item.severity === query.severity)
  list.sort((a, b) => b.reactionDate.localeCompare(a.reactionDate))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 创建服务反应记录
 * @param data 反应记录信息
 * @returns 创建后的记录
 */
export async function createServiceReaction(data: ServiceReactionCreate): Promise<ServiceReaction> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const customer = mockCustomers.find(c => c.id === data.customerId)
  if (!customer) throw new Error('客户不存在')
  const now = new Date().toISOString().replace('T', ' ').substring(0, 19)
  const newRecord: ServiceReaction = {
    id: ++reactionIdCounter,
    customerId: data.customerId,
    customerName: customer.name,
    customerPhone: customer.phone,
    orderId: data.orderId,
    serviceItem: data.serviceItem,
    reactionDate: data.reactionDate,
    reaction: data.reaction,
    severity: data.severity,
    remark: data.remark,
    createdAt: now
  }
  mockServiceReactions.unshift(newRecord)
  return { ...newRecord }
}

// ==================== 服务对比照片 Mock 数据 ====================

// 使用 placeholder 图片作为 Mock 照片
const mockPhotoBaseUrl = 'https://picsum.photos/seed'

const mockComparisonPhotos: ServiceComparisonPhoto[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张丽华',
    orderId: 2001,
    orderNo: 'OD20260705001',
    serviceItem: '深层补水面部护理',
    photoDate: '2026-07-05',
    photoType: 1,
    photoUrl: `${mockPhotoBaseUrl}/before1/400/400`,
    remark: '护理前',
    createdAt: '2026-07-05 14:00:00'
  },
  {
    id: 2,
    customerId: 1001,
    customerName: '张丽华',
    orderId: 2001,
    orderNo: 'OD20260705001',
    serviceItem: '深层补水面部护理',
    photoDate: '2026-07-05',
    photoType: 2,
    photoUrl: `${mockPhotoBaseUrl}/after1/400/400`,
    remark: '护理后，皮肤含水量明显提升',
    createdAt: '2026-07-05 15:30:00'
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王芳',
    orderId: 2003,
    orderNo: 'OD20260710003',
    serviceItem: '光子嫩肤',
    photoDate: '2026-07-10',
    photoType: 1,
    photoUrl: `${mockPhotoBaseUrl}/before2/400/400`,
    remark: '嫩肤前，色斑明显',
    createdAt: '2026-07-10 14:00:00'
  },
  {
    id: 4,
    customerId: 1003,
    customerName: '王芳',
    orderId: 2003,
    orderNo: 'OD20260710003',
    serviceItem: '光子嫩肤',
    photoDate: '2026-07-10',
    photoType: 2,
    photoUrl: `${mockPhotoBaseUrl}/after2/400/400`,
    remark: '嫩肤后即刻',
    createdAt: '2026-07-10 16:00:00'
  },
  {
    id: 5,
    customerId: 1004,
    customerName: '赵敏',
    orderId: 2002,
    orderNo: 'OD20260708002',
    serviceItem: '果酸焕肤',
    photoDate: '2026-07-08',
    photoType: 1,
    photoUrl: `${mockPhotoBaseUrl}/before3/400/400`,
    remark: '焕肤前',
    createdAt: '2026-07-08 14:00:00'
  },
  {
    id: 6,
    customerId: 1004,
    customerName: '赵敏',
    orderId: 2002,
    orderNo: 'OD20260708002',
    serviceItem: '果酸焕肤',
    photoDate: '2026-07-08',
    photoType: 2,
    photoUrl: `${mockPhotoBaseUrl}/after3/400/400`,
    remark: '焕肤后，有轻微红肿',
    createdAt: '2026-07-08 17:00:00'
  },
  {
    id: 7,
    customerId: 1001,
    customerName: '张丽华',
    orderId: 2006,
    orderNo: 'OD20260711006',
    serviceItem: '精华导入',
    photoDate: '2026-07-11',
    photoType: 1,
    photoUrl: `${mockPhotoBaseUrl}/before4/400/400`,
    remark: '导入前',
    createdAt: '2026-07-11 14:00:00'
  },
  {
    id: 8,
    customerId: 1001,
    customerName: '张丽华',
    orderId: 2006,
    orderNo: 'OD20260711006',
    serviceItem: '精华导入',
    photoDate: '2026-07-11',
    photoType: 2,
    photoUrl: `${mockPhotoBaseUrl}/after4/400/400`,
    remark: '导入后，肤色提亮',
    createdAt: '2026-07-11 15:30:00'
  }
]

let photoIdCounter = 100

/**
 * 获取对比照片分页列表
 * @param query 查询参数
 * @returns 分页照片列表
 */
export async function getComparisonPhotos(query?: ComparisonPhotoQuery): Promise<PagedResponse<ServiceComparisonPhoto>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockComparisonPhotos]
  if (query?.customerId !== undefined) list = list.filter(item => item.customerId === query.customerId)
  if (query?.customerName) list = list.filter(item => (item.customerName || '').includes(query.customerName!))
  if (query?.serviceItem) list = list.filter(item => (item.serviceItem || '').includes(query.serviceItem!))
  list.sort((a, b) => b.photoDate.localeCompare(a.photoDate))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 创建对比照片记录
 * @param data 照片信息
 * @returns 创建后的照片记录
 */
export async function createComparisonPhoto(data: ComparisonPhotoCreate): Promise<ServiceComparisonPhoto> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const customer = mockCustomers.find(c => c.id === data.customerId)
  if (!customer) throw new Error('客户不存在')
  const now = new Date().toISOString().replace('T', ' ').substring(0, 19)
  const newPhoto: ServiceComparisonPhoto = {
    id: ++photoIdCounter,
    customerId: data.customerId,
    customerName: customer.name,
    orderId: data.orderId,
    serviceItem: data.serviceItem,
    photoDate: data.photoDate,
    photoType: data.photoType,
    photoUrl: data.photoUrl,
    remark: data.remark,
    createdAt: now
  }
  mockComparisonPhotos.push(newPhoto)
  return { ...newPhoto }
}

// ==================== 身体数据记录 Mock 数据 ====================

const mockBodyDataRecords: BodyDataRecord[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    recordDate: '2026-06-01',
    weight: 58.5,
    bodyFat: 25.3,
    bust: 86,
    waist: 72,
    hip: 92,
    remark: '初始记录',
    createdAt: '2026-06-01 10:00:00'
  },
  {
    id: 2,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    recordDate: '2026-06-15',
    weight: 57.8,
    bodyFat: 24.1,
    bust: 86,
    waist: 70,
    hip: 91,
    remark: '瘦身塑形课程第2周',
    createdAt: '2026-06-15 10:00:00'
  },
  {
    id: 3,
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    recordDate: '2026-07-01',
    weight: 56.5,
    bodyFat: 22.8,
    bust: 85,
    waist: 68,
    hip: 90,
    remark: '瘦身塑形课程第4周，腰围减少4cm',
    createdAt: '2026-07-01 10:00:00'
  },
  {
    id: 4,
    customerId: 1003,
    customerName: '王芳',
    customerPhone: '13800138003',
    recordDate: '2026-06-10',
    weight: 62.0,
    bodyFat: 28.5,
    bust: 90,
    waist: 78,
    hip: 96,
    remark: '初始记录',
    createdAt: '2026-06-10 14:00:00'
  },
  {
    id: 5,
    customerId: 1003,
    customerName: '王芳',
    customerPhone: '13800138003',
    recordDate: '2026-07-08',
    weight: 60.5,
    bodyFat: 26.2,
    bust: 89,
    waist: 75,
    hip: 94,
    remark: '疗程过半，体重下降1.5kg',
    createdAt: '2026-07-08 14:00:00'
  },
  {
    id: 6,
    customerId: 1005,
    customerName: '孙丽',
    customerPhone: '13800138005',
    recordDate: '2026-07-05',
    weight: 50.2,
    bodyFat: 18.5,
    bust: 82,
    waist: 64,
    hip: 88,
    remark: '身材匀称，维持良好',
    createdAt: '2026-07-05 11:00:00'
  },
  {
    id: 7,
    customerId: 1006,
    customerName: '刘婷',
    customerPhone: '13800138006',
    recordDate: '2026-06-20',
    weight: 55.0,
    bodyFat: 23.0,
    bust: 84,
    waist: 70,
    hip: 90,
    remark: '开始纤体课程',
    createdAt: '2026-06-20 15:00:00'
  },
  {
    id: 8,
    customerId: 1006,
    customerName: '刘婷',
    customerPhone: '13800138006',
    recordDate: '2026-07-10',
    weight: 54.0,
    bodyFat: 21.5,
    bust: 84,
    waist: 68,
    hip: 89,
    remark: '纤体课程3周，体脂下降1.5%',
    createdAt: '2026-07-10 15:00:00'
  }
]

let bodyDataIdCounter = 100

/**
 * 获取身体数据分页列表
 * @param query 查询参数
 * @returns 分页身体数据列表
 */
export async function getBodyDataRecords(query?: BodyDataQuery): Promise<PagedResponse<BodyDataRecord>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockBodyDataRecords]
  if (query?.customerId !== undefined) list = list.filter(item => item.customerId === query.customerId)
  if (query?.customerName) list = list.filter(item => (item.customerName || '').includes(query.customerName!))
  if (query?.startDate) list = list.filter(item => item.recordDate >= query.startDate!)
  if (query?.endDate) list = list.filter(item => item.recordDate <= query.endDate!)
  list.sort((a, b) => b.recordDate.localeCompare(a.recordDate))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 创建身体数据记录
 * @param data 身体数据信息
 * @returns 创建后的记录
 */
export async function createBodyDataRecord(data: BodyDataCreate): Promise<BodyDataRecord> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const customer = mockCustomers.find(c => c.id === data.customerId)
  if (!customer) throw new Error('客户不存在')
  const now = new Date().toISOString().replace('T', ' ').substring(0, 19)
  const newRecord: BodyDataRecord = {
    id: ++bodyDataIdCounter,
    customerId: data.customerId,
    customerName: customer.name,
    customerPhone: customer.phone,
    recordDate: data.recordDate,
    weight: data.weight,
    bodyFat: data.bodyFat,
    bust: data.bust,
    waist: data.waist,
    hip: data.hip,
    remark: data.remark,
    createdAt: now
  }
  mockBodyDataRecords.unshift(newRecord)
  return { ...newRecord }
}
