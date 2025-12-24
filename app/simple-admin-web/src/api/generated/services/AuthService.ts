/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LoginResultDto } from '../models/LoginResultDto';
import type { UserInfoDto } from '../models/UserInfoDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthService {
  /**
   * @returns LoginResultDto Success
   * @throws ApiError
   */
  public static postRbacAuthLogin({
    account,
    password,
  }: {
    account?: string,
    password?: string,
  }): CancelablePromise<LoginResultDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/auth/login',
      query: {
        'account': account,
        'password': password,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns LoginResultDto Success
   * @throws ApiError
   */
  public static postRbacAuthRefreshToken({
    refreshToken,
  }: {
    refreshToken?: string,
  }): CancelablePromise<LoginResultDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/auth/refresh-token',
      query: {
        'refreshToken': refreshToken,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserInfoDto Success
   * @throws ApiError
   */
  public static getRbacAuthGetCurrentUser(): CancelablePromise<UserInfoDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/auth/get-current-user',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static postRbacAuthChangePassword({
    oldPassword,
    newPassword,
  }: {
    oldPassword?: string,
    newPassword?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/auth/change-password',
      query: {
        'oldPassword': oldPassword,
        'newPassword': newPassword,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserInfoDto Success
   * @throws ApiError
   */
  public static putRbacAuthUpdateProfile({
    name,
    email,
  }: {
    name?: string,
    email?: string,
  }): CancelablePromise<UserInfoDto> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/auth/update-profile',
      query: {
        'name': name,
        'email': email,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns string Success
   * @throws ApiError
   */
  public static postRbacAuthUploadAvatar({
    formData,
  }: {
    formData?: {
      file?: Blob;
    },
  }): CancelablePromise<string> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/auth/upload-avatar',
      formData: formData,
      mediaType: 'multipart/form-data',
      errors: {
        400: `Bad Request`,
      },
    });
  }
}
