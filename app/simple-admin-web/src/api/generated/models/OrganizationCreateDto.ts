/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { OrganizationType } from './OrganizationType';
export type OrganizationCreateDto = {
  parentId?: string | null;
  name?: string | null;
  code?: string | null;
  type?: OrganizationType;
  leaderId?: string | null;
  order?: number;
  description?: string | null;
  isEnabled?: boolean;
};

