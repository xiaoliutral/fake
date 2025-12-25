/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * 登录结果 DTO
 */
export type LoginResultDto = {
  /**
   * 访问令牌
   */
  accessToken?: string | null;
  /**
   * 刷新令牌
   */
  refreshToken?: string | null;
  /**
   * 令牌类型
   */
  tokenType?: string | null;
  /**
   * 过期时间（秒）
   */
  expiresIn?: number;
  /**
   * 用户ID
   */
  userId?: string;
};

