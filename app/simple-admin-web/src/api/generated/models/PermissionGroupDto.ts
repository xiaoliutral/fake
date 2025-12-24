/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PermissionDefinitionDto } from './PermissionDefinitionDto';
/**
 * 权限分组 DTO
 */
export type PermissionGroupDto = {
  /**
   * 分组代码
   */
  code?: string | null;
  /**
   * 分组名称
   */
  name?: string | null;
  /**
   * 权限列表
   */
  permissions?: Array<PermissionDefinitionDto> | null;
};

