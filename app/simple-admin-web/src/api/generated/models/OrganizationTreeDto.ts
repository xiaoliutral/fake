/* generated using openapi-typescript-codegen -- do no edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { OrganizationType } from './OrganizationType';
export type OrganizationTreeDto = {
  id?: string;
  parentId?: string | null;
  name?: string | null;
  code?: string | null;
  type?: OrganizationType;
  leaderId?: string | null;
  leaderName?: string | null;
  order?: number;
  isEnabled?: boolean;
  children?: Array<OrganizationTreeDto> | null;
};

