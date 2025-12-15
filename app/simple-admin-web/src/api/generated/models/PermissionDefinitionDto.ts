/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * 权限定义 DTO
 */
export type PermissionDefinitionDto = {
    /**
     * 权限代码
     */
    code?: string | null;
    /**
     * 权限名称
     */
    name?: string | null;
    /**
     * 父级权限代码
     */
    parentCode?: string | null;
    /**
     * 描述
     */
    description?: string | null;
    /**
     * 子权限列表
     */
    children?: Array<PermissionDefinitionDto> | null;
};

