/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * 创建用户 DTO
 */
export type UserCreateDto = {
  /**
   * 名称
   */
  name: string;
  /**
   * 账号
   */
  account: string;
  /**
   * 密码
   */
  password: string;
  /**
   * 邮箱
   */
  email?: string | null;
  /**
   * 头像
   */
  avatar?: string | null;
  /**
   * 角色ID列表
   */
  roleIds?: Array<string> | null;
};

