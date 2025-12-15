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
     * @param id
     * @returns UserDto Success
     * @throws ApiError
     */
    public static getRbacUserGet(
        id?: string,
    ): CancelablePromise<UserDto> {
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
     * @param keyword 关键字（搜索名称或账号）
     * @param roleId 角色ID
     * @param page 页码（从1开始）
     * @param pageSize 每页数量
     * @param orderBy 排序字段
     * @param descending 是否降序
     * @returns UserDtoPagedResultDto Success
     * @throws ApiError
     */
    public static getRbacUserGetList(
        keyword?: string,
        roleId?: string,
        page?: number,
        pageSize?: number,
        orderBy?: string,
        descending?: boolean,
    ): CancelablePromise<UserDtoPagedResultDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/rbac/user/get-list',
            query: {
                'Keyword': keyword,
                'RoleId': roleId,
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
     * @param roleId
     * @returns UserSimpleDto Success
     * @throws ApiError
     */
    public static getRbacUserGetUsersByRole(
        roleId?: string,
    ): CancelablePromise<Array<UserSimpleDto>> {
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
     * @param requestBody
     * @returns UserDto Success
     * @throws ApiError
     */
    public static postRbacUserCreate(
        requestBody?: UserCreateDto,
    ): CancelablePromise<UserDto> {
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
     * @param id
     * @param requestBody
     * @returns UserDto Success
     * @throws ApiError
     */
    public static putRbacUserUpdate(
        id?: string,
        requestBody?: UserUpdateDto,
    ): CancelablePromise<UserDto> {
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
     * @param id
     * @param requestBody
     * @returns any Success
     * @throws ApiError
     */
    public static putRbacUserUpdatePassword(
        id?: string,
        requestBody?: UpdatePasswordDto,
    ): CancelablePromise<any> {
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
     * @param id
     * @param avatarUrl
     * @returns any Success
     * @throws ApiError
     */
    public static putRbacUserUpdateAvatar(
        id?: string,
        avatarUrl?: string,
    ): CancelablePromise<any> {
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
     * @param id
     * @returns any Success
     * @throws ApiError
     */
    public static deleteRbacUserDelete(
        id?: string,
    ): CancelablePromise<any> {
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
     * @param ids
     * @returns any Success
     * @throws ApiError
     */
    public static deleteRbacUserDeleteBatch(
        ids?: Array<string>,
    ): CancelablePromise<any> {
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
     * @param userId
     * @param requestBody
     * @returns any Success
     * @throws ApiError
     */
    public static postRbacUserAssignRoles(
        userId?: string,
        requestBody?: Array<string>,
    ): CancelablePromise<any> {
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
     * @param userId
     * @returns RoleDto Success
     * @throws ApiError
     */
    public static getRbacUserGetUserRoles(
        userId?: string,
    ): CancelablePromise<Array<RoleDto>> {
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
     * @param userId
     * @returns string Success
     * @throws ApiError
     */
    public static getRbacUserGetUserPermissions(
        userId?: string,
    ): CancelablePromise<Array<string>> {
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
     * @param userId
     * @param permissionCode
     * @returns boolean Success
     * @throws ApiError
     */
    public static postRbacUserHasPermission(
        userId?: string,
        permissionCode?: string,
    ): CancelablePromise<boolean> {
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
