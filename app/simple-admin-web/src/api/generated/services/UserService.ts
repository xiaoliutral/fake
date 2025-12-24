/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { RoleDto } from '../models/RoleDto';
import type { UpdatePasswordDto } from '../models/UpdatePasswordDto';
import type { UserCreateDto } from '../models/UserCreateDto';
import type { UserDto } from '../models/UserDto';
import type { UserDtoPagedResultDto } from '../models/UserDtoPagedResultDto';
import type { UserSimpleDto } from '../models/UserSimpleDto';
import type { UserUpdateDto } from '../models/UserUpdateDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class UserService {
  /**
   * @returns UserDto Success
   * @throws ApiError
   */
  public static getRbacUserGet({
    id,
  }: {
    id?: string,
  }): CancelablePromise<UserDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/user/get',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserDtoPagedResultDto Success
   * @throws ApiError
   */
  public static getRbacUserGetList({
    keyword,
    roleId,
    organizationId,
    page,
    pageSize,
    orderBy,
    descending,
  }: {
    /**
     * 关键字（搜索名称或账号）
     */
    keyword?: string,
    /**
     * 角色ID
     */
    roleId?: string,
    /**
     * 组织ID
     */
    organizationId?: string,
    /**
     * 页码（从1开始）
     */
    page?: number,
    /**
     * 每页数量
     */
    pageSize?: number,
    /**
     * 排序字段
     */
    orderBy?: string,
    /**
     * 是否降序
     */
    descending?: boolean,
  }): CancelablePromise<UserDtoPagedResultDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/user/get-list',
      query: {
        'Keyword': keyword,
        'RoleId': roleId,
        'OrganizationId': organizationId,
        'Page': page,
        'PageSize': pageSize,
        'OrderBy': orderBy,
        'Descending': descending,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserSimpleDto Success
   * @throws ApiError
   */
  public static getRbacUserGetUsersByRole({
    roleId,
  }: {
    roleId?: string,
  }): CancelablePromise<Array<UserSimpleDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/user/get-users-by-role',
      query: {
        'roleId': roleId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserDto Success
   * @throws ApiError
   */
  public static postRbacUserCreate({
    requestBody,
  }: {
    requestBody?: UserCreateDto,
  }): CancelablePromise<UserDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/user/create',
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns UserDto Success
   * @throws ApiError
   */
  public static putRbacUserUpdate({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: UserUpdateDto,
  }): CancelablePromise<UserDto> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/user/update',
      query: {
        'id': id,
      },
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static putRbacUserUpdatePassword({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: UpdatePasswordDto,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/user/update-password',
      query: {
        'id': id,
      },
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static putRbacUserUpdateAvatar({
    id,
    avatarUrl,
  }: {
    id?: string,
    avatarUrl?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/user/update-avatar',
      query: {
        'id': id,
        'avatarUrl': avatarUrl,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static deleteRbacUserDelete({
    id,
  }: {
    id?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/rbac/user/delete',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static deleteRbacUserDeleteBatch({
    ids,
  }: {
    ids?: Array<string>,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/rbac/user/delete-batch',
      query: {
        'ids': ids,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static postRbacUserAssignRoles({
    userId,
    requestBody,
  }: {
    userId?: string,
    requestBody?: Array<string>,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/user/assign-roles',
      query: {
        'userId': userId,
      },
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns RoleDto Success
   * @throws ApiError
   */
  public static getRbacUserGetUserRoles({
    userId,
  }: {
    userId?: string,
  }): CancelablePromise<Array<RoleDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/user/get-user-roles',
      query: {
        'userId': userId,
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
  public static getRbacUserGetUserPermissions({
    userId,
  }: {
    userId?: string,
  }): CancelablePromise<Array<string>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/user/get-user-permissions',
      query: {
        'userId': userId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns boolean Success
   * @throws ApiError
   */
  public static postRbacUserHasPermission({
    userId,
    permissionCode,
  }: {
    userId?: string,
    permissionCode?: string,
  }): CancelablePromise<boolean> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/user/has-permission',
      query: {
        'userId': userId,
        'permissionCode': permissionCode,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
}
