/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { MenuType } from './MenuType';
/**
 * 创建菜单 DTO
 */
export type MenuCreateDto = {
  /**
   * 父级菜单ID
   */
  pId?: string | null;
  /**
   * 名称
   */
  name: string;
  /**
   * 权限代码
   */
  permissionCode?: string | null;
  type: MenuType;
  /**
   * 图标
   */
  icon?: string | null;
  /**
   * 路由
   */
  route?: string | null;
  /**
   * 组件
   */
  component?: string | null;
  /**
   * 排序
   */
  order?: number;
  /**
   * 是否隐藏
   */
  isHidden?: boolean;
  /**
   * 是否缓存
   */
  isCached?: boolean;
  /**
   * 描述
   */
  description?: string | null;
};

