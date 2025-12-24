/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { OrganizationType } from './OrganizationType';
export type OrganizationDto = {
  id?: string;
  parentId?: string | null;
  name?: string | null;
  code?: string | null;
  type?: OrganizationType;
  leaderId?: string | null;
  leaderName?: string | null;
  order?: number;
  description?: string | null;
  isEnabled?: boolean;
  createdBy?: string;
  createdAt?: string;
  updatedBy?: string;
  updatedAt?: string;
};

