import type { ApiErrorBody, ApiMeta, ApiSuccess } from '@logicfit/shared-types';

export const API_VERSION = 'v1';
export const API_BASE_PATH = '/api/v1';

export type HealthData = {
  status: 'ok';
  service: 'logicfit-api';
  environment: string;
  version: string;
};

export type ReadinessData = {
  status: 'ready' | 'not_ready';
  service: 'logicfit-api';
  database: 'connected' | 'not_configured' | 'unavailable';
};

export type VersionData = {
  version: string;
  apiVersion: string;
  environment: string;
};

export type HealthResponse = ApiSuccess<HealthData>;
export type ReadinessResponse = ApiSuccess<ReadinessData>;
export type VersionResponse = ApiSuccess<VersionData>;
export type FoundationErrorResponse = ApiErrorBody;

export type SafeDiagnostics = {
  environment: string;
  apiBasePath: string;
  sqlServer: string;
  controlPlaneDatabase: string;
  defaultGymDatabase: string;
  externalNotificationsEnabled: boolean;
};

export type FoundationDiagnosticsResponse = ApiSuccess<SafeDiagnostics>;

export type _ContractMeta = ApiMeta;
