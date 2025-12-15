/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PermissionDefinitionDto } from '../models/PermissionDefinitionDto';
import type { PermissionGroupDto } from '../models/PermissionGroupDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PermissionService {
    /**
     * @returns PermissionDefinitionDto Success
     * @throws ApiError
     */
    public static getRbacPermissionGetAllPermissions(): CancelablePromise<Array<PermissionDefinitionDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/rbac/permission/get-all-permissions',
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @returns PermissionGroupDto Success
     * @throws ApiError
     */
    public static getRbacPermissionGetPermissionTree(): CancelablePromise<Array<PermissionGroupDto>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/rbac/permission/get-permission-tree',
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
    public static postRbacPermissionCheckPermission(
        userId?: string,
        permissionCode?: string,
    ): CancelablePromise<boolean> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/rbac/permission/check-permission',
            query: {
                'userId': userId,
                'permissionCode': permissionCode,
            },
            errors: {
                400: `Bad Request`,
            },
        });
    }
    /**
     * @param userId
     * @param requestBody
     * @returns boolean Success
     * @throws ApiError
     */
    public static postRbacPermissionCheckPermissions(
        userId?: string,
        requestBody?: Array<string>,
    ): CancelablePromise<Record<string, boolean>> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/rbac/permission/check-permissions',
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
}
