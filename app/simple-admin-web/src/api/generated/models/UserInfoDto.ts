/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { MenuTreeDto } from './MenuTreeDto';
import type { RoleSimpleDto } from './RoleSimpleDto';
/**
 * 用户信息 DTO（包含权限和菜单）
 */
export type UserInfoDto = {
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
   * 账号
   */
  account?: string | null;
  /**
   * 邮箱
   */
  email?: string | null;
  /**
   * 头像
   */
  avatar?: string | null;
  /**
   * 角色列表
   */
  roles?: Array<RoleSimpleDto> | null;
  /**
   * 权限列表
   */
  permissions?: Array<string> | null;
  /**
   * 菜单列表
   */
  menus?: Array<MenuTreeDto> | null;
};

