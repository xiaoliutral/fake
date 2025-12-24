/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { RoleCreateDto } from '../models/RoleCreateDto';
import type { RoleDto } from '../models/RoleDto';
import type { RoleDtoPagedResultDto } from '../models/RoleDtoPagedResultDto';
import type { RoleSimpleDto } from '../models/RoleSimpleDto';
import type { RoleUpdateDto } from '../models/RoleUpdateDto';
import type { UserSimpleDto } from '../models/UserSimpleDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class RoleService {
  /**
   * @returns RoleDto Success
   * @throws ApiError
   */
  public static getRbacRoleGet({
    id,
  }: {
    id?: string,
  }): CancelablePromise<RoleDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns RoleDtoPagedResultDto Success
   * @throws ApiError
   */
  public static getRbacRoleGetList({
    keyword,
    page,
    pageSize,
    orderBy,
    descending,
  }: {
    /**
     * 关键字（搜索名称或编码）
     */
    keyword?: string,
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
  }): CancelablePromise<RoleDtoPagedResultDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get-list',
      query: {
        'Keyword': keyword,
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
   * @returns RoleSimpleDto Success
   * @throws ApiError
   */
  public static getRbacRoleGetAllRoles(): CancelablePromise<Array<RoleSimpleDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get-all-roles',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns RoleDto Success
   * @throws ApiError
   */
  public static postRbacRoleCreate({
    requestBody,
  }: {
    requestBody?: RoleCreateDto,
  }): CancelablePromise<RoleDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/role/create',
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
  public static putRbacRoleUpdate({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: RoleUpdateDto,
  }): CancelablePromise<RoleDto> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/role/update',
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
  public static deleteRbacRoleDelete({
    id,
  }: {
    id?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/rbac/role/delete',
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
  public static postRbacRoleAssignPermissions({
    roleId,
    requestBody,
  }: {
    roleId?: string,
    requestBody?: Array<string>,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/role/assign-permissions',
      query: {
        'roleId': roleId,
      },
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns string Success
   * @throws ApiError
   */
  public static getRbacRoleGetRolePermissions({
    roleId,
  }: {
    roleId?: string,
  }): CancelablePromise<Array<string>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get-role-permissions',
      query: {
        'roleId': roleId,
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
  public static getRbacRoleGetRoleUsers({
    roleId,
  }: {
    roleId?: string,
  }): CancelablePromise<Array<UserSimpleDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get-role-users',
      query: {
        'roleId': roleId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns number Success
   * @throws ApiError
   */
  public static getRbacRoleGetRoleUserCount({
    roleId,
  }: {
    roleId?: string,
  }): CancelablePromise<number> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/role/get-role-user-count',
      query: {
        'roleId': roleId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
}
