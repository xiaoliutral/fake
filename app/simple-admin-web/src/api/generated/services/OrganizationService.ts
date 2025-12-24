/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { OrganizationCreateDto } from '../models/OrganizationCreateDto';
import type { OrganizationDto } from '../models/OrganizationDto';
import type { OrganizationMoveDto } from '../models/OrganizationMoveDto';
import type { OrganizationTreeDto } from '../models/OrganizationTreeDto';
import type { OrganizationUpdateDto } from '../models/OrganizationUpdateDto';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class OrganizationService {
  /**
   * @returns OrganizationDto Success
   * @throws ApiError
   */
  public static getRbacOrganizationGet({
    id,
  }: {
    id?: string,
  }): CancelablePromise<OrganizationDto> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/organization/get',
      query: {
        'id': id,
      },
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns OrganizationDto Success
   * @throws ApiError
   */
  public static getRbacOrganizationGetList(): CancelablePromise<Array<OrganizationDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/organization/get-list',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns OrganizationTreeDto Success
   * @throws ApiError
   */
  public static getRbacOrganizationGetTree(): CancelablePromise<Array<OrganizationTreeDto>> {
    return __request(OpenAPI, {
      method: 'GET',
      url: '/rbac/organization/get-tree',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns OrganizationDto Success
   * @throws ApiError
   */
  public static postRbacOrganizationCreate({
    requestBody,
  }: {
    requestBody?: OrganizationCreateDto,
  }): CancelablePromise<OrganizationDto> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/organization/create',
      body: requestBody,
      mediaType: 'application/json',
      errors: {
        400: `Bad Request`,
      },
    });
  }
  /**
   * @returns OrganizationDto Success
   * @throws ApiError
   */
  public static putRbacOrganizationUpdate({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: OrganizationUpdateDto,
  }): CancelablePromise<OrganizationDto> {
    return __request(OpenAPI, {
      method: 'PUT',
      url: '/rbac/organization/update',
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
  public static deleteRbacOrganizationDelete({
    id,
  }: {
    id?: string,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'DELETE',
      url: '/rbac/organization/delete',
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
  public static postRbacOrganizationMove({
    id,
    requestBody,
  }: {
    id?: string,
    requestBody?: OrganizationMoveDto,
  }): CancelablePromise<any> {
    return __request(OpenAPI, {
      method: 'POST',
      url: '/rbac/organization/move',
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
}
