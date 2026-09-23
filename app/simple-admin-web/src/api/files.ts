/**
 * 文件管理 API（手动维护，与 /files 动态 API 对齐）
 */
import { OpenAPI } from './generated/core/OpenAPI'
import { request as __request } from './generated/core/request'
import type { CancelablePromise } from './generated/core/CancelablePromise'

export interface FileDto {
  id: string
  fileName: string
  contentType?: string | null
  size: number
  url?: string | null
}

export interface PresignUploadInput {
  fileName: string
  contentType?: string
  policy?: string
  expiresSeconds?: number
}

export interface PresignUploadDto {
  fileId: string
  uploadUrl: string
  method: string
  contentType?: string | null
  headers?: Record<string, string> | null
  expireAt: string
}

export interface ConfirmUploadInput {
  fileId: string
  fileName?: string
  contentType?: string
  size?: number
  verifyExists?: boolean
}

export class FileApi {
  /** 服务端转发上传 */
  public static upload(formData: { file: Blob; policy?: string }): CancelablePromise<FileDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/files/file/upload',
      formData,
      mediaType: 'multipart/form-data',
      errors: { 400: 'Bad Request' }
    })
  }

  /** 预签名直传凭证 */
  public static presignUpload(body: PresignUploadInput): CancelablePromise<PresignUploadDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/files/file/presign-upload',
      body,
      mediaType: 'application/json',
      errors: { 400: 'Bad Request' }
    })
  }

  /** 直传完成后确认 */
  public static confirmUpload(body: ConfirmUploadInput): CancelablePromise<FileDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/files/file/confirm-upload',
      body,
      mediaType: 'application/json',
      errors: { 400: 'Bad Request' }
    })
  }

  public static get(id: string): CancelablePromise<FileDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/files/file/get',
      query: { id },
      errors: { 400: 'Bad Request' }
    })
  }

  public static getAccessUrl(id: string, expiresSeconds?: number): CancelablePromise<string> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/files/file/access-url',
      query: { id, expiresSeconds },
      errors: { 400: 'Bad Request' }
    })
  }

  public static delete(id: string): CancelablePromise<void> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/files/file/delete',
      query: { id },
      errors: { 400: 'Bad Request' }
    })
  }
}
