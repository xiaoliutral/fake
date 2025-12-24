/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * 角色 DTO
 */
export type RoleDto = {
  id?: string;
  createdAt?: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
  /**
   * 名称
   */
  name?: string | null;
  /**
   * 编码
   */
  code?: string | null;
  /**
   * 权限列表
   */
  permissions?: Array<string> | null;
};

