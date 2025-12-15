/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { UserInfoDto } from '../models/UserInfoDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthService {
    /**
     * @param account
     * @param password
     * @returns UserInfoDto Success
     * @throws ApiError
     */
    public static postRbacAuthLogin(
        account?: string,
        password?: string,
    ): CancelablePromise<UserInfoDto> {
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
     * @param userId
     * @returns UserInfoDto Success
     * @throws ApiError
     */
    public static getRbacAuthGetCurrentUser(
        userId?: string,
    ): CancelablePromise<UserInfoDto> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/rbac/auth/get-current-user',
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
     * @param oldPassword
     * @param newPassword
     * @returns any Success
     * @throws ApiError
     */
    public static postRbacAuthChangePassword(
        userId?: string,
        oldPassword?: string,
        newPassword?: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/rbac/auth/change-password',
            query: {
                'userId': userId,
                'oldPassword': oldPassword,
                'newPassword': newPassword,
            },
            errors: {
                400: `Bad Request`,
            },
        });
    }
}
