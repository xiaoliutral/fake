/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { MenuCreateDto } from '../models/MenuCreateDto';
import type { MenuDto } from '../models/MenuDto';
import type { MenuTreeDto } from '../models/MenuTreeDto';
import type { MenuUpdateDto } from '../models/MenuUpdateDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MenuService {
  /**
   * @returns MenuDto Success
   * @throws ApiError
   */
  public static getRbacMenuGet({
    id,
  }: {
    id?: string,
  }): CancelablePromise<MenuDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/menu/get',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns MenuTreeDto Success
   * @throws ApiError
   */
  public static getRbacMenuGetMenuTree(): CancelablePromise<Array<MenuTreeDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/menu/get-menu-tree',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns MenuTreeDto Success
   * @throws ApiError
   */
  public static getRbacMenuGetUserMenus({
    userId,
  }: {
    userId?: string,
  }): CancelablePromise<Array<MenuTreeDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/menu/get-user-menus',
      query: {
        'userId': userId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns MenuDto Success
   * @throws ApiError
   */
  public static postRbacMenuCreate({
    requestBody,
  }: {
    requestBody?: MenuCreateDto,
  }): CancelablePromise<MenuDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/menu/create',
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns MenuDto Success
   * @throws ApiError
   */
  public static putRbacMenuUpdate({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: MenuUpdateDto,
  }): CancelablePromise<MenuDto> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/menu/update',
      query: {
        'id': id,
      },
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static putRbacMenuUpdateOrder({
    id,
    order,
  }: {
    id?: string,
    order?: number,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/menu/update-order',
      query: {
        'id': id,
        'order': order,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static deleteRbacMenuDelete({
    id,
  }: {
    id?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/rbac/menu/delete',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  public static postRbacMenuMoveMenu({
    menuId,
    targetParentId,
  }: {
    menuId?: string,
    targetParentId?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/menu/move-menu',
      query: {
        'menuId': menuId,
        'targetParentId': targetParentId,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
}
