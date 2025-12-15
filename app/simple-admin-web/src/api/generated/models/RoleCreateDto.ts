/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * 创建角色 DTO
 */
export type RoleCreateDto = {
    /**
     * 名称
     */
    name: string;
    /**
     * 编码
     */
    code: string;
    /**
     * 权限列表
     */
    permissions?: Array<string> | null;
};

