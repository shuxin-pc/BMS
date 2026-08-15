// 图片上传共享封装
// 各业务模块只需拿到 objectKey 存入自己的表，不关心桶名、路径前缀与签名规则

import { request } from './storeRequest'

/** 上传结果（与后端 FileUploadResult 对齐） */
export interface FileUploadResult {
  /** 对象键，需按原样提交给业务接口保存 */
  objectKey: string
  fileName: string
  size: number
}

/**
 * 上传单张图片
 * @param file 图片文件
 * @param bizType 业务类型，需命中后端配置的白名单（如 customer-photo）
 */
export function uploadImage(file: File, bizType: string): Promise<FileUploadResult> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('bizType', bizType)
  return request<FileUploadResult>('/files', { method: 'POST', body: formData })
}
